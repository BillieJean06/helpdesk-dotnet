using Microsoft.AspNetCore.Identity;

namespace Helpdesk.Infrastructure.Identity;

/// <summary>
/// Usuário de autenticação. Deliberadamente fora do Domain: identidade é um contexto
/// separado, e o agregado Ticket só guarda Guids (SolicitanteId, ResponsavelId).
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public required string Nome { get; set; }
    public required Guid TenantId { get; set; }
}
