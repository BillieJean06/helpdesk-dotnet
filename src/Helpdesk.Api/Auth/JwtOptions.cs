namespace Helpdesk.Api.Auth;

/// <summary>Vinculada à seção "Jwt" da configuração. A chave de assinatura é secreta:
/// vem de user-secrets ou variável de ambiente, nunca de appsettings.json.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string Key { get; init; }
    public int ExpiracaoMinutos { get; init; } = 60;
}
