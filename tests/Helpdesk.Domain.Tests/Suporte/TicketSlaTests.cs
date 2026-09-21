using Helpdesk.Domain.Suporte;
using Microsoft.Extensions.Time.Testing;

namespace Helpdesk.Domain.Tests.Suporte;

public class TicketSlaTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Cliente = Guid.NewGuid();
    private static readonly Guid Atendente = Guid.NewGuid();

    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero));

    private DateTimeOffset Agora => _relogio.GetUtcNow();

    // Alta: 1h para primeira resposta, 8h para resolução
    private Ticket NovoTicket(Prioridade prioridade = Prioridade.Alta, PoliticaSla? politica = null) =>
        Ticket.Abrir(Tenant, Cliente, "Sistema não carrega", "Tela branca", prioridade, Agora, politica);

    private Ticket EmAtendimento()
    {
        var t = NovoTicket();
        t.Assumir(Atendente, Agora);
        return t;
    }

    // Prazos na abertura

    [Theory]
    [InlineData(Prioridade.Critica, 15, 4 * 60)]
    [InlineData(Prioridade.Alta, 60, 8 * 60)]
    [InlineData(Prioridade.Media, 4 * 60, 24 * 60)]
    [InlineData(Prioridade.Baixa, 8 * 60, 72 * 60)]
    public void Abrir_calcula_prazos_pela_prioridade(Prioridade prioridade, int minutosResposta, int minutosResolucao)
    {
        var t = NovoTicket(prioridade);

        Assert.Equal(Agora.AddMinutes(minutosResposta), t.PrazoPrimeiraResposta);
        Assert.Equal(Agora.AddMinutes(minutosResolucao), t.PrazoResolucao);
    }

    [Fact]
    public void Ticket_guarda_o_sla_contratado_na_abertura()
    {
        var politica = new PoliticaSla(Enum.GetValues<Prioridade>()
            .ToDictionary(p => p, _ => new PrazosSla(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30))));

        var t = NovoTicket(Prioridade.Baixa, politica);

        Assert.Equal(Agora.AddMinutes(5), t.PrazoPrimeiraResposta);
        Assert.Equal(Agora.AddMinutes(30), t.PrazoResolucao);
        Assert.Equal(TimeSpan.FromMinutes(30), t.Sla.Resolucao);
    }

    // Tempo restante e vencimento

    [Fact]
    public void Tempo_restante_diminui_conforme_o_relogio_avanca()
    {
        var t = NovoTicket();

        _relogio.Advance(TimeSpan.FromMinutes(20));

        Assert.Equal(TimeSpan.FromMinutes(40), t.TempoRestantePrimeiraResposta(Agora));
        Assert.Equal(TimeSpan.FromHours(8) - TimeSpan.FromMinutes(20), t.TempoRestanteResolucao(Agora));
    }

    [Fact]
    public void Exatamente_no_prazo_ainda_nao_esta_vencido_e_um_instante_depois_esta()
    {
        var t = NovoTicket();

        _relogio.Advance(TimeSpan.FromHours(1));
        Assert.False(t.PrimeiraRespostaVencida(Agora));

        _relogio.Advance(TimeSpan.FromSeconds(1));
        Assert.True(t.PrimeiraRespostaVencida(Agora));
        Assert.False(t.ResolucaoVencida(Agora));
    }

    // Primeira resposta

    [Fact]
    public void Assumir_sozinho_nao_conta_como_primeira_resposta()
    {
        var t = NovoTicket();

        t.Assumir(Atendente, Agora);

        Assert.Null(t.PrimeiraRespostaEm);
    }

    [Fact]
    public void Comentario_do_solicitante_nao_conta_como_primeira_resposta()
    {
        var t = NovoTicket();

        t.Comentar(Cliente, "Alguém aí?", Agora);

        Assert.Null(t.PrimeiraRespostaEm);
    }

    [Fact]
    public void Comentario_do_atendente_registra_primeira_resposta_e_para_o_relogio()
    {
        var t = EmAtendimento();
        _relogio.Advance(TimeSpan.FromMinutes(30));
        var respondeuEm = Agora;

        t.Comentar(Atendente, "Estou verificando", Agora);
        _relogio.Advance(TimeSpan.FromHours(5));

        Assert.Equal(respondeuEm, t.PrimeiraRespostaEm);
        Assert.False(t.PrimeiraRespostaVencida(Agora));
        Assert.Equal(TimeSpan.FromMinutes(30), t.TempoRestantePrimeiraResposta(Agora));
    }

    [Fact]
    public void Segunda_resposta_nao_altera_o_marco_da_primeira()
    {
        var t = EmAtendimento();
        t.Comentar(Atendente, "Primeira", Agora);
        var primeira = t.PrimeiraRespostaEm;

        _relogio.Advance(TimeSpan.FromMinutes(10));
        t.Comentar(Atendente, "Segunda", Agora);

        Assert.Equal(primeira, t.PrimeiraRespostaEm);
    }

    [Fact]
    public void Responder_depois_do_prazo_deixa_a_primeira_resposta_vencida_para_sempre()
    {
        var t = EmAtendimento();
        _relogio.Advance(TimeSpan.FromMinutes(90));

        t.Comentar(Atendente, "Desculpe a demora", Agora);
        _relogio.Advance(TimeSpan.FromDays(2));

        Assert.True(t.PrimeiraRespostaVencida(Agora));
    }

    [Fact]
    public void AguardarCliente_conta_como_primeira_resposta()
    {
        var t = EmAtendimento();
        _relogio.Advance(TimeSpan.FromMinutes(10));

        t.AguardarCliente(Atendente, Agora);

        Assert.Equal(Agora, t.PrimeiraRespostaEm);
    }

    [Fact]
    public void Resolver_sem_resposta_previa_conta_como_primeira_resposta()
    {
        var t = EmAtendimento();
        _relogio.Advance(TimeSpan.FromMinutes(10));

        t.Resolver(Atendente, Agora);

        Assert.Equal(Agora, t.PrimeiraRespostaEm);
    }

    // Pausa

    [Fact]
    public void Relogio_congela_enquanto_aguarda_o_cliente()
    {
        var t = EmAtendimento();
        _relogio.Advance(TimeSpan.FromHours(1));
        t.AguardarCliente(Atendente, Agora);
        var restanteAoPausar = t.TempoRestanteResolucao(Agora);

        _relogio.Advance(TimeSpan.FromDays(3));

        Assert.Equal(restanteAoPausar, t.TempoRestanteResolucao(Agora));
        Assert.False(t.ResolucaoVencida(Agora));
    }

    [Fact]
    public void Ao_retomar_o_tempo_pausado_e_devolvido_ao_prazo()
    {
        var t = EmAtendimento();
        var prazoOriginal = t.PrazoResolucao;
        _relogio.Advance(TimeSpan.FromHours(1));
        t.AguardarCliente(Atendente, Agora);
        var restanteAoPausar = t.TempoRestanteResolucao(Agora);

        _relogio.Advance(TimeSpan.FromHours(10));
        t.RetomarAtendimento(Agora);

        Assert.Equal(prazoOriginal.AddHours(10), t.PrazoResolucao);
        Assert.Null(t.PausadoDesde);
        Assert.Equal(restanteAoPausar, t.TempoRestanteResolucao(Agora));
    }

    [Fact]
    public void Depois_de_retomar_o_relogio_volta_a_correr()
    {
        var t = EmAtendimento();
        t.AguardarCliente(Atendente, Agora);
        _relogio.Advance(TimeSpan.FromHours(2));
        t.RetomarAtendimento(Agora);
        var restante = t.TempoRestanteResolucao(Agora);

        _relogio.Advance(TimeSpan.FromHours(3));

        Assert.Equal(restante - TimeSpan.FromHours(3), t.TempoRestanteResolucao(Agora));
    }

    [Fact]
    public void Varias_pausas_acumulam()
    {
        var t = EmAtendimento();
        var prazoOriginal = t.PrazoResolucao;

        t.AguardarCliente(Atendente, Agora);
        _relogio.Advance(TimeSpan.FromHours(2));
        t.RetomarAtendimento(Agora);

        _relogio.Advance(TimeSpan.FromMinutes(30));
        t.AguardarCliente(Atendente, Agora);
        _relogio.Advance(TimeSpan.FromHours(4));
        t.RetomarAtendimento(Agora);

        Assert.Equal(prazoOriginal.AddHours(6), t.PrazoResolucao);
    }

    // Resolução

    [Fact]
    public void Resolver_dentro_do_prazo_nao_vence_depois()
    {
        var t = EmAtendimento();
        _relogio.Advance(TimeSpan.FromHours(2));
        t.Resolver(Atendente, Agora);

        _relogio.Advance(TimeSpan.FromDays(30));

        Assert.False(t.ResolucaoVencida(Agora));
        Assert.Equal(Agora.AddDays(-30), t.ResolvidoEm);
    }

    [Fact]
    public void Resolver_fora_do_prazo_permanece_violado()
    {
        var t = EmAtendimento();
        _relogio.Advance(TimeSpan.FromHours(9));
        t.Resolver(Atendente, Agora);

        Assert.True(t.ResolucaoVencida(Agora));
        t.Fechar(Agora);
        Assert.True(t.ResolucaoVencida(Agora.AddDays(10)));
    }

    [Fact]
    public void Resolver_apos_uma_pausa_longa_respeita_o_prazo_estendido()
    {
        var t = EmAtendimento();
        _relogio.Advance(TimeSpan.FromHours(1));
        t.AguardarCliente(Atendente, Agora);
        _relogio.Advance(TimeSpan.FromDays(2));
        t.RetomarAtendimento(Agora);
        _relogio.Advance(TimeSpan.FromHours(6));

        t.Resolver(Atendente, Agora);

        // 1h + 6h trabalhadas < 8h de SLA, apesar de 2 dias corridos
        Assert.False(t.ResolucaoVencida(Agora));
    }

    // Reabertura

    [Fact]
    public void Reabrir_reinicia_os_dois_prazos()
    {
        var t = EmAtendimento();
        t.Comentar(Atendente, "Feito", Agora);
        t.Resolver(Atendente, Agora);
        _relogio.Advance(TimeSpan.FromDays(1));

        t.Reabrir(Agora);

        Assert.Null(t.PrimeiraRespostaEm);
        Assert.Null(t.ResolvidoEm);
        Assert.Equal(Agora.AddHours(1), t.PrazoPrimeiraResposta);
        Assert.Equal(Agora.AddHours(8), t.PrazoResolucao);
        Assert.False(t.PrimeiraRespostaVencida(Agora));
        Assert.False(t.ResolucaoVencida(Agora));
    }

    // Política e prazos

    [Fact]
    public void Politica_exige_prazos_para_todas_as_prioridades()
    {
        var incompleta = new Dictionary<Prioridade, PrazosSla>
        {
            [Prioridade.Baixa] = new(TimeSpan.FromHours(1), TimeSpan.FromHours(2))
        };

        Assert.Throws<DomainException>(() => new PoliticaSla(incompleta));
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(-5, 60)]
    [InlineData(60, 30)]
    public void Prazos_invalidos_sao_rejeitados(int minutosResposta, int minutosResolucao)
    {
        Assert.Throws<DomainException>(() =>
            new PrazosSla(TimeSpan.FromMinutes(minutosResposta), TimeSpan.FromMinutes(minutosResolucao)));
    }
}
