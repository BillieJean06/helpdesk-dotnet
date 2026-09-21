namespace Helpdesk.Domain.Suporte;

/// <summary>
/// Agregado raiz do contexto Suporte. Todas as mudanças de estado passam por
/// métodos que protegem as invariantes; nenhuma camada externa altera o status.
/// </summary>
public sealed class Ticket
{
    private readonly List<Comentario> _comentarios = [];

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string Descricao { get; private set; } = string.Empty;
    public Prioridade Prioridade { get; private set; }
    public StatusTicket Status { get; private set; }
    public Guid SolicitanteId { get; private set; }
    public Guid? ResponsavelId { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    // SLA
    public PrazosSla Sla { get; private set; } = null!;
    public DateTimeOffset PrazoPrimeiraResposta { get; private set; }
    public DateTimeOffset PrazoResolucao { get; private set; }
    public DateTimeOffset? PrimeiraRespostaEm { get; private set; }
    public DateTimeOffset? ResolvidoEm { get; private set; }
    public DateTimeOffset? PausadoDesde { get; private set; }

    public IReadOnlyCollection<Comentario> Comentarios => _comentarios.AsReadOnly();

    private Ticket() { } // EF Core

    public static Ticket Abrir(
        Guid tenantId,
        Guid solicitanteId,
        string titulo,
        string descricao,
        Prioridade prioridade,
        DateTimeOffset agora,
        PoliticaSla? politica = null)
    {
        if (tenantId == Guid.Empty) throw new DomainException("Tenant é obrigatório.");
        if (solicitanteId == Guid.Empty) throw new DomainException("Solicitante é obrigatório.");
        if (string.IsNullOrWhiteSpace(titulo)) throw new DomainException("Título é obrigatório.");
        if (string.IsNullOrWhiteSpace(descricao)) throw new DomainException("Descrição é obrigatória.");
        if (!Enum.IsDefined(prioridade)) throw new DomainException("Prioridade inválida.");

        var sla = (politica ?? PoliticaSla.Padrao).Para(prioridade);

        return new Ticket
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SolicitanteId = solicitanteId,
            Titulo = titulo.Trim(),
            Descricao = descricao.Trim(),
            Prioridade = prioridade,
            Status = StatusTicket.Aberto,
            CriadoEm = agora,
            AtualizadoEm = agora,
            Sla = sla,
            PrazoPrimeiraResposta = agora + sla.PrimeiraResposta,
            PrazoResolucao = agora + sla.Resolucao
        };
    }

    /// <summary>Atendente assume um ticket na fila (Aberto ou Reaberto).</summary>
    public void Assumir(Guid atendenteId, DateTimeOffset agora)
    {
        if (atendenteId == Guid.Empty) throw new DomainException("Atendente é obrigatório.");
        ExigirStatus("assumir", StatusTicket.Aberto, StatusTicket.Reaberto);

        ResponsavelId = atendenteId;
        MudarPara(StatusTicket.EmAtendimento, agora);
    }

    /// <summary>
    /// Atendente pediu informação e aguarda o cliente. Conta como primeira resposta
    /// e pausa o relógio de SLA até <see cref="RetomarAtendimento"/>.
    /// </summary>
    public void AguardarCliente(Guid atendenteId, DateTimeOffset agora)
    {
        ExigirResponsavel(atendenteId);
        ExigirStatus("aguardar o cliente", StatusTicket.EmAtendimento);

        RegistrarPrimeiraResposta(agora);
        PausadoDesde = agora;
        MudarPara(StatusTicket.AguardandoCliente, agora);
    }

    /// <summary>
    /// O cliente respondeu; o atendimento volta a correr. O tempo pausado é
    /// devolvido ao prazo de resolução.
    /// </summary>
    public void RetomarAtendimento(DateTimeOffset agora)
    {
        ExigirStatus("retomar o atendimento", StatusTicket.AguardandoCliente);

        PrazoResolucao += agora - PausadoDesde!.Value;
        PausadoDesde = null;
        MudarPara(StatusTicket.EmAtendimento, agora);
    }

    /// <summary>Resolver conta como primeira resposta caso ainda não tenha havido nenhuma.</summary>
    public void Resolver(Guid atendenteId, DateTimeOffset agora)
    {
        ExigirResponsavel(atendenteId);
        ExigirStatus("resolver", StatusTicket.EmAtendimento);

        RegistrarPrimeiraResposta(agora);
        ResolvidoEm = agora;
        MudarPara(StatusTicket.Resolvido, agora);
    }

    public void Fechar(DateTimeOffset agora)
    {
        ExigirStatus("fechar", StatusTicket.Resolvido);

        MudarPara(StatusTicket.Fechado, agora);
    }

    /// <summary>
    /// Reabertura formal: devolve o ticket à fila, sem responsável, e reinicia
    /// os dois prazos de SLA a partir de agora.
    /// </summary>
    public void Reabrir(DateTimeOffset agora)
    {
        ExigirStatus("reabrir", StatusTicket.Resolvido, StatusTicket.Fechado);

        ResponsavelId = null;
        PrimeiraRespostaEm = null;
        ResolvidoEm = null;
        PrazoPrimeiraResposta = agora + Sla.PrimeiraResposta;
        PrazoResolucao = agora + Sla.Resolucao;
        MudarPara(StatusTicket.Reaberto, agora);
    }

    /// <summary>Um comentário de quem não é o solicitante conta como primeira resposta.</summary>
    public void Comentar(Guid autorId, string texto, DateTimeOffset agora)
    {
        if (autorId == Guid.Empty) throw new DomainException("Autor é obrigatório.");
        if (string.IsNullOrWhiteSpace(texto)) throw new DomainException("Comentário não pode ser vazio.");
        if (Status == StatusTicket.Fechado)
            throw new DomainException("Não é possível comentar em um ticket fechado.");

        _comentarios.Add(new Comentario(autorId, texto.Trim(), agora));
        if (autorId != SolicitanteId) RegistrarPrimeiraResposta(agora);
        AtualizadoEm = agora;
    }

    // Consultas de SLA. O relógio congela enquanto o ticket está pausado e
    // depois que o marco (primeira resposta / resolução) foi atingido.

    public TimeSpan TempoRestantePrimeiraResposta(DateTimeOffset agora) =>
        PrazoPrimeiraResposta - (PrimeiraRespostaEm ?? RelogioAtual(agora));

    public TimeSpan TempoRestanteResolucao(DateTimeOffset agora) =>
        PrazoResolucao - (ResolvidoEm ?? RelogioAtual(agora));

    public bool PrimeiraRespostaVencida(DateTimeOffset agora) =>
        TempoRestantePrimeiraResposta(agora) < TimeSpan.Zero;

    public bool ResolucaoVencida(DateTimeOffset agora) =>
        TempoRestanteResolucao(agora) < TimeSpan.Zero;

    private DateTimeOffset RelogioAtual(DateTimeOffset agora) => PausadoDesde ?? agora;

    private void RegistrarPrimeiraResposta(DateTimeOffset agora) =>
        PrimeiraRespostaEm ??= agora;

    private void ExigirStatus(string acao, params StatusTicket[] permitidos)
    {
        if (!permitidos.Contains(Status))
            throw new DomainException($"Não é possível {acao} um ticket com status {Status}.");
    }

    private void ExigirResponsavel(Guid atendenteId)
    {
        if (ResponsavelId is null || ResponsavelId != atendenteId)
            throw new DomainException("Apenas o responsável pelo ticket pode executar esta ação.");
    }

    private void MudarPara(StatusTicket novo, DateTimeOffset agora)
    {
        Status = novo;
        AtualizadoEm = agora;
    }
}
