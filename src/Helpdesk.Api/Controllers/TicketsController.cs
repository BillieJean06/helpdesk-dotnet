using Helpdesk.Application.Abstractions;
using Helpdesk.Application.Suporte.Dtos;
using Helpdesk.Application.Suporte.UseCases;
using Helpdesk.Domain.Suporte;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Api.Controllers;

public sealed record AbrirTicketRequest(string Titulo, string Descricao, Prioridade Prioridade);
public sealed record ComentarTicketRequest(string Texto);

[ApiController]
[Route("api/tickets")]
[Authorize]
public sealed class TicketsController(
    AbrirTicketUseCase abrirTicket,
    AssumirTicketUseCase assumirTicket,
    ObterTicketUseCase obterTicket,
    ListarTicketsUseCase listarTickets,
    ComentarTicketUseCase comentarTicket,
    IUsuarioAtual usuarioAtual) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TicketResumoDto>>> Listar(CancellationToken ct)
    {
        // Cliente só vê a própria fila; atendente e supervisor veem a fila inteira do tenant.
        var apenasDoSolicitante = EhAtendenteOuSupervisor() ? (Guid?)null : usuarioAtual.UsuarioId;

        var tickets = await listarTickets.ExecutarAsync(apenasDoSolicitante, ct);
        return tickets.Select(TicketResumoDto.De).ToList();
    }

    [HttpPost]
    [Authorize(Roles = Papeis.Cliente)]
    public async Task<ActionResult<Guid>> Abrir(AbrirTicketRequest request, CancellationToken ct)
    {
        var id = await abrirTicket.ExecutarAsync(request.Titulo, request.Descricao, request.Prioridade, ct);
        return CreatedAtAction(nameof(Obter), new { id }, id);
    }

    [HttpPost("{id:guid}/assumir")]
    [Authorize(Roles = Papeis.Atendente)]
    public async Task<IActionResult> Assumir(Guid id, CancellationToken ct)
    {
        var assumiu = await assumirTicket.ExecutarAsync(id, ct);
        return assumiu ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketDetalheDto>> Obter(Guid id, CancellationToken ct)
    {
        var ticket = await obterTicket.ExecutarAsync(id, ct);
        if (ticket is null) return NotFound();
        if (!TemAcessoAoTicket(ticket)) return Forbid();

        return TicketDetalheDto.De(ticket);
    }

    [HttpPost("{id:guid}/comentarios")]
    public async Task<IActionResult> Comentar(Guid id, ComentarTicketRequest request, CancellationToken ct)
    {
        // Duas idas ao banco (uma para checar acesso, outra dentro do caso de uso): aceitável
        // aqui pela simplicidade — não é um caminho de alto volume neste projeto de estudo.
        var ticket = await obterTicket.ExecutarAsync(id, ct);
        if (ticket is null) return NotFound();
        if (!TemAcessoAoTicket(ticket)) return Forbid();

        var comentou = await comentarTicket.ExecutarAsync(id, request.Texto, ct);
        return comentou ? NoContent() : NotFound();
    }

    // Cliente só acessa o próprio chamado; atendente e supervisor acessam qualquer um do tenant
    // (o filtro global do EF já garante que é do tenant certo).
    private bool TemAcessoAoTicket(Ticket ticket) =>
        EhAtendenteOuSupervisor() || ticket.SolicitanteId == usuarioAtual.UsuarioId;

    private bool EhAtendenteOuSupervisor() =>
        User.IsInRole(Papeis.Atendente) || User.IsInRole(Papeis.Supervisor);
}
