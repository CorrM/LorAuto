using LorAuto.Card;
using LorAuto.Client.Model;

namespace LorAuto.Strategies;

public sealed class Generic : Strategy
{
    public override IEnumerable<InGameCard> Mulligan(IEnumerable<InGameCard> mulliganCards)
    {
        return mulliganCards.Where(c => c.Cost > 3);
    }

    public override Dictionary<InGameCard, InGameCard> Block(BoardState boardState, out Dictionary<InGameCard, IEnumerable<InGameCard>?>? spellsToUse, out IEnumerable<InGameCard>? abilitiesToUse)
    {
        spellsToUse = null;
        abilitiesToUse = null;

        var ret = new Dictionary<InGameCard, InGameCard>();

        for (var i = 0; i < boardState.CardsBoard.Count; i++)
        {
            if (i >= boardState.OpponentCardsAttackOrBlock.Count)
                break;
            
            ret.Add(boardState.CardsBoard[i], boardState.OpponentCardsAttackOrBlock[i]);
        }
        
        return ret;
    }
}
