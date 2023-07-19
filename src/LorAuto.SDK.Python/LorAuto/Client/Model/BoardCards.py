from dataclasses import dataclass

from LorAuto.Card.Model.InGameCard import InGameCard


@dataclass
class BoardCards:
    """
    Represents the collection of cards on the game board.
    """

    AllCards: list[InGameCard]
    CardsHand: list[InGameCard]
    CardsBoard: list[InGameCard]
    CardsMulligan: list[InGameCard]
    CardsAttackOrBlock: list[InGameCard]
    SpellStack: list[InGameCard]
    OpponentCardsAttackOrBlock: list[InGameCard]
    OpponentCardsBoard: list[InGameCard]
    OpponentCardsHand: list[InGameCard]
