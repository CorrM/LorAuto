from typing import Optional, Tuple

from LorAuto.Card.Model.EGameCardKeyword import EGameCardKeyword
from LorAuto.Card.Model.EGameCardType import EGameCardType
from LorAuto.Card.Model.InGameCard import InGameCard
from LorAuto.Client.CardTargetSelector import CardTargetSelector
from LorAuto.Client.Model.EGamePlayAction import EGamePlayAction
from LorAuto.Client.Model.EGameState import EGameState
from LorAuto.Client.Model.GameBoardData import GameBoardData
from LorAuto.Plugin.Model.PluginInfo import PluginInfo, EPluginKind
from LorAuto.Plugin.StrategyPlugin import StrategyPlugin


class GenericPython(StrategyPlugin):
    @property
    def PluginInformation(self) -> PluginInfo:
        return PluginInfo("GenericPython", EPluginKind.Strategy, "xxx", "https://github.com/CorrM/LorAuto/")

    def Mulligan(self, mulligan_cards: list[InGameCard]) -> list[InGameCard]:
        return [mulligan_cards[0]]

    def PlayHandCard(self, board_data: GameBoardData, game_state: EGameState, mana: int, spell_mana: int) \
            -> Optional[Tuple[InGameCard, Optional[CardTargetSelector]]]:
        # Get the list of attributes and methods for the object
        attributes = dir(board_data)

        # Print the attributes
        for attribute in attributes:
            print(attribute)

        return (board_data.Cards.CardsHand[0], None)

    def Block(self, board_data: GameBoardData, spells_to_use: list[CardTargetSelector]) \
            -> dict[InGameCard, InGameCard]:
        dict_to_ret = {board_data.cards.opponent_cards_board[0]: board_data.cards.cards_board[0]}
        return dict_to_ret

    def RespondToOpponentAction(self, board_data: GameBoardData, game_state: EGameState, mana: int, spell_mana: int) \
            -> EGamePlayAction:
        return EGamePlayAction.PlayCards

    def AttackTokenUsage(self, board_data: GameBoardData, mana: int, spell_mana: int) -> EGamePlayAction:
        return EGamePlayAction.PlayCards

    def Attack(self, board_data: GameBoardData, player_board_cards: list[InGameCard]) -> list[InGameCard]:
        print(player_board_cards)
        return player_board_cards.copy()
