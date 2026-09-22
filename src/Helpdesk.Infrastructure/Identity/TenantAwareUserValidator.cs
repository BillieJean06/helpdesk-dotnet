using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Infrastructure.Identity;

/// <summary>
/// Substitui o <c>UserValidator&lt;TUser&gt;</c> padrão do Identity: ele checa unicidade de
/// username e e-mail globalmente, na tabela inteira. Aqui a checagem é por tenant — duas
/// empresas podem ter, cada uma, um usuário com o mesmo e-mail, sem colidir.
/// </summary>
internal sealed class TenantAwareUserValidator : IUserValidator<ApplicationUser>
{
    public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        List<IdentityError> erros = [];

        if (string.IsNullOrWhiteSpace(user.UserName))
        {
            erros.Add(new IdentityError { Code = "InvalidUserName", Description = "Nome de usuário é obrigatório." });
        }
        else
        {
            var permitidos = manager.Options.User.AllowedUserNameCharacters;
            var temCaractereInvalido = !string.IsNullOrEmpty(permitidos) && user.UserName.Any(c => !permitidos.Contains(c));
            if (temCaractereInvalido)
            {
                erros.Add(new IdentityError
                {
                    Code = "InvalidUserName",
                    Description = $"Nome de usuário '{user.UserName}' contém caracteres não permitidos."
                });
            }
            else
            {
                // Normalizamos aqui, sem confiar em user.NormalizedUserName: o UserManager só
                // preenche esse campo DEPOIS de rodar os validadores (é por isso que o
                // UserValidator padrão do Identity também normaliza na hora, em vez de ler
                // a propriedade já normalizada do objeto).
                var normalizedUserName = manager.NormalizeName(user.UserName);
                if (await manager.Users.AnyAsync(u =>
                    u.TenantId == user.TenantId && u.NormalizedUserName == normalizedUserName && u.Id != user.Id))
                {
                    erros.Add(new IdentityError
                    {
                        Code = "DuplicateUserName",
                        Description = $"Já existe um usuário '{user.UserName}' nesta empresa."
                    });
                }
            }
        }

        if (manager.Options.User.RequireUniqueEmail)
        {
            var normalizedEmail = manager.NormalizeEmail(user.Email);
            if (await manager.Users.AnyAsync(u =>
                u.TenantId == user.TenantId && u.NormalizedEmail == normalizedEmail && u.Id != user.Id))
            {
                erros.Add(new IdentityError
                {
                    Code = "DuplicateEmail",
                    Description = $"Já existe um usuário com o e-mail '{user.Email}' nesta empresa."
                });
            }
        }

        return erros.Count == 0 ? IdentityResult.Success : IdentityResult.Failed([.. erros]);
    }
}
