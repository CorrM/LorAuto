from dataclasses import dataclass

from LorAuto.Card.Model.EInGameCardPosition import EInGameCardPosition
from LorAuto.Card.Model.GameCard import GameCard
from LorAuto.Utils.Point import Point
from LorAuto.Utils.Size import Size


@dataclass
class InGameCard(GameCard):
    """
    Represents an in-game card.
    """

    CardId: int
    InGamePosition: EInGameCardPosition
    Position: Point
    Size: Size
    TopCenterPos: Point
    BottomCenterPos: Point
    IsLocalPlayer: bool

    def __str__(self) -> str:
        """
        Returns a string representation of the InGameCard object.

        Returns:
            str: A string representation of the InGameCard object.
        """
        return f"InGameCard({super().__str__()} -- TopCenter: ({self.top_center_pos}); IsLocalPlayer: {self.is_local_player})"
