using Helpdesk.Application.Abstractions;
using Helpdesk.Application.Suporte.Dtos;
using Helpdesk.Application.Suporte.UseCases;
using Helpdesk.Domain.Suporte;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Api.Controllers;

public sealed record AbrirTicketRequest(string Titulo, string Descricao, Prioridade Prioridade);

[ApiController]
[Route("api/tickets")]
[Authorize]
public sealed class TicketsController(
    AbrirTicketUseCase abrirTicket,
    AssumirTicketUseCase assumirTicket,
    ObterTicketUseCase obterTicket,
    IUsuarioAtual usuarioAtual) : ControllerBase
{
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

        // Cliente só vê o próprio chamado; atendente e supervisor veem qualquer um do tenant
        // (o filtro global do EF já garante que é do tenant certo).
        var podeVer = User.IsInRole(Papeis.Atendente) || User.IsInRole(Papeis.Supervisor)
            || ticket.SolicitanteId == usuarioAtual.UsuarioId;

        if (!podeVer) return Forbid();

        return TicketDetalheDto.De(ticket);
    }
}
