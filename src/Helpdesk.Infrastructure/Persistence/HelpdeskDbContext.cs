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

        // O Identity cria, por padrão, um índice único global em NormalizedUserName. Aqui a
        // unicidade é por tenant (duas empresas podem ter cada uma um usuário com o mesmo
        // e-mail/username), então trocamos pelo índice composto — em conjunto com o
        // TenantAwareUserValidator, que faz a mesma checagem na camada de aplicação.
        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.HasIndex(u => u.NormalizedUserName).IsUnique(false);
            b.HasIndex(u => new { u.TenantId, u.NormalizedEmail }).IsUnique();
        });

        // Isolamento multi-tenant: nenhuma consulta enxerga tickets de outro tenant.
        // ApplicationUser não tem esse filtro: login acontece antes de o tenant ser conhecido.
        modelBuilder.Entity<Ticket>().HasQueryFilter(t => t.TenantId == TenantAtual);
    }
}
