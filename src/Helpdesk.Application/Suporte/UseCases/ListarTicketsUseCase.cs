using Helpdesk.Domain.Suporte;

namespace Helpdesk.Application.Suporte.UseCases;

/// <summary>Caso de uso: fila de tickets do tenant, ou só os de um solicitante.
/// Quem decide "cliente vê só o próprio" é a Api — é autorização, não regra do agregado.</summary>
public sealed class ListarTicketsUseCase(ITicketRepository repositorio)
{
    public Task<IReadOnlyList<Ticket>> ExecutarAsync(Guid? apenasDoSolicitante, CancellationToken ct = default) =>
        repositorio.ListarAsync(apenasDoSolicitante, ct);
}
