from dataclasses import dataclass
from LorAuto.Client.Model.BoardCards import BoardCards


@dataclass
class GameBoardData:
    """
    Represents the data of the game board.
    """

    Cards: BoardCards
    Mana: int
    SpellMana: int
    NexusHealth: int
    OpponentNexusHealth: int
