﻿using LorAuto.Card;
using LorAuto.Card.Model;
using LorAuto.Client.Model;
using LorAuto.Plugin.Model;
using LorAuto.Plugin.Types;

namespace LorAuto.Strategy.Generic;

public sealed class GenericStrategy : StrategyPlugin
{
    public override PluginInfo PluginInformation { get; }

    public GenericStrategy()
    {
        PluginInformation = new PluginInfo()
        {
            Name = "Generic",
            PluginKind = EPluginKind.Strategy,
            Description = "Provides a versatile and customizable gameplay strategyB.",
            SourceCodeLink = "https://github.com/CorrM/LorAuto",
        };
    }

    public override List<InGameCard> Mulligan(IEnumerable<InGameCard> mulliganCards)
    {
        return mulliganCards.Where(c => c.Cost > 3).ToList();
    }

    public override (InGameCard HandCard, CardTargetSelector? Target)? PlayHandCard(
        GameBoardData boardData,
        GameState gameState
    )
    {
        InGameCard? cardToPlay = GetPlayableHandCards(boardData)
            .Where(c => c.Type != EGameCardType.Spell)
            .Where(c => c.Cost <= boardData.Mana && !c.Description.StartsWith("To play me"))
            .MaxBy(c => c.Attack);

        if (cardToPlay is null)
            return null;

        return (cardToPlay, null);
    }

    public override Dictionary<InGameCard, InGameCard> Block(
        GameBoardData boardData,
        out List<CardTargetSelector>? spellsToUse
    )
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

                if (opponent.Keywords.Contains(EGameCardKeyword.Elusive) &&
                    !myCard.Keywords.Contains(EGameCardKeyword.Elusive))
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

    public override EGamePlayAction RespondToOpponentAction(GameBoardData boardData, GameState gameState)
    {
        return EGamePlayAction.PlayCards;
    }

    public override EGamePlayAction AttackTokenUsage(GameBoardData boardData)
    {
        // Open attack if opponent have no card to block
        return boardData.Cards.OpponentCardsBoard.Count == 0 ? EGamePlayAction.Attack : EGamePlayAction.PlayCards;
    }

    public override List<InGameCard> Attack(GameBoardData boardData, List<InGameCard> playerBoardCards)
    {
        return playerBoardCards.OrderBy(c => c.Description.Contains("Support:"))
            .ThenByDescending(c => c.Attack)
            .ToList();
    }
}