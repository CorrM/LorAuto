from enum import IntEnum


class EGameCardType(IntEnum):
    """
    Represents the type of game card.
    """
    Spell = 0
    Unit = 1
    Ability = 2
    Trap = 3
    Landmark = 4
    Equipment = 5
