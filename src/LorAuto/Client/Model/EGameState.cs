namespace LorAuto.Client.Model;

public enum EGameState
{
    None,
    Hold,
    Menus,
    MenusDeckSelected,
    SearchGame,
    UserInteractNotReady,
    Mulligan,
    OpponentTurn,
    DefendTurn,
    AttackTurn,
    Attacking,
    Blocking,
    RoundEnd,
    Pass,
    End,
}
