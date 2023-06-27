using LorAuto.Card;

namespace LorAuto.Client;

public sealed class BoardState
{
    public List<InGameCard> CardsHand { get; }
    public List<InGameCard> CardsBoard { get; }
    public List<InGameCard> CardsMulligan { get; }
    public List<InGameCard> CardsAttack { get; }
    public List<InGameCard> SpellStack { get; }
    public List<InGameCard> OpponentCardsAttack { get; }
    public List<InGameCard> OpponentCardsBoard { get; }
    public List<InGameCard> OpponentCardsHand { get; }

    public BoardState()
    {
        CardsHand = new List<InGameCard>();
        CardsBoard = new List<InGameCard>();
        CardsMulligan = new List<InGameCard>();
        CardsAttack = new List<InGameCard>();
        SpellStack = new List<InGameCard>();
        OpponentCardsAttack = new List<InGameCard>();
        OpponentCardsBoard = new List<InGameCard>();
        OpponentCardsHand = new List<InGameCard>();
    }
}
