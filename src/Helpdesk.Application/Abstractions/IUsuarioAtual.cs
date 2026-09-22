namespace Helpdesk.Application.Abstractions;

/// <summary>Usuário autenticado na requisição em curso.</summary>
public interface IUsuarioAtual
{
    /// <exception cref="InvalidOperationException">Não há usuário autenticado na requisição.</exception>
    Guid UsuarioId { get; }
}
