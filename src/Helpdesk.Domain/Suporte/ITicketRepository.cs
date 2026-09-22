namespace Helpdesk.Domain.Suporte;

/// <summary>
/// Porta de persistência do agregado <see cref="Ticket"/>. Pertence ao domínio;
/// a implementação (EF Core) mora na Infrastructure.
/// </summary>
public interface ITicketRepository
{
    /// <summary>Carrega o agregado completo (com comentários) do tenant atual, ou null.</summary>
    Task<Ticket?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Marca o ticket para inclusão; só persiste em <see cref="SalvarAsync"/>.</summary>
    void Adicionar(Ticket ticket);

    /// <summary>Persiste, em uma única transação, tudo que mudou no agregado.</summary>
    Task SalvarAsync(CancellationToken ct = default);
}
