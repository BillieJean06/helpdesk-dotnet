using Helpdesk.Domain.Suporte;

namespace Helpdesk.Domain.Tests.Suporte;

public class TicketTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Cliente = Guid.NewGuid();
    private static readonly Guid Atendente = Guid.NewGuid();
    private static readonly DateTimeOffset T0 = new(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

    private static Ticket NovoTicket() =>
        Ticket.Abrir(Tenant, Cliente, "Sistema não carrega", "Tela branca ao logar", Prioridade.Alta, T0);

    private static Ticket EmAtendimento()
    {
        var t = NovoTicket();
        t.Assumir(Atendente, T0.AddMinutes(5));
        return t;
    }

    private static Ticket Resolvido()
    {
        var t = EmAtendimento();
        t.Resolver(Atendente, T0.AddHours(1));
        return t;
    }

    // Abrir

    [Fact]
    public void Abrir_cria_ticket_aberto_sem_responsavel()
    {
        var t = NovoTicket();

        Assert.Equal(StatusTicket.Aberto, t.Status);
        Assert.Null(t.ResponsavelId);
        Assert.Equal(Cliente, t.SolicitanteId);
        Assert.Equal(T0, t.CriadoEm);
        Assert.NotEqual(Guid.Empty, t.Id);
    }

    [Theory]
    [InlineData("", "desc")]
    [InlineData("   ", "desc")]
    [InlineData("titulo", "")]
    [InlineData("titulo", "  ")]
    public void Abrir_rejeita_titulo_ou_descricao_vazios(string titulo, string descricao)
    {
        Assert.Throws<DomainException>(() =>
            Ticket.Abrir(Tenant, Cliente, titulo, descricao, Prioridade.Baixa, T0));
    }

    [Fact]
    public void Abrir_rejeita_tenant_ou_solicitante_vazios()
    {
        Assert.Throws<DomainException>(() =>
            Ticket.Abrir(Guid.Empty, Cliente, "t", "d", Prioridade.Baixa, T0));
        Assert.Throws<DomainException>(() =>
            Ticket.Abrir(Tenant, Guid.Empty, "t", "d", Prioridade.Baixa, T0));
    }

    [Fact]
    public void Abrir_rejeita_prioridade_invalida()
    {
        Assert.Throws<DomainException>(() =>
            Ticket.Abrir(Tenant, Cliente, "t", "d", (Prioridade)99, T0));
    }

    // Fluxo feliz

    [Fact]
    public void Fluxo_completo_ate_fechar()
    {
        var t = NovoTicket();

        t.Assumir(Atendente, T0.AddMinutes(5));
        Assert.Equal(StatusTicket.EmAtendimento, t.Status);
        Assert.Equal(Atendente, t.ResponsavelId);

        t.AguardarCliente(Atendente, T0.AddMinutes(10));
        Assert.Equal(StatusTicket.AguardandoCliente, t.Status);

        t.RetomarAtendimento(T0.AddMinutes(30));
        Assert.Equal(StatusTicket.EmAtendimento, t.Status);

        t.Resolver(Atendente, T0.AddHours(1));
        Assert.Equal(StatusTicket.Resolvido, t.Status);

        t.Fechar(T0.AddDays(1));
        Assert.Equal(StatusTicket.Fechado, t.Status);
        Assert.Equal(T0.AddDays(1), t.AtualizadoEm);
    }

    // Transições inválidas

    [Fact]
    public void Nao_pula_de_Aberto_direto_para_Resolvido_ou_Fechado()
    {
        var t = NovoTicket();

        Assert.Throws<DomainException>(() => t.Resolver(Atendente, T0));
        Assert.Throws<DomainException>(() => t.Fechar(T0));
        Assert.Equal(StatusTicket.Aberto, t.Status);
    }

    [Fact]
    public void Nao_assume_ticket_que_ja_esta_em_atendimento()
    {
        var t = EmAtendimento();

        Assert.Throws<DomainException>(() => t.Assumir(Guid.NewGuid(), T0));
        Assert.Equal(Atendente, t.ResponsavelId);
    }

    [Fact]
    public void Nao_fecha_ticket_que_nao_foi_resolvido()
    {
        var t = EmAtendimento();

        Assert.Throws<DomainException>(() => t.Fechar(T0));
    }

    [Fact]
    public void Nao_retoma_atendimento_se_nao_esta_aguardando_cliente()
    {
        var t = EmAtendimento();

        Assert.Throws<DomainException>(() => t.RetomarAtendimento(T0));
    }

    [Fact]
    public void Nao_reabre_ticket_que_ainda_esta_em_andamento()
    {
        Assert.Throws<DomainException>(() => NovoTicket().Reabrir(T0));
        Assert.Throws<DomainException>(() => EmAtendimento().Reabrir(T0));
    }

    // Responsável

    [Fact]
    public void Apenas_o_responsavel_resolve()
    {
        var t = EmAtendimento();

        Assert.Throws<DomainException>(() => t.Resolver(Guid.NewGuid(), T0));
        Assert.Equal(StatusTicket.EmAtendimento, t.Status);
    }

    [Fact]
    public void Apenas_o_responsavel_coloca_em_espera_do_cliente()
    {
        var t = EmAtendimento();

        Assert.Throws<DomainException>(() => t.AguardarCliente(Guid.NewGuid(), T0));
    }

    [Fact]
    public void Assumir_exige_atendente_valido()
    {
        Assert.Throws<DomainException>(() => NovoTicket().Assumir(Guid.Empty, T0));
    }

    // Reabertura

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reabrir_devolve_a_fila_sem_responsavel(bool fecharAntes)
    {
        var t = Resolvido();
        if (fecharAntes) t.Fechar(T0.AddDays(1));

        t.Reabrir(T0.AddDays(2));

        Assert.Equal(StatusTicket.Reaberto, t.Status);
        Assert.Null(t.ResponsavelId);
    }

    [Fact]
    public void Ticket_reaberto_pode_ser_assumido_por_outro_atendente()
    {
        var t = Resolvido();
        t.Reabrir(T0.AddDays(1));
        var outro = Guid.NewGuid();

        t.Assumir(outro, T0.AddDays(1));

        Assert.Equal(StatusTicket.EmAtendimento, t.Status);
        Assert.Equal(outro, t.ResponsavelId);
    }

    // Comentários

    [Fact]
    public void Comentar_registra_autor_texto_e_data()
    {
        var t = NovoTicket();

        t.Comentar(Cliente, "  Ainda acontece  ", T0.AddMinutes(2));

        var c = Assert.Single(t.Comentarios);
        Assert.Equal(Cliente, c.AutorId);
        Assert.Equal("Ainda acontece", c.Texto);
        Assert.Equal(T0.AddMinutes(2), c.CriadoEm);
        Assert.Equal(T0.AddMinutes(2), t.AtualizadoEm);
    }

    [Fact]
    public void Comentar_rejeita_texto_vazio_e_autor_vazio()
    {
        var t = NovoTicket();

        Assert.Throws<DomainException>(() => t.Comentar(Cliente, "  ", T0));
        Assert.Throws<DomainException>(() => t.Comentar(Guid.Empty, "oi", T0));
        Assert.Empty(t.Comentarios);
    }

    [Fact]
    public void Nao_comenta_em_ticket_fechado()
    {
        var t = Resolvido();
        t.Fechar(T0.AddDays(1));

        Assert.Throws<DomainException>(() => t.Comentar(Cliente, "obrigado", T0.AddDays(2)));
    }

    [Fact]
    public void Comentarios_nao_podem_ser_alterados_de_fora()
    {
        var t = NovoTicket();

        Assert.False(t.Comentarios is IList<Comentario> lista && !lista.IsReadOnly);
    }
}
