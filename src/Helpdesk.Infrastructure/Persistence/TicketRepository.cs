using Helpdesk.Application.Abstractions;
using Helpdesk.Domain.Suporte;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Infrastructure.Persistence;

internal sealed class TicketRepository(HelpdeskDbContext db, ITenantContext tenant) : ITicketRepository
{
    public Task<Ticket?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Tickets
            .Include(t => t.Comentarios.OrderBy(c => c.CriadoEm))
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Ticket>> ListarAsync(Guid? apenasDoSolicitante, CancellationToken ct = default)
    {
        var query = db.Tickets.AsQueryable();
        if (apenasDoSolicitante is { } solicitanteId)
            query = query.Where(t => t.SolicitanteId == solicitanteId);

        return await query.OrderByDescending(t => t.CriadoEm).ToListAsync(ct);
    }

    public void Adicionar(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        // Defesa em profundidade: o filtro global protege leituras, isto protege gravações.
        if (ticket.TenantId != tenant.TenantId)
            throw new InvalidOperationException("Não é possível adicionar um ticket de outro tenant.");

        db.Tickets.Add(ticket);
    }

    // DbUpdateConcurrencyException propaga de propósito: quem chama decide como tratar o conflito.
    public Task SalvarAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
