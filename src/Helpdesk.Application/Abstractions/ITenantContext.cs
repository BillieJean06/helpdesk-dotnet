namespace Helpdesk.Application.Abstractions;

/// <summary>Tenant (empresa cliente) da requisição em curso.</summary>
public interface ITenantContext
{
    /// <exception cref="InvalidOperationException">Não há tenant identificado na requisição.</exception>
    Guid TenantId { get; }
}
