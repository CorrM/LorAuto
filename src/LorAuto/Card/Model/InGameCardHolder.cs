namespace LorAuto.Card.Model;

/// <summary>
/// Represents the possible positions of an in-game card.
/// </summary>
public enum InGameCardHolder
{
    /// <summary>
    /// No specific position.
    /// </summary>
    None,

    /// <summary>
    /// Mulligan position (during the mulligan phase).
    /// </summary>
    Mulligan,

    /// <summary>
    /// Hand position (in the player's hand).
    /// </summary>
    Hand,

    /// <summary>
    /// Board position (on the player's board).
    /// </summary>
    Board,

    /// <summary>
    /// Attack or block position (engaged in combat or blocking).
    /// </summary>
    AttackOrBlock,

    /// <summary>
    /// Spell stack position (card in the spell stack).
    /// </summary>
    SpellStack,

    /// <summary>
    /// Opponent's attack or block position (engaged in combat or blocking on the opponent's side).
    /// </summary>
    OpponentAttackOrBlock,

    /// <summary>
    /// Opponent's board position (on the opponent's board).
    /// </summary>
    OpponentBoard,

    /// <summary>
    /// Opponent's hand position (in the opponent's hand).
    /// </summary>
    OpponentHand
}
