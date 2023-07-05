using LorAuto.Card.Model;
using LorAuto.Client.Model;
using LorAuto.Strategies.Model;

namespace LorAuto.Strategies;

// TODO: What if bot want to summon but board is full (6 card in board)
// TODO: Maybe add `AcceptAttack` and `AcceptBlock` to make sure cards are moved, as card could be stunned

public abstract class Strategy
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mulliganCards"></param>
    /// <returns>Cards to replace</returns>
    public abstract IEnumerable<InGameCard> Mulligan(IEnumerable<InGameCard> mulliganCards);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="boardCards"></param>
    /// <param name="gameState"></param>
    /// <param name="mana"></param>
    /// <param name="spellMana"></param>
    /// <param name="playableHandCards"></param>
    /// <returns>Key value of sorted hand cards to play</returns>
    /// <remarks>
    /// <c>handCard</c> would be your hand card.
    /// <c>targets</c> would be opponent card(s) to target if played card is a spell or card have spell effect.
    /// <c>targets</c> are empty list card will target nexus.
    /// if <c>targets</c> is <c>null</c> then card will played to board.
    /// </remarks>
    public abstract (InGameCard HandCard, List<InGameCard?>? Targets)? PlayHandCard(BoardCards boardCards, EGameState gameState, int mana, int spellMana, IEnumerable<InGameCard> playableHandCards);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="boardCards"></param>
    /// <param name="spellsToUse">if value of Dictionary is null then spell will target nexus</param>
    /// <param name="abilitiesToUse"></param>
    /// <returns>Your defend cards against opponent attacking cards</returns>
    /// <remarks>
    /// Returned dictionary <c>key</c> would be your card, <c>value</c> would be opponent card
    /// </remarks>
    public abstract Dictionary<InGameCard, InGameCard> Block(BoardCards boardCards, out Dictionary<InGameCard, IEnumerable<InGameCard>?>? spellsToUse, out IEnumerable<InGameCard>? abilitiesToUse);

    /// <summary>
    /// Called when you have a chance to respond to an action taken by your opponent
    /// </summary>
    /// <param name="boardCards"></param>
    /// <param name="gameState"></param>
    /// <param name="mana"></param>
    /// <param name="spellMana"></param>
    /// <returns></returns>
    public abstract EGamePlayAction RespondToOpponentAction(BoardCards boardCards, EGameState gameState, int mana, int spellMana);

    /// <summary>
    /// Called when you have attack token
    /// </summary>
    /// <param name="boardCards"></param>
    /// <param name="mana"></param>
    /// <param name="spellMana"></param>
    /// <returns>Attack token usage</returns>
    public abstract EGamePlayAction AttackTokenUsage(BoardCards boardCards, int mana, int spellMana);

    /// <summary>
    /// Called when its your turn to attack
    /// </summary>
    /// <param name="boardCards"></param>
    /// <param name="yourBoardCards"></param>
    /// <returns>List of board cards to attack, sorted from left to right</returns>
    /// <remarks>Don't return <paramref name="boardCards"/></remarks>
    public abstract List<InGameCard> Attack(BoardCards boardCards, IEnumerable<InGameCard> yourBoardCards);
}
