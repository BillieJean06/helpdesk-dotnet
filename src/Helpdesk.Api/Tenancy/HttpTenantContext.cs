using Helpdesk.Application.Abstractions;

namespace Helpdesk.Api.Tenancy;

/// <summary>
/// Lê o tenant do claim <c>tenant_id</c> do usuário autenticado. Sem claim válido, falha:
/// nunca assume um tenant padrão, para não vazar dados entre empresas.
/// </summary>
internal sealed class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    public const string ClaimType = "tenant_id";

    public Guid TenantId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirst(ClaimType)?.Value, out var id) && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("Tenant não identificado na requisição.");
}
