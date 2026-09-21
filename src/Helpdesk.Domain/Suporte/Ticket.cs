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

    public IReadOnlyCollection<Comentario> Comentarios => _comentarios.AsReadOnly();

    private Ticket() { } // EF Core

    public static Ticket Abrir(
        Guid tenantId,
        Guid solicitanteId,
        string titulo,
        string descricao,
        Prioridade prioridade,
        DateTimeOffset agora)
    {
        if (tenantId == Guid.Empty) throw new DomainException("Tenant é obrigatório.");
        if (solicitanteId == Guid.Empty) throw new DomainException("Solicitante é obrigatório.");
        if (string.IsNullOrWhiteSpace(titulo)) throw new DomainException("Título é obrigatório.");
        if (string.IsNullOrWhiteSpace(descricao)) throw new DomainException("Descrição é obrigatória.");
        if (!Enum.IsDefined(prioridade)) throw new DomainException("Prioridade inválida.");

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
            AtualizadoEm = agora
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

    /// <summary>Atendente pediu informação e aguarda o cliente.</summary>
    public void AguardarCliente(Guid atendenteId, DateTimeOffset agora)
    {
        ExigirResponsavel(atendenteId);
        ExigirStatus("aguardar o cliente", StatusTicket.EmAtendimento);

        MudarPara(StatusTicket.AguardandoCliente, agora);
    }

    /// <summary>O cliente respondeu; o atendimento volta a correr.</summary>
    public void RetomarAtendimento(DateTimeOffset agora)
    {
        ExigirStatus("retomar o atendimento", StatusTicket.AguardandoCliente);

        MudarPara(StatusTicket.EmAtendimento, agora);
    }

    public void Resolver(Guid atendenteId, DateTimeOffset agora)
    {
        ExigirResponsavel(atendenteId);
        ExigirStatus("resolver", StatusTicket.EmAtendimento);

        MudarPara(StatusTicket.Resolvido, agora);
    }

    public void Fechar(DateTimeOffset agora)
    {
        ExigirStatus("fechar", StatusTicket.Resolvido);

        MudarPara(StatusTicket.Fechado, agora);
    }

    /// <summary>Reabertura formal: devolve o ticket à fila, sem responsável.</summary>
    public void Reabrir(DateTimeOffset agora)
    {
        ExigirStatus("reabrir", StatusTicket.Resolvido, StatusTicket.Fechado);

        ResponsavelId = null;
        MudarPara(StatusTicket.Reaberto, agora);
    }

    public void Comentar(Guid autorId, string texto, DateTimeOffset agora)
    {
        if (autorId == Guid.Empty) throw new DomainException("Autor é obrigatório.");
        if (string.IsNullOrWhiteSpace(texto)) throw new DomainException("Comentário não pode ser vazio.");
        if (Status == StatusTicket.Fechado)
            throw new DomainException("Não é possível comentar em um ticket fechado.");

        _comentarios.Add(new Comentario(autorId, texto.Trim(), agora));
        AtualizadoEm = agora;
    }

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
