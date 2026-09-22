using Helpdesk.Application.Suporte.UseCases;
using Helpdesk.Domain.Suporte;
using Microsoft.Extensions.Time.Testing;

namespace Helpdesk.Application.Tests;

public class ListarTicketsUseCaseTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero));
    private readonly FakeTicketRepository _repositorio = new();

    private Ticket NovoTicket(Guid solicitante) =>
        Ticket.Abrir(Tenant, solicitante, "t", "d", Prioridade.Media, _relogio.GetUtcNow());

    [Fact]
    public async Task Sem_filtro_lista_todos_os_tickets()
    {
        var clienteA = Guid.NewGuid();
        var clienteB = Guid.NewGuid();
        _repositorio.Adicionar(NovoTicket(clienteA));
        _repositorio.Adicionar(NovoTicket(clienteB));

        var tickets = await new ListarTicketsUseCase(_repositorio).ExecutarAsync(apenasDoSolicitante: null);

        Assert.Equal(2, tickets.Count);
    }

    [Fact]
    public async Task Com_filtro_lista_so_os_tickets_do_solicitante()
    {
        var clienteA = Guid.NewGuid();
        var clienteB = Guid.NewGuid();
        var ticketDoA = NovoTicket(clienteA);
        _repositorio.Adicionar(ticketDoA);
        _repositorio.Adicionar(NovoTicket(clienteB));

        var tickets = await new ListarTicketsUseCase(_repositorio).ExecutarAsync(clienteA);

        var ticket = Assert.Single(tickets);
        Assert.Equal(ticketDoA.Id, ticket.Id);
    }
}
