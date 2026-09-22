using Helpdesk.Application.Abstractions;
using Helpdesk.Domain.Suporte;

namespace Helpdesk.Application.Suporte.UseCases;

/// <summary>Caso de uso: cliente ou atendente comenta um ticket.</summary>
public sealed class ComentarTicketUseCase(ITicketRepository repositorio, TimeProvider relogio, IUsuarioAtual usuario)
{
    /// <returns>false se o ticket não existe (ou não pertence ao tenant de quem chama).</returns>
    /// <exception cref="DomainException">Comentário vazio, longo demais, ou ticket fechado.</exception>
    public async Task<bool> ExecutarAsync(Guid ticketId, string texto, CancellationToken ct = default)
    {
        var ticket = await repositorio.ObterPorIdAsync(ticketId, ct);
        if (ticket is null) return false;

        ticket.Comentar(usuario.UsuarioId, texto, relogio.GetUtcNow());
        await repositorio.SalvarAsync(ct);

        return true;
    }
}
