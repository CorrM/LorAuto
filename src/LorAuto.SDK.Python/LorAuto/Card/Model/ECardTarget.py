from enum import IntEnum


class ECardTarget(IntEnum):
    """
    Represents the possible targets for a card.
    """
    Card = 0
    HandCard = 1
    Nexus = 2
    OpponentNexus = 3
