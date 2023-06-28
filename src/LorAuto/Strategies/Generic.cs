using LorAuto.Card;

namespace LorAuto.Strategies;

public sealed class Generic : Strategy
{
    public override IEnumerable<InGameCard> Mulligan(IEnumerable<InGameCard> mulliganCards)
    {
        return mulliganCards.Where(c => c.Cost > 3);
    }
}
