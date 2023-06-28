using LorAuto.Card;
using LorAuto.Client.Model;

namespace LorAuto.Strategies;

public abstract class Strategy
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mulliganCards"></param>
    /// <returns>Cards to replace</returns>
    public abstract IEnumerable<InGameCard> Mulligan(IEnumerable<InGameCard> mulliganCards);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="boardState"></param>
    /// <param name="spellsToUse">if value of Dictionary is null then spell will target nexus</param>
    /// <param name="abilitiesToUse"></param>
    /// <returns>Your defend cards against opponent attacking cards</returns>
    /// <remarks>
    /// Returned dictionary <c>key</c> would be your card, <c>value</c> would be opponent card
    /// </remarks>
    public abstract Dictionary<InGameCard, InGameCard> Block(BoardState boardState, out Dictionary<InGameCard, IEnumerable<InGameCard>?>? spellsToUse, out IEnumerable<InGameCard>? abilitiesToUse);
}
