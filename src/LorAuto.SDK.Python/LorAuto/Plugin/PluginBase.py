from abc import ABC, abstractmethod

from LorAuto.Plugin.Model.PluginInfo import PluginInfo


class PluginBase(ABC):
    @property
    @abstractmethod
    def PluginInformation(self) -> PluginInfo:
        """
        Gets the information about the plugin.

        Returns:
            PluginInfo: The information about the plugin.
        """
        pass

    def Dispose(self) -> None:
        """
        Disposes the resources used by this instance.
        """
        pass
