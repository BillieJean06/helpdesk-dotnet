using Helpdesk.Application.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Helpdesk.Infrastructure.Identity;

/// <summary>
/// Cria os papéis e um usuário de exemplo por papel, só para ambiente de desenvolvimento.
/// As credenciais são públicas de propósito (README), como o docker-compose: servem só
/// para rodar o projeto localmente, nunca seriam usadas assim em produção.
/// </summary>
public static class IdentitySeeder
{
    public const string SenhaDemo = "Demo123$";

    private static readonly (string Email, string Nome, string Papel)[] Usuarios =
    [
        ("cliente@helpdesk.local", "Cliente Demo", Papeis.Cliente),
        ("atendente@helpdesk.local", "Atendente Demo", Papeis.Atendente),
        ("supervisor@helpdesk.local", "Supervisor Demo", Papeis.Supervisor)
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var papel in Papeis.Todos)
        {
            if (!await roleManager.RoleExistsAsync(papel))
                await roleManager.CreateAsync(new IdentityRole<Guid>(papel));
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var (email, nome, papel) in Usuarios)
        {
            var normalizedEmail = userManager.NormalizeEmail(email);
            var jaExiste = await userManager.Users.AnyAsync(
                u => u.TenantId == TenantsDemo.Empresa && u.NormalizedEmail == normalizedEmail, ct);
            if (jaExiste) continue;

            var usuario = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Nome = nome,
                TenantId = TenantsDemo.Empresa
            };

            var resultado = await userManager.CreateAsync(usuario, SenhaDemo);
            if (!resultado.Succeeded)
            {
                var erros = string.Join("; ", resultado.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Falha ao semear o usuário {email}: {erros}");
            }

            await userManager.AddToRoleAsync(usuario, papel);
        }
    }
}
