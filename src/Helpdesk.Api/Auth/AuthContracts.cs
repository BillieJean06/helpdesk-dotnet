namespace Helpdesk.Api.Auth;

/// <summary>
/// <paramref name="TenantId"/> identifica a empresa. Hoje é um Guid explícito porque ainda
/// não existe um fluxo de escolha de empresa (subdomínio, convite, código); é um placeholder
/// consciente para isso, não a solução final — ver "Multi-tenancy" no roadmap do README.
/// </summary>
public sealed record LoginRequest(Guid TenantId, string Email, string Senha);

public sealed record LoginResponse(string Token, DateTimeOffset ExpiraEm);
