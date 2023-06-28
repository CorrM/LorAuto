using LorAuto.Card;

namespace LorAuto.Strategies;

public abstract class Strategy
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mulliganCards"></param>
    /// <returns>List of cards to replace</returns>
    public abstract IEnumerable<InGameCard> Mulligan(IEnumerable<InGameCard> mulliganCards);
}
