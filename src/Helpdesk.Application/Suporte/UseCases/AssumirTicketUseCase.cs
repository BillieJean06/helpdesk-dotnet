using Helpdesk.Application.Abstractions;
using Helpdesk.Domain.Suporte;

namespace Helpdesk.Application.Suporte.UseCases;

/// <summary>Caso de uso: um atendente assume um ticket da fila.</summary>
public sealed class AssumirTicketUseCase(ITicketRepository repositorio, TimeProvider relogio, IUsuarioAtual usuario)
{
    /// <returns>false se o ticket não existe (ou não pertence ao tenant do atendente).</returns>
    /// <exception cref="DomainException">Transição inválida para o estado atual do ticket.</exception>
    public async Task<bool> ExecutarAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await repositorio.ObterPorIdAsync(ticketId, ct);
        if (ticket is null) return false;

        ticket.Assumir(usuario.UsuarioId, relogio.GetUtcNow());
        await repositorio.SalvarAsync(ct);

        return true;
    }
}
