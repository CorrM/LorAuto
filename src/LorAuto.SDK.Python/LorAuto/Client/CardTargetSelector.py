from LorAuto.Card.Model.ECardTarget import ECardTarget
from LorAuto.Card.Model.InGameCard import InGameCard


class CardTargetSelector:
    """
    Represents a card target selector, used for specifying targets for a card's effect in board.
    """

    def __init__(self, card: InGameCard):
        self._card = card
        self._targets = []

    def AddTarget(self, card: InGameCard):
        """
        Adds a card target to the target selector.
        """
        self._targets.append((ECardTarget.Card, card))
        return self

    def AddTargetNexus(self):
        """
        Adds a nexus target to the target selector.
        """
        self._targets.append((ECardTarget.Nexus, None))
        return self

    def AddTargetOpponentNexus(self):
        """
        Adds an opponent nexus target to the target selector.
        """
        self._targets.append((ECardTarget.OpponentNexus, None))
        return self

    def AddTargetHandCard(self, card_in_player_hand: InGameCard):
        """
        Adds a hand card target to the target selector.
        """
        self._targets.append((ECardTarget.HandCard, card_in_player_hand))
        return self
