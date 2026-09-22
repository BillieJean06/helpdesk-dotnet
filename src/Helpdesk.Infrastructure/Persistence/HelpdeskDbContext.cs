using Helpdesk.Application.Abstractions;
using Helpdesk.Domain.Suporte;
using Helpdesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Infrastructure.Persistence;

/// <summary>
/// Um único DbContext para o monolito modular: agregados de Suporte e as tabelas do
/// ASP.NET Identity (usuários e papéis) convivem no mesmo banco. Separar em dois
/// DbContexts só se justificaria com bases de dados físicas diferentes.
/// </summary>
public sealed class HelpdeskDbContext(DbContextOptions<HelpdeskDbContext> options, ITenantContext tenant)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();

    // Lida a cada instância do contexto: o EF parametriza o filtro global por esta propriedade.
    private Guid TenantAtual => tenant.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // tabelas do Identity

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HelpdeskDbContext).Assembly);

        // Isolamento multi-tenant: nenhuma consulta enxerga tickets de outro tenant.
        modelBuilder.Entity<Ticket>().HasQueryFilter(t => t.TenantId == TenantAtual);
    }
}
