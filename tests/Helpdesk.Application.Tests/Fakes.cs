using Helpdesk.Application.Abstractions;
using Helpdesk.Domain.Suporte;

namespace Helpdesk.Application.Tests;

/// <summary>Repositório em memória: testa os casos de uso sem tocar em banco de dados.</summary>
internal sealed class FakeTicketRepository : ITicketRepository
{
    private readonly Dictionary<Guid, Ticket> _tickets = [];

    public int VezesSalvo { get; private set; }

    public Task<Ticket?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_tickets.GetValueOrDefault(id));

    public Task<IReadOnlyList<Ticket>> ListarAsync(Guid? apenasDoSolicitante, CancellationToken ct = default)
    {
        IEnumerable<Ticket> query = _tickets.Values.OrderByDescending(t => t.CriadoEm);
        if (apenasDoSolicitante is { } solicitanteId)
            query = query.Where(t => t.SolicitanteId == solicitanteId);

        return Task.FromResult<IReadOnlyList<Ticket>>(query.ToList());
    }

    public void Adicionar(Ticket ticket) => _tickets[ticket.Id] = ticket;

    public Task SalvarAsync(CancellationToken ct = default)
    {
        VezesSalvo++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeTenantContext(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;
}

internal sealed class FakeUsuarioAtual(Guid usuarioId) : IUsuarioAtual
{
    public Guid UsuarioId => usuarioId;
}
