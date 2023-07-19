using LorAuto.Card.Model;

namespace LorAuto.Client.Model;

/// <summary>
/// Represents the collection of cards on the game board.
/// </summary>
public sealed class BoardCards
{
    /// <summary>
    /// Gets the list of all cards on the board.
    /// </summary>
    public List<InGameCard> AllCards { get; }

    /// <summary>
    /// Gets the list of cards in the player's hand.
    /// </summary>
    public List<InGameCard> CardsHand { get; }

    /// <summary>
    /// Gets the list of cards on the player's board.
    /// </summary>
    public List<InGameCard> CardsBoard { get; }

    /// <summary>
    /// Gets the list of cards in the mulligan phase.
    /// </summary>
    public List<InGameCard> CardsMulligan { get; }

    /// <summary>
    /// Gets the list of cards involved in an attack or block.
    /// </summary>
    public List<InGameCard> CardsAttackOrBlock { get; }

    /// <summary>
    /// Gets the list of cards in the spell stack.
    /// </summary>
    public List<InGameCard> SpellStack { get; }

    /// <summary>
    /// Gets the list of opponent's cards involved in an attack or block.
    /// </summary>
    public List<InGameCard> OpponentCardsAttackOrBlock { get; }

    /// <summary>
    /// Gets the list of opponent's cards on the board.
    /// </summary>
    public List<InGameCard> OpponentCardsBoard { get; }

    /// <summary>
    /// Gets the list of opponent's cards in hand.
    /// </summary>
    public List<InGameCard> OpponentCardsHand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoardCards"/> class.
    /// </summary>
    public BoardCards()
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

    /// <summary>
    /// Clears all the card collections.
    /// </summary>
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

    /// <summary>
    /// Sorts the card collections based on their X position.
    /// </summary>
    public void Sort()
    {
        int Cmp(InGameCard card1, InGameCard card2) => card1.Position.X.CompareTo(card2.Position.X);

        CardsHand.Sort(Cmp);
        CardsBoard.Sort(Cmp);
        CardsMulligan.Sort(Cmp);
        CardsAttackOrBlock.Sort(Cmp);
        SpellStack.Sort(Cmp);
        OpponentCardsAttackOrBlock.Sort(Cmp);
        OpponentCardsBoard.Sort(Cmp);
        OpponentCardsHand.Sort(Cmp);
    }
}
