using Helpdesk.Api.Auth;
using Helpdesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
        var usuario = await userManager.FindByEmailAsync(request.Email);
        if (usuario is null || !await userManager.CheckPasswordAsync(usuario, request.Senha))
            return Unauthorized();

        var papeis = await userManager.GetRolesAsync(usuario);
        var (token, expiraEm) = tokenService.Gerar(usuario, papeis);

        return Ok(new LoginResponse(token, expiraEm));
    }
}
