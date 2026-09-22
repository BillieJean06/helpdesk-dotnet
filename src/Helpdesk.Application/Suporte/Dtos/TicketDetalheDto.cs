using Helpdesk.Domain.Suporte;

namespace Helpdesk.Application.Suporte.Dtos;

public sealed record ComentarioDto(Guid AutorId, string Texto, DateTimeOffset CriadoEm)
{
    public static ComentarioDto De(Comentario c) => new(c.AutorId, c.Texto, c.CriadoEm);
}

/// <summary>Visão de lista/fila: sem comentários, mais leve que o detalhe.</summary>
public sealed record TicketResumoDto(
    Guid Id,
    string Titulo,
    Prioridade Prioridade,
    StatusTicket Status,
    Guid SolicitanteId,
    Guid? ResponsavelId,
    DateTimeOffset CriadoEm,
    DateTimeOffset PrazoPrimeiraResposta,
    DateTimeOffset PrazoResolucao)
{
    public static TicketResumoDto De(Ticket t) => new(
        t.Id, t.Titulo, t.Prioridade, t.Status, t.SolicitanteId, t.ResponsavelId,
        t.CriadoEm, t.PrazoPrimeiraResposta, t.PrazoResolucao);
}

public sealed record TicketDetalheDto(
    Guid Id,
    string Titulo,
    string Descricao,
    Prioridade Prioridade,
    StatusTicket Status,
    Guid SolicitanteId,
    Guid? ResponsavelId,
    DateTimeOffset CriadoEm,
    DateTimeOffset PrazoPrimeiraResposta,
    DateTimeOffset PrazoResolucao,
    IReadOnlyCollection<ComentarioDto> Comentarios)
{
    /// <summary>Projeta o agregado para transporte; nunca expõe o <see cref="Ticket"/> em si pela API.</summary>
    public static TicketDetalheDto De(Ticket t) => new(
        t.Id,
        t.Titulo,
        t.Descricao,
        t.Prioridade,
        t.Status,
        t.SolicitanteId,
        t.ResponsavelId,
        t.CriadoEm,
        t.PrazoPrimeiraResposta,
        t.PrazoResolucao,
        t.Comentarios.Select(ComentarioDto.De).ToList());
}
