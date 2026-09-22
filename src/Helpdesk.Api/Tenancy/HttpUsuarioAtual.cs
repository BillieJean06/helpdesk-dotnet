using System.IdentityModel.Tokens.Jwt;
using Helpdesk.Application.Abstractions;

namespace Helpdesk.Api.Tenancy;

/// <summary>Lê o id do usuário do claim <c>sub</c> do JWT.</summary>
internal sealed class HttpUsuarioAtual(IHttpContextAccessor accessor) : IUsuarioAtual
{
    public Guid UsuarioId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id
            : throw new InvalidOperationException("Usuário não identificado na requisição.");
}
