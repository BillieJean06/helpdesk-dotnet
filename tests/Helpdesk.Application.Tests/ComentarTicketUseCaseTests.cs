using Helpdesk.Application.Suporte.UseCases;
using Helpdesk.Domain.Suporte;
using Microsoft.Extensions.Time.Testing;

namespace Helpdesk.Application.Tests;

public class ComentarTicketUseCaseTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Cliente = Guid.NewGuid();
    private static readonly Guid Atendente = Guid.NewGuid();
    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero));
    private readonly FakeTicketRepository _repositorio = new();

    private ComentarTicketUseCase CriarUseCase(Guid autor) => new(_repositorio, _relogio, new FakeUsuarioAtual(autor));

    private Ticket TicketAberto()
    {
        var t = Ticket.Abrir(Tenant, Cliente, "t", "d", Prioridade.Media, _relogio.GetUtcNow());
        _repositorio.Adicionar(t);
        return t;
    }

    [Fact]
    public async Task Adiciona_comentario_com_o_autor_do_contexto()
    {
        var ticket = TicketAberto();

        var comentou = await CriarUseCase(Atendente).ExecutarAsync(ticket.Id, "Estou verificando");

        Assert.True(comentou);
        var comentario = Assert.Single(ticket.Comentarios);
        Assert.Equal(Atendente, comentario.AutorId);
        Assert.Equal("Estou verificando", comentario.Texto);
    }

    [Fact]
    public async Task Retorna_false_para_ticket_inexistente()
    {
        var comentou = await CriarUseCase(Atendente).ExecutarAsync(Guid.NewGuid(), "oi");

        Assert.False(comentou);
    }

    [Fact]
    public async Task Propaga_violacao_de_regra_para_comentario_vazio()
    {
        var ticket = TicketAberto();

        await Assert.ThrowsAsync<DomainException>(() => CriarUseCase(Cliente).ExecutarAsync(ticket.Id, "  "));
    }
}
