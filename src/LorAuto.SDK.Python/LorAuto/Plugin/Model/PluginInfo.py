from dataclasses import dataclass
from typing import Optional

from LorAuto.Plugin.Model.EPluginKind import EPluginKind


@dataclass
class PluginInfo:
    Name: str
    PluginKind: EPluginKind
    Description: str
    SourceCodeLink: Optional[str]
