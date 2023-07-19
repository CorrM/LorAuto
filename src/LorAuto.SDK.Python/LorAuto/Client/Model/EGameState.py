from enum import IntEnum


class EGameState(IntEnum):
    """
    Represents the possible states of the game.
    """
    NoneState = 0
    Hold = 1
    Menus = 2
    MenusDeckSelected = 3
    SearchGame = 4
    UserInteractNotReady = 5
    Mulligan = 6
    OpponentTurn = 7
    DefendTurn = 8
    AttackTurn = 9
    Attacking = 10
    Blocking = 11
    End = 12
