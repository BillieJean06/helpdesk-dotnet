using Helpdesk.Application.Suporte.UseCases;
using Helpdesk.Domain.Suporte;
using Microsoft.Extensions.Time.Testing;

namespace Helpdesk.Application.Tests;

public class AbrirTicketUseCaseTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Cliente = Guid.NewGuid();
    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero));
    private readonly FakeTicketRepository _repositorio = new();

    private AbrirTicketUseCase CriarUseCase() =>
        new(_repositorio, _relogio, new FakeTenantContext(Tenant), new FakeUsuarioAtual(Cliente));

    [Fact]
    public async Task Cria_o_ticket_com_o_tenant_e_solicitante_do_contexto_da_requisicao()
    {
        var id = await CriarUseCase().ExecutarAsync("Sistema não carrega", "Tela branca", Prioridade.Alta);

        var ticket = await _repositorio.ObterPorIdAsync(id);
        Assert.NotNull(ticket);
        Assert.Equal(Tenant, ticket.TenantId);
        Assert.Equal(Cliente, ticket.SolicitanteId);
        Assert.Equal(StatusTicket.Aberto, ticket.Status);
    }

    [Fact]
    public async Task Usa_o_relogio_injetado_para_calcular_os_prazos_de_sla()
    {
        var id = await CriarUseCase().ExecutarAsync("t", "d", Prioridade.Alta);

        var ticket = await _repositorio.ObterPorIdAsync(id);
        Assert.Equal(_relogio.GetUtcNow().AddHours(1), ticket!.PrazoPrimeiraResposta);
        Assert.Equal(_relogio.GetUtcNow().AddHours(8), ticket.PrazoResolucao);
    }

    [Fact]
    public async Task Persiste_o_ticket_no_repositorio()
    {
        await CriarUseCase().ExecutarAsync("t", "d", Prioridade.Baixa);

        Assert.Equal(1, _repositorio.VezesSalvo);
    }

    [Fact]
    public async Task Propaga_a_violacao_de_regra_de_dominio_sem_persistir()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            CriarUseCase().ExecutarAsync("", "d", Prioridade.Baixa));

        Assert.Equal(0, _repositorio.VezesSalvo);
    }
}
