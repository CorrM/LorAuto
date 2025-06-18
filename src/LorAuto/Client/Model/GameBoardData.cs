using LorAuto.Card.Model;

namespace LorAuto.Client.Model;

/// <summary>
/// Represents the data of the game board.
/// </summary>
public sealed class GameBoardData
{
    /// <summary>
    /// Gets the current game state.
    /// </summary>
    public GameState GameState { get; internal set; }
    
    /// <summary>
    /// Gets the cards present on the game board.
    /// </summary>
    public BoardCards Cards { get; }

    /// <summary>
    /// Gets the current mana count.
    /// </summary>
    public int Mana { get; internal set; }

    /// <summary>
    /// Gets the current spell mana count.
    /// </summary>
    public int SpellMana { get; internal set; }

    /// <summary>
    /// Gets the health of the player's Nexus.
    /// </summary>
    public int NexusHealth { get; internal set; }

    /// <summary>
    /// Gets the health of the opponent's Nexus.
    /// </summary>
    public int OpponentNexusHealth { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameBoardData"/> class.
    /// </summary>
    public GameBoardData()
    {
        Cards = new BoardCards();
    }
}
