namespace LorAuto.Card.Model;

/// <summary>
/// Represents the possible targets for a card.
/// </summary>
internal enum ECardTarget
{
    /// <summary>
    /// The target is another card.
    /// </summary>
    Card,

    /// <summary>
    /// The target is a card in the player's hand.
    /// </summary>
    HandCard,

    /// <summary>
    /// The target is the nexus.
    /// </summary>
    Nexus,

    /// <summary>
    /// The target is the opponent nexus.
    /// </summary>
    OpponentNexus
}
