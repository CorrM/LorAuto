using LorAuto.Card;

namespace LorAuto.Client.Model;

public sealed class BoardState
{
    public List<InGameCard> AllCards { get; }
    public List<InGameCard> CardsHand { get; }
    public List<InGameCard> CardsBoard { get; }
    public List<InGameCard> CardsMulligan { get; }
    public List<InGameCard> CardsAttackOrBlock { get; }
    public List<InGameCard> SpellStack { get; }
    public List<InGameCard> OpponentCardsAttackOrBlock { get; }
    public List<InGameCard> OpponentCardsBoard { get; }
    public List<InGameCard> OpponentCardsHand { get; }

    public BoardState()
    {
        AllCards = new List<InGameCard>();
        CardsHand = new List<InGameCard>();
        CardsBoard = new List<InGameCard>();
        CardsMulligan = new List<InGameCard>();
        CardsAttackOrBlock = new List<InGameCard>();
        SpellStack = new List<InGameCard>();
        OpponentCardsAttackOrBlock = new List<InGameCard>();
        OpponentCardsBoard = new List<InGameCard>();
        OpponentCardsHand = new List<InGameCard>();
    }

    public void Clear()
    {
        AllCards.Clear();
        CardsHand.Clear();
        CardsBoard.Clear();
        CardsMulligan.Clear();
        CardsAttackOrBlock.Clear();
        SpellStack.Clear();
        OpponentCardsAttackOrBlock.Clear();
        OpponentCardsBoard.Clear();
        OpponentCardsHand.Clear();
    }
}
