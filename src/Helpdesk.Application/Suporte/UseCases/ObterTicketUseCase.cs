using Helpdesk.Domain.Suporte;

namespace Helpdesk.Application.Suporte.UseCases;

/// <summary>Caso de uso: consulta de um ticket. A checagem de "quem pode ver" fica na Api,
/// porque depende do papel de quem pergunta, não é invariante do agregado.</summary>
public sealed class ObterTicketUseCase(ITicketRepository repositorio)
{
    public Task<Ticket?> ExecutarAsync(Guid ticketId, CancellationToken ct = default) =>
        repositorio.ObterPorIdAsync(ticketId, ct);
}
