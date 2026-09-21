namespace Helpdesk.Domain.Suporte;

/// <summary>Entidade filha do <see cref="Ticket"/>: só é criada por ele.</summary>
public sealed class Comentario
{
    public Guid Id { get; private set; }
    public Guid AutorId { get; private set; }
    public string Texto { get; private set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; private set; }

    private Comentario() { } // EF Core

    internal Comentario(Guid autorId, string texto, DateTimeOffset criadoEm)
    {
        Id = Guid.NewGuid();
        AutorId = autorId;
        Texto = texto;
        CriadoEm = criadoEm;
    }
}
