using Helpdesk.Application.Abstractions;
using Helpdesk.Domain.Suporte;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Infrastructure.Persistence;

public sealed class HelpdeskDbContext(DbContextOptions<HelpdeskDbContext> options, ITenantContext tenant)
    : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();

    // Lida a cada instância do contexto: o EF parametriza o filtro global por esta propriedade.
    private Guid TenantAtual => tenant.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HelpdeskDbContext).Assembly);

        // Isolamento multi-tenant: nenhuma consulta enxerga tickets de outro tenant.
        modelBuilder.Entity<Ticket>().HasQueryFilter(t => t.TenantId == TenantAtual);
    }
}
