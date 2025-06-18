namespace LorAuto.Client.Model;

/// <summary>
/// Represents the possible states of the game.
/// </summary>
public enum GameState
{
    /// <summary>
    /// No specific state.
    /// </summary>
    None,

    /// <summary>
    /// Hold state.
    /// </summary>
    Hold,

    /// <summary>
    /// Menus state.
    /// </summary>
    Menus,

    /// <summary>
    /// MenusDeckSelected state.
    /// </summary>
    MenusDeckSelected,

    /// <summary>
    /// SearchGame state.
    /// </summary>
    SearchGame,

    /// <summary>
    /// UserInteractNotReady state.
    /// </summary>
    UserInteractNotReady,

    /// <summary>
    /// Mulligan state.
    /// </summary>
    Mulligan,

    /// <summary>
    /// OpponentTurn state.
    /// </summary>
    OpponentTurn,

    /// <summary>
    /// DefendTurn state.
    /// </summary>
    DefendTurn,

    /// <summary>
    /// AttackTurn state.
    /// </summary>
    AttackTurn,

    /// <summary>
    /// MidAttack state.
    /// </summary>
    MidAttack,
    
    /// <summary>
    /// Attacking state.
    /// </summary>
    Attacking,

    /// <summary>
    /// Blocking state.
    /// </summary>
    Blocking,

    /// <summary>
    /// End state.
    /// </summary>
    End
}
