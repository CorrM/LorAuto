namespace LorAuto.Card.Model;

/// <summary>
/// Represents the type of a game card.
/// </summary>
public enum EGameCardType
{
    /// <summary>
    /// Indicates that the card is a spell.
    /// </summary>
    Spell,

    /// <summary>
    /// Indicates that the card is a unit.
    /// </summary>
    Unit,

    /// <summary>
    /// Indicates that the card is an ability.
    /// </summary>
    Ability,

    /// <summary>
    /// Indicates that the card is a trap.
    /// </summary>
    Trap,

    /// <summary>
    /// Indicates that the card is a landmark.
    /// </summary>
    Landmark,

    /// <summary>
    /// Indicates that the card is equipment.
    /// </summary>
    Equipment,
}
