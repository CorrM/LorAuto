using LorAuto.Card;
using LorAuto.Card.Model;
using LorAuto.Client.Model;

namespace LorAuto.Plugin.Types;

// TODO: What if bot want to summon but board is full (6 card in board)
// TODO: Maybe add `AcceptAttack` and `AcceptBlock` to make sure cards are moved, as card could be stunned

/// <summary>
/// Base class for implementing strategies.
/// </summary>
public abstract class StrategyPlugin : PluginBase
{
    /// <summary>
    /// Gets the list of playable hand cards based on the current board state and available resources.
    /// </summary>
    /// <param name="boardCards">The board cards containing the player's hand cards.</param>
    /// <param name="mana">The available mana.</param>
    /// <param name="spellMana">The available spell mana.</param>
    /// <returns>A list of playable hand cards.</returns>
    public virtual List<InGameCard> GetPlayableHandCards(BoardCards boardCards, int mana, int spellMana)
    {
        return boardCards.CardsHand
            .Where(card => card.Cost <= mana || (card.Type == EGameCardType.Spell && card.Cost <= mana + spellMana))
            .OrderByDescending(card => card.Cost)
            .ToList();
    }

    /// <summary>
    /// Performs the mulligan phase, selecting cards to replace.
    /// </summary>
    /// <param name="mulliganCards">The cards available for mulligan.</param>
    /// <returns>The list of cards to replace.</returns>
    public abstract List<InGameCard> Mulligan(IEnumerable<InGameCard> mulliganCards);

    /// <summary>
    /// Plays a hand card from the player's hand.
    /// </summary>
    /// <param name="boardData">The current game board data.</param>
    /// <param name="gameState">The current game state.</param>
    /// <param name="mana">The available mana.</param>
    /// <param name="spellMana">The available spell mana.</param>
    /// <returns>A tuple containing the played hand card and its target selector (if applicable).</returns>
    public abstract (InGameCard HandCard, CardTargetSelector? Target)? PlayHandCard(GameBoardData boardData, EGameState gameState, int mana, int spellMana);

    /// <summary>
    /// Blocks incoming attacks from the opponent's board cards.
    /// </summary>
    /// <param name="boardData">The current game board data.</param>
    /// <param name="spellsToUse">Output parameter for the list of spell cards to use for blocking.</param>
    /// <returns>A dictionary mapping player's own board cards to the opponent's board cards to block them.</returns>
    public abstract Dictionary<InGameCard, InGameCard> Block(GameBoardData boardData, out List<CardTargetSelector>? spellsToUse);

    /// <summary>
    /// Responds to an opponent's action during the game.
    /// </summary>
    /// <param name="boardData">The current game board data.</param>
    /// <param name="gameState">The current game state.</param>
    /// <param name="mana">The available mana.</param>
    /// <param name="spellMana">The available spell mana.</param>
    /// <returns>The action to perform in response to the opponent's action.</returns>
    public abstract EGamePlayAction RespondToOpponentAction(GameBoardData boardData, EGameState gameState, int mana, int spellMana);

    /// <summary>
    /// Determines the action to take for using attack tokens.
    /// </summary>
    /// <param name="boardData">The current game board data.</param>
    /// <param name="mana">The available mana.</param>
    /// <param name="spellMana">The available spell mana.</param>
    /// <returns>The action to take for using attack tokens.</returns>
    public abstract EGamePlayAction AttackTokenUsage(GameBoardData boardData, int mana, int spellMana);

    /// <summary>
    /// Performs the attack action on the opponent's board.
    /// </summary>
    /// <param name="boardData">The current game board data.</param>
    /// <param name="playerBoardCards">The cards on the player's board.</param>
    /// <returns>A list of cards to attack with.</returns>
    public abstract List<InGameCard> Attack(GameBoardData boardData, List<InGameCard> playerBoardCards);
}
