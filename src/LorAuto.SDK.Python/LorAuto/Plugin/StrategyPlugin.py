from abc import abstractmethod
from typing import Tuple, Optional

from LorAuto.Card.Model.EGameCardType import EGameCardType
from LorAuto.Card.Model.InGameCard import InGameCard
from LorAuto.Client.CardTargetSelector import CardTargetSelector
from LorAuto.Client.Model.BoardCards import BoardCards
from LorAuto.Client.Model.EGamePlayAction import EGamePlayAction
from LorAuto.Client.Model.EGameState import EGameState
from LorAuto.Client.Model.GameBoardData import GameBoardData
from LorAuto.Plugin.PluginBase import PluginBase


class StrategyPlugin(PluginBase):
    """
    Base class for implementing strategies.
    """

    def GetPlayableHandCards(self, board_cards: BoardCards, mana: int, spell_mana: int) -> list[InGameCard]:
        """
        Gets the list of playable hand cards based on the current board state and available resources.

        Args:
            board_cards (BoardCards): The board cards containing the player's hand cards.
            mana (int): The available mana.
            spell_mana (int): The available spell mana.

        Returns:
            List[InGameCard]: A list of playable hand cards.
        """
        pass

    @abstractmethod
    def Mulligan(self, mulligan_cards: list[InGameCard]) -> list[InGameCard]:
        """
        Performs the mulligan phase, selecting cards to replace.

        Args:
            mulligan_cards (List[InGameCard]): The cards available for mulligan.

        Returns:
            List[InGameCard]: The list of cards to replace.
        """
        pass

    @abstractmethod
    def PlayHandCard(self, board_data: GameBoardData, game_state: EGameState, mana: int,
                       spell_mana: int) -> Optional[Tuple[InGameCard, Optional[CardTargetSelector]]]:
        """
        Plays a hand card from the player's hand.

        Args:
            board_data (GameBoardData): The current game board data.
            game_state (EGameState): The current game state.
            mana (int): The available mana.
            spell_mana (int): The available spell mana.

        Returns:
            Optional[Tuple[InGameCard, Optional[CardTargetSelector]]]: A tuple containing the played hand card and
                its target selector (if applicable).
        """
        pass

    @abstractmethod
    def Block(self, board_data: GameBoardData,
              spells_to_use: list[CardTargetSelector]) -> dict[InGameCard, InGameCard]:
        """
        Blocks incoming attacks from the opponent's board cards.

        Args:
            board_data (GameBoardData): The current game board data.
            spells_to_use (List[CardTargetSelector]): Output parameter for the list of spell cards to use for blocking.

        Returns:
            Dict[InGameCard, InGameCard]: A dictionary mapping player's own board cards to the opponent's board cards
            to block them.
        """
        pass

    @abstractmethod
    def RespondToOpponentAction(self, board_data: GameBoardData, game_state: EGameState,
                                   mana: int, spell_mana: int) -> EGamePlayAction:
        """
        Responds to an opponent's action during the game.

        Args:
            board_data (GameBoardData): The current game board data.
            game_state (EGameState): The current game state.
            mana (int): The available mana.
            spell_mana (int): The available spell mana.

        Returns:
            EGamePlayAction: The action to perform in response to the opponent's action.
        """
        pass

    @abstractmethod
    def AttackTokenUsage(self, board_data: GameBoardData, mana: int, spell_mana: int) -> EGamePlayAction:
        """
        Determines the action to take for using attack tokens.

        Args:
            board_data (GameBoardData): The current game board data.
            mana (int): The available mana.
            spell_mana (int): The available spell mana.

        Returns:
            EGamePlayAction: The action to take for using attack tokens.
        """
        pass

    @abstractmethod
    def Attack(self, board_data: GameBoardData, player_board_cards: list[InGameCard]) -> list[InGameCard]:
        """
        Performs the attack action on the opponent's board.

        Args:
            board_data (GameBoardData): The current game board data.
            player_board_cards (List[InGameCard]): The cards on the player's board.

        Returns:
            List[InGameCard]: A list of cards to attack with.
        """
        pass
