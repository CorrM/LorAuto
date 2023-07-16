using LorAuto.Card.Model;

namespace LorAuto.Strategy.Model;

/// <summary>
/// Represents a card target selector, used for specifying targets for a card's effect in board.
/// </summary>
public sealed class CardTargetSelector
{
    private readonly InGameCard _card;
    private readonly List<(ECardTarget, InGameCard?)> _targets;

    /// <summary>
    /// Initializes a new instance of the <see cref="CardTargetSelector"/> class with the specified card.
    /// </summary>
    /// <param name="card">The card associated with the target selector.</param>
    private CardTargetSelector(InGameCard card)
    {
        _card = card;
        _targets = new List<(ECardTarget, InGameCard?)>();
    }

    /// <summary>
    /// Creates a new card target selector for the specified card.
    /// </summary>
    /// <param name="card">The card for which to create the target selector.</param>
    /// <returns>A new instance of the <see cref="CardTargetSelector"/> class.</returns>
    public static CardTargetSelector Select(InGameCard card)
    {
        return new CardTargetSelector(card);
    }

    /// <summary>
    /// Gets the list of targets selected by the target selector.
    /// </summary>
    /// <returns>The list of targets selected by the target selector.</returns>
    internal List<(ECardTarget, InGameCard?)> GetTargets()
    {
        return _targets;
    }

    /// <summary>
    /// Retrieves the selected card from the card target selector.
    /// </summary>
    /// <returns>The selected card as an <see cref="InGameCard"/>.</returns>
    internal InGameCard GetSelectedCard()
    {
        return _card;
    }

    /// <summary>
    /// Adds a card target to the target selector.
    /// </summary>
    /// <param name="card">The card target to add.</param>
    /// <returns>A <see cref="CardTargetSelector"/> instance to continue selecting targets.</returns>
    public CardTargetSelector Target(InGameCard card)
    {
        _targets.Add((ECardTarget.Card, card));

        return this;
    }

    /// <summary>
    /// Adds a nexus target to the target selector.
    /// </summary>
    /// <returns>A <see cref="CardTargetSelector"/> instance to continue selecting targets.</returns>
    public CardTargetSelector TargetNexus()
    {
        _targets.Add((ECardTarget.Nexus, null));

        return this;
    }

    /// <summary>
    /// Adds an opponent nexus target to the target selector.
    /// </summary>
    /// <returns>A <see cref="CardTargetSelector"/> instance to continue selecting targets.</returns>
    public CardTargetSelector TargetOpponentNexus()
    {
        _targets.Add((ECardTarget.OpponentNexus, null));

        return this;
    }

    /// <summary>
    /// Adds a hand card target to the target selector.
    /// </summary>
    /// <param name="cardInPlayerHand">The hand card to target.</param>
    /// <returns>A <see cref="CardTargetSelector"/> instance to continue selecting targets.</returns>
    public CardTargetSelector TargetHandCard(InGameCard cardInPlayerHand)
    {
        _targets.Add((ECardTarget.HandCard, cardInPlayerHand));

        return this;
    }
}
