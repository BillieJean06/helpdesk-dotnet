using Helpdesk.Domain.Suporte;
using Helpdesk.Infrastructure.Identity;
using Helpdesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Helpdesk.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Requer um <c>ITenantContext</c> e um <c>IUsuarioAtual</c> registrados pelo host.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<HelpdeskDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<ITicketRepository, TicketRepository>();

        // AddIdentityCore (não AddIdentity): sem cookies, sem esquema de sign-in próprio.
        // A Api emite o JWT; aqui só entram UserManager/RoleManager e o hashing de senha.
        //
        // Sem AddDefaultTokenProviders(): ele cadastra os provedores de token para reset de
        // senha e confirmação de e-mail, que fazem parte do Microsoft.AspNetCore.Identity
        // completo (shared framework do ASP.NET Core), não desses pacotes NuGet "puros",
        // usáveis numa classlib comum. Sem essas funcionalidades no escopo atual, evito
        // acoplar a Infrastructure ao SDK Web só por causa disso.
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<HelpdeskDbContext>();

        // O validador padrão do Identity checa unicidade de username/e-mail na tabela inteira.
        // Substituímos pelo tenant-aware: duas empresas podem ter cada uma um usuário com o
        // mesmo e-mail, sem colidir.
        services.RemoveAll<IUserValidator<ApplicationUser>>();
        services.AddScoped<IUserValidator<ApplicationUser>, TenantAwareUserValidator>();

        return services;
    }
}
