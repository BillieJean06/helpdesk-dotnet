using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Helpdesk.Api.Tenancy;
using Helpdesk.Infrastructure.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Helpdesk.Api.Auth;

/// <summary>Emite o JWT de sessão: identidade (sub), tenant e papéis, tudo que
/// <see cref="HttpTenantContext"/>, <see cref="HttpUsuarioAtual"/> e [Authorize(Roles=...)] leem depois.</summary>
public sealed class JwtTokenService(JwtOptions _options, TimeProvider relogio)
{
    public (string Token, DateTimeOffset ExpiraEm) Gerar(ApplicationUser usuario, IEnumerable<string> papeis)
    {
        var agora = relogio.GetUtcNow();
        var expiraEm = agora.AddMinutes(_options.ExpiracaoMinutos);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email!),
            new(HttpTenantContext.ClaimType, usuario.TenantId.ToString()),
            .. papeis.Select(p => new Claim(ClaimTypes.Role, p))
        ];

        var credenciais = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: agora.UtcDateTime,
            expires: expiraEm.UtcDateTime,
            signingCredentials: credenciais);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}
