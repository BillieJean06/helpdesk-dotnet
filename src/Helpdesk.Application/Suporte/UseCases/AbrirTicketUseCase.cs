using Helpdesk.Application.Abstractions;
using Helpdesk.Domain.Suporte;

namespace Helpdesk.Application.Suporte.UseCases;

/// <summary>Caso de uso: o cliente autenticado abre um novo chamado.</summary>
public sealed class AbrirTicketUseCase(
    ITicketRepository repositorio,
    TimeProvider relogio,
    ITenantContext tenant,
    IUsuarioAtual usuario)
{
    public async Task<Guid> ExecutarAsync(
        string titulo, string descricao, Prioridade prioridade, CancellationToken ct = default)
    {
        var ticket = Ticket.Abrir(
            tenant.TenantId, usuario.UsuarioId, titulo, descricao, prioridade, relogio.GetUtcNow());

        repositorio.Adicionar(ticket);
        await repositorio.SalvarAsync(ct);

        return ticket.Id;
    }
}
