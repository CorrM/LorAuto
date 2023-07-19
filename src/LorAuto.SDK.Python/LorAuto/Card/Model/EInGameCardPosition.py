from enum import IntEnum


class EInGameCardPosition(IntEnum):
    """
    Represents the possible positions of an in-game card.
    """
    Unknown = 0
    Mulligan = 1
    Hand = 2
    Board = 3
    AttackOrBlock = 4
    SpellStack = 5
    OpponentAttackOrBlock = 6
    OpponentBoard = 7
    OpponentHand = 8
