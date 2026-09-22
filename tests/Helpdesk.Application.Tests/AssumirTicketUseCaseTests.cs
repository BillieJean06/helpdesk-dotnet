using Helpdesk.Application.Suporte.UseCases;
using Helpdesk.Domain.Suporte;
using Microsoft.Extensions.Time.Testing;

namespace Helpdesk.Application.Tests;

public class AssumirTicketUseCaseTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Cliente = Guid.NewGuid();
    private static readonly Guid Atendente = Guid.NewGuid();
    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero));
    private readonly FakeTicketRepository _repositorio = new();

    private AssumirTicketUseCase CriarUseCase() => new(_repositorio, _relogio, new FakeUsuarioAtual(Atendente));

    private Ticket TicketAberto()
    {
        var t = Ticket.Abrir(Tenant, Cliente, "t", "d", Prioridade.Media, _relogio.GetUtcNow());
        _repositorio.Adicionar(t);
        return t;
    }

    [Fact]
    public async Task Assume_o_ticket_com_o_usuario_do_contexto_e_o_relogio_injetado()
    {
        var ticket = TicketAberto();

        var assumiu = await CriarUseCase().ExecutarAsync(ticket.Id);

        Assert.True(assumiu);
        Assert.Equal(StatusTicket.EmAtendimento, ticket.Status);
        Assert.Equal(Atendente, ticket.ResponsavelId);
    }

    [Fact]
    public async Task Retorna_false_para_ticket_inexistente()
    {
        var assumiu = await CriarUseCase().ExecutarAsync(Guid.NewGuid());

        Assert.False(assumiu);
    }

    [Fact]
    public async Task Propaga_a_violacao_de_regra_ao_assumir_ticket_ja_em_atendimento()
    {
        var ticket = TicketAberto();
        ticket.Assumir(Guid.NewGuid(), _relogio.GetUtcNow());

        await Assert.ThrowsAsync<DomainException>(() => CriarUseCase().ExecutarAsync(ticket.Id));
    }
}
