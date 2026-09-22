namespace Helpdesk.Infrastructure.Identity;

/// <summary>Empresa fixa usada pelo <see cref="IdentitySeeder"/> em ambiente de desenvolvimento.</summary>
public static class TenantsDemo
{
    public static readonly Guid Empresa = Guid.Parse("11111111-1111-1111-1111-111111111111");
}
