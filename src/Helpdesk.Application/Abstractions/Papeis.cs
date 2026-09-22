namespace Helpdesk.Application.Abstractions;

/// <summary>
/// Papéis de quem usa o sistema. Autorização (quem pode fazer o quê) é decisão da
/// Application/Api; o agregado <see cref="Domain.Suporte.Ticket"/> não conhece papel algum,
/// só protege consistência (ex.: "apenas o responsável resolve").
/// </summary>
public static class Papeis
{
    public const string Cliente = "Cliente";
    public const string Atendente = "Atendente";
    public const string Supervisor = "Supervisor";

    public static readonly IReadOnlyList<string> Todos = [Cliente, Atendente, Supervisor];
}
