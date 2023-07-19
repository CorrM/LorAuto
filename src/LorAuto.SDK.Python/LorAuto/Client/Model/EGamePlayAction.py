from enum import IntEnum


class EGamePlayAction(IntEnum):
    """
    Represents the possible game play actions.
    """
    Attack = 0
    PlayCards = 1
    Skip = 2
