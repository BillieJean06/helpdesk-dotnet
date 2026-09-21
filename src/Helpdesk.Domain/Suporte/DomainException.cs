namespace Helpdesk.Domain.Suporte;

/// <summary>Violação de uma regra de negócio ou invariante do domínio.</summary>
public sealed class DomainException(string message) : Exception(message);
