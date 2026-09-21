namespace Helpdesk.Domain.Suporte;

/// <summary>Regra que traduz uma <see cref="Prioridade"/> em prazos de SLA.</summary>
public sealed class PoliticaSla
{
    private readonly Dictionary<Prioridade, PrazosSla> _prazos;

    public static PoliticaSla Padrao { get; } = new(new Dictionary<Prioridade, PrazosSla>
    {
        [Prioridade.Critica] = new(TimeSpan.FromMinutes(15), TimeSpan.FromHours(4)),
        [Prioridade.Alta] = new(TimeSpan.FromHours(1), TimeSpan.FromHours(8)),
        [Prioridade.Media] = new(TimeSpan.FromHours(4), TimeSpan.FromHours(24)),
        [Prioridade.Baixa] = new(TimeSpan.FromHours(8), TimeSpan.FromHours(72))
    });

    public PoliticaSla(IReadOnlyDictionary<Prioridade, PrazosSla> prazos)
    {
        ArgumentNullException.ThrowIfNull(prazos);

        foreach (var prioridade in Enum.GetValues<Prioridade>())
        {
            if (!prazos.ContainsKey(prioridade))
                throw new DomainException($"A política de SLA não define prazos para a prioridade {prioridade}.");
        }

        _prazos = new Dictionary<Prioridade, PrazosSla>(prazos);
    }

    public PrazosSla Para(Prioridade prioridade) =>
        _prazos.TryGetValue(prioridade, out var prazos)
            ? prazos
            : throw new DomainException("Prioridade inválida.");
}
