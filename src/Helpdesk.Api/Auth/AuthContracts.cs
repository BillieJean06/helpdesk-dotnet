namespace Helpdesk.Api.Auth;

public sealed record LoginRequest(string Email, string Senha);

public sealed record LoginResponse(string Token, DateTimeOffset ExpiraEm);
