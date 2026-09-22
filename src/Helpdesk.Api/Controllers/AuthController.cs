using Helpdesk.Api.Auth;
using Helpdesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(UserManager<ApplicationUser> userManager, JwtTokenService tokenService)
    : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        // Não usa FindByEmailAsync: ele procura por e-mail em todos os tenants. O login
        // precisa da empresa (TenantId) para não confundir usuários de empresas diferentes
        // que, agora, podem ter o mesmo e-mail.
        var normalizedEmail = userManager.NormalizeEmail(request.Email);
        var usuario = await userManager.Users.FirstOrDefaultAsync(
            u => u.TenantId == request.TenantId && u.NormalizedEmail == normalizedEmail, ct);

        if (usuario is null || !await userManager.CheckPasswordAsync(usuario, request.Senha))
            return Unauthorized();

        var papeis = await userManager.GetRolesAsync(usuario);
        var (token, expiraEm) = tokenService.Gerar(usuario, papeis);

        return Ok(new LoginResponse(token, expiraEm));
    }
}
