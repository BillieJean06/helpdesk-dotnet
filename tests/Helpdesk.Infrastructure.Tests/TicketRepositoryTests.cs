using Helpdesk.Domain.Suporte;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Helpdesk.Infrastructure.Tests;

public class TicketRepositoryTests(PostgresFixture pg) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

    // Cada teste usa um tenant novo: os testes compartilham o banco sem se enxergar.
    private readonly Guid _tenant = Guid.NewGuid();
    private readonly Guid _cliente = Guid.NewGuid();
    private readonly Guid _atendente = Guid.NewGuid();

    private Ticket NovoTicket(Guid? tenant = null) =>
        Ticket.Abrir(tenant ?? _tenant, _cliente, "Sistema não carrega", "Tela branca ao logar", Prioridade.Critica, T0);

    private async Task<Guid> Persistir(Ticket ticket)
    {
        using var sessao = pg.AbrirSessao(ticket.TenantId);
        sessao.Repo.Adicionar(ticket);
        await sessao.Repo.SalvarAsync();
        return ticket.Id;
    }

    [FactPostgres]
    public async Task Persiste_e_recarrega_o_agregado_completo()
    {
        var original = NovoTicket();
        original.Assumir(_atendente, T0.AddMinutes(5));
        original.Comentar(_cliente, "Urgente", T0.AddMinutes(6));
        original.Comentar(_atendente, "Verificando", T0.AddMinutes(10));
        original.AguardarCliente(_atendente, T0.AddMinutes(20));
        var id = await Persistir(original);

        using var sessao = pg.AbrirSessao(_tenant);
        var t = await sessao.Repo.ObterPorIdAsync(id);

        Assert.NotNull(t);
        Assert.Equal(_tenant, t.TenantId);
        Assert.Equal(_cliente, t.SolicitanteId);
        Assert.Equal(_atendente, t.ResponsavelId);
        Assert.Equal("Sistema não carrega", t.Titulo);
        Assert.Equal(Prioridade.Critica, t.Prioridade);
        Assert.Equal(StatusTicket.AguardandoCliente, t.Status);
        Assert.Equal(new PrazosSla(TimeSpan.FromMinutes(15), TimeSpan.FromHours(4)), t.Sla);
        Assert.Equal(original.PrazoPrimeiraResposta, t.PrazoPrimeiraResposta);
        Assert.Equal(original.PrazoResolucao, t.PrazoResolucao);
        Assert.Equal(T0.AddMinutes(10), t.PrimeiraRespostaEm);
        Assert.Equal(T0.AddMinutes(20), t.PausadoDesde);
        Assert.Null(t.ResolvidoEm);
        Assert.Equal(["Urgente", "Verificando"], t.Comentarios.Select(c => c.Texto));
    }

    [FactPostgres]
    public async Task Comentar_em_ticket_ja_carregado_persiste_o_novo_comentario()
    {
        var ticket = NovoTicket();
        ticket.Comentar(_cliente, "Primeiro", T0.AddMinutes(1));
        var id = await Persistir(ticket);

        using (var sessao = pg.AbrirSessao(_tenant))
        {
            var t = await sessao.Repo.ObterPorIdAsync(id);
            t!.Comentar(_atendente, "Segundo", T0.AddMinutes(2));
            await sessao.Repo.SalvarAsync();
        }

        using var conferencia = pg.AbrirSessao(_tenant);
        var recarregado = await conferencia.Repo.ObterPorIdAsync(id);
        Assert.Equal(["Primeiro", "Segundo"], recarregado!.Comentarios.Select(c => c.Texto));
    }

    [FactPostgres]
    public async Task Carrega_comentarios_em_ordem_cronologica()
    {
        var ticket = NovoTicket();
        ticket.Comentar(_cliente, "tarde", T0.AddMinutes(30));
        ticket.Comentar(_cliente, "cedo", T0.AddMinutes(10));
        ticket.Comentar(_cliente, "meio", T0.AddMinutes(20));
        var id = await Persistir(ticket);

        using var sessao = pg.AbrirSessao(_tenant);
        var t = await sessao.Repo.ObterPorIdAsync(id);

        Assert.Equal(["cedo", "meio", "tarde"], t!.Comentarios.Select(c => c.Texto));
    }

    [FactPostgres]
    public async Task Outro_tenant_nao_enxerga_o_ticket()
    {
        var id = await Persistir(NovoTicket());

        using var doutraEmpresa = pg.AbrirSessao(Guid.NewGuid());
        Assert.Null(await doutraEmpresa.Repo.ObterPorIdAsync(id));

        using var daMesmaEmpresa = pg.AbrirSessao(_tenant);
        Assert.NotNull(await daMesmaEmpresa.Repo.ObterPorIdAsync(id));
    }

    [FactPostgres]
    public void Nao_adiciona_ticket_de_outro_tenant()
    {
        var ticketDeOutraEmpresa = NovoTicket(Guid.NewGuid());

        using var sessao = pg.AbrirSessao(_tenant);

        Assert.Throws<InvalidOperationException>(() => sessao.Repo.Adicionar(ticketDeOutraEmpresa));
    }

    [FactPostgres]
    public async Task Dois_atendentes_nao_assumem_o_mesmo_ticket()
    {
        var id = await Persistir(NovoTicket());

        using var sessao1 = pg.AbrirSessao(_tenant);
        using var sessao2 = pg.AbrirSessao(_tenant);
        var t1 = await sessao1.Repo.ObterPorIdAsync(id);
        var t2 = await sessao2.Repo.ObterPorIdAsync(id);

        t1!.Assumir(_atendente, T0.AddMinutes(1));
        await sessao1.Repo.SalvarAsync();

        t2!.Assumir(Guid.NewGuid(), T0.AddMinutes(1));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => sessao2.Repo.SalvarAsync());
    }

    [FactPostgres]
    public async Task Listar_sem_filtro_traz_todos_os_tickets_do_tenant_mais_recentes_primeiro()
    {
        var primeiro = NovoTicket();
        await Persistir(primeiro);
        var maisRecente = Ticket.Abrir(_tenant, _cliente, "Outro chamado", "Descrição", Prioridade.Baixa, T0.AddMinutes(1));
        await Persistir(maisRecente);

        using var sessao = pg.AbrirSessao(_tenant);
        var tickets = await sessao.Repo.ListarAsync(apenasDoSolicitante: null);

        Assert.Equal([maisRecente.Id, primeiro.Id], tickets.Select(t => t.Id));
    }

    [FactPostgres]
    public async Task Listar_com_filtro_traz_so_os_tickets_do_solicitante()
    {
        var outroCliente = Guid.NewGuid();
        var ticketDoCliente = NovoTicket();
        await Persistir(ticketDoCliente);
        var ticketDeOutroCliente = Ticket.Abrir(_tenant, outroCliente, "t", "d", Prioridade.Baixa, T0);
        await Persistir(ticketDeOutroCliente);

        using var sessao = pg.AbrirSessao(_tenant);
        var tickets = await sessao.Repo.ListarAsync(_cliente);

        var ticket = Assert.Single(tickets);
        Assert.Equal(ticketDoCliente.Id, ticket.Id);
    }

    [FactPostgres]
    public async Task Listar_nao_traz_tickets_de_outro_tenant()
    {
        await Persistir(NovoTicket());

        using var deOutraEmpresa = pg.AbrirSessao(Guid.NewGuid());
        var tickets = await deOutraEmpresa.Repo.ListarAsync(apenasDoSolicitante: null);

        Assert.Empty(tickets);
    }

    [FactPostgres]
    public async Task Enums_sao_gravados_como_texto()
    {
        var id = await Persistir(NovoTicket());

        await using var conn = new NpgsqlConnection(pg.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("select status, prioridade from tickets where id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("Aberto", reader.GetString(0));
        Assert.Equal("Critica", reader.GetString(1));
    }
}
