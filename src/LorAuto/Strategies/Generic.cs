using LorAuto.Card.Model;
using LorAuto.Client.Model;
using LorAuto.Strategies.Model;

namespace LorAuto.Strategies;

public sealed class Generic : Strategy
{
    public override IEnumerable<InGameCard> Mulligan(IEnumerable<InGameCard> mulliganCards)
    {
        return mulliganCards.Where(c => c.Cost > 3);
    }
    
    public override (InGameCard HandCard, List<InGameCard?>? Targets)? PlayHandCard(BoardCards boardCards, EGameState gameState, int mana, int spellMana, IEnumerable<InGameCard> playableHandCards)
    {
        InGameCard? cardToPlay = playableHandCards.Where(c => c.Type is not (GameCardType.Ability or GameCardType.Spell))
            .Where(c => c.Cost <= mana)
            .MaxBy(c => c.Attack);
        
        if (cardToPlay is not null)
            return (cardToPlay, null);

        return null;
    }

    public override Dictionary<InGameCard, InGameCard> Block(BoardCards boardCards, out Dictionary<InGameCard, IEnumerable<InGameCard>?>? spellsToUse, out IEnumerable<InGameCard>? abilitiesToUse)
    {
        spellsToUse = null;
        abilitiesToUse = null;

        var ret = new Dictionary<InGameCard, InGameCard>();
        foreach (InGameCard myCard in boardCards.CardsBoard)
        {
            foreach (InGameCard opponent in boardCards.OpponentCardsAttackOrBlock)
            {
                if (opponent.Keywords.Contains(GameCardKeyword.Elusive) && !myCard.Keywords.Contains(GameCardKeyword.Elusive))
                    continue;

                if (opponent.Keywords.Contains(GameCardKeyword.Fearsome) && myCard.Attack < 3)
                    continue;
            
                if (myCard.Keywords.Contains(GameCardKeyword.CantBlock))
                    continue;
            
                bool isBlockable = true;
                foreach (InGameCard allyCard in boardCards.CardsAttackOrBlock)
                {
                    if (Math.Abs(allyCard.TopCenterPos.X - opponent.TopCenterPos.X) >= 10)
                        continue;

                    isBlockable = false;
                    break;
                }

                if (!isBlockable)
                    continue;
                
                ret.Add(myCard, opponent);
                break;
            }
        }
        
        return ret;
    }

    public override EGamePlayAction RespondToOpponentAction(BoardCards boardCards, EGameState gameState, int mana, int spellMana)
    {
        return EGamePlayAction.PlayCards;
    }

    public override EGamePlayAction AttackTokenUsage(BoardCards boardCards, int mana, int spellMana)
    {
        return EGamePlayAction.PlayCards;
    }

    public override List<InGameCard> Attack(BoardCards boardCards, IEnumerable<InGameCard> yourBoardCards)
    {
        List<InGameCard> inGameCards = yourBoardCards.ToList();
        
        // Support will be listed first
        //foreach (InGameCard card in inGameCards.Where(c => c.Description.Contains("Support:")))
        //{
        //    
        //}
        
        return inGameCards;
    }
}
