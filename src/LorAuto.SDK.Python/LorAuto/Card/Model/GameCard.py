from dataclasses import dataclass

from LorAuto.Card.Model.EGameCardKeyword import EGameCardKeyword
from LorAuto.Card.Model.EGameCardType import EGameCardType


@dataclass
class GameCard:
    """
    Represents a game card.
    """

    Name: str
    CardCode: str
    Cost: int
    Attack: int
    Health: int
    CardType: EGameCardType
    Keywords: list[EGameCardKeyword]
    Description: str

    def __str__(self) -> str:
        """
        Returns a string representation of the GameCard object.

        Returns:
            str: A string representation of the GameCard object.
        """
        return f"Card({self.name}({self.cost}) T: {self.card_type.value} A: {self.attack} H: {self.health})"
