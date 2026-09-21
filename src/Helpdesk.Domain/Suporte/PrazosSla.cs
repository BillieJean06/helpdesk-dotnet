namespace Helpdesk.Domain.Suporte;

/// <summary>
/// Prazos de SLA contratados para um ticket. Fica gravado no ticket na abertura:
/// mudar a <see cref="PoliticaSla"/> depois não altera tickets já abertos.
/// </summary>
public sealed record PrazosSla
{
    public TimeSpan PrimeiraResposta { get; }
    public TimeSpan Resolucao { get; }

    public PrazosSla(TimeSpan primeiraResposta, TimeSpan resolucao)
    {
        if (primeiraResposta <= TimeSpan.Zero)
            throw new DomainException("Prazo de primeira resposta deve ser positivo.");
        if (resolucao < primeiraResposta)
            throw new DomainException("Prazo de resolução não pode ser menor que o de primeira resposta.");

        PrimeiraResposta = primeiraResposta;
        Resolucao = resolucao;
    }
}
