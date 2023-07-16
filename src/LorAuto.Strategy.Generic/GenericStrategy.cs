using LorAuto.Card.Model;
using LorAuto.Client.Model;
using LorAuto.Strategy.Model;

namespace LorAuto.Strategy.Generic;

public sealed class GenericStrategy : StrategyBase
{
    public override List<InGameCard> Mulligan(IEnumerable<InGameCard> mulliganCards)
    {
        return mulliganCards.Where(c => c.Cost > 3).ToList();
    }

    public override (InGameCard HandCard, CardTargetSelector? Target)? PlayHandCard(GameBoardData boardData, EGameState gameState, int mana, int spellMana)
    {
        InGameCard? cardToPlay = GetPlayableHandCards(boardData.Cards, mana, spellMana)
            .Where(c => c.Type != EGameCardType.Spell)
            .Where(c => c.Cost <= mana && !c.Description.StartsWith("To play me"))
            .MaxBy(c => c.Attack);

        if (cardToPlay is null)
            return null;

        // CardTargetSelector.Select(cardToPlay).Target(cardToPlay);

        return (cardToPlay, null);
    }

    public override Dictionary<InGameCard, InGameCard> Block(GameBoardData boardData, out List<CardTargetSelector>? spellsToUse)
    {
        spellsToUse = null;

        var ret = new Dictionary<InGameCard, InGameCard>();
        int opponentStartIdx = 0; // To not block same opponent card by all our cards

        // What if my cards more than opponent cards ?
        foreach (InGameCard myCard in boardData.Cards.CardsBoard)
        {
            for (int i = opponentStartIdx; i < boardData.Cards.OpponentCardsAttackOrBlock.Count; i++)
            {
                InGameCard opponent = boardData.Cards.OpponentCardsAttackOrBlock[i];

                bool isBlockable = true;
                foreach (InGameCard allyCard in boardData.Cards.CardsAttackOrBlock)
                {
                    if (Math.Abs(allyCard.TopCenterPos.X - opponent.TopCenterPos.X) >= 10)
                        continue;

                    isBlockable = false;
                    break;
                }

                if (!isBlockable)
                {
                    ++opponentStartIdx;
                    continue;
                }

                if (opponent.Keywords.Contains(EGameCardKeyword.Elusive) && !myCard.Keywords.Contains(EGameCardKeyword.Elusive))
                    continue;

                if (opponent.Keywords.Contains(EGameCardKeyword.Fearsome) && myCard.Attack < 3)
                    continue;

                if (myCard.Keywords.Contains(EGameCardKeyword.CantBlock))
                    continue;

                ret.Add(myCard, opponent);
                ++opponentStartIdx;
                break;
            }
        }

        return ret;
    }

    public override EGamePlayAction RespondToOpponentAction(GameBoardData boardData, EGameState gameState, int mana, int spellMana)
    {
        return EGamePlayAction.PlayCards;
    }

    public override EGamePlayAction AttackTokenUsage(GameBoardData boardData, int mana, int spellMana)
    {
        // Open attack if opponent have no card to block
        return boardData.Cards.OpponentCardsBoard.Count == 0
            ? EGamePlayAction.Attack
            : EGamePlayAction.PlayCards;
    }

    public override List<InGameCard> Attack(GameBoardData boardData, List<InGameCard> playerBoardCards)
    {
        return playerBoardCards
            .OrderBy(c => c.Description.Contains("Support:"))
            .ThenBy(c => c.Attack)
            .ToList();
    }
}
