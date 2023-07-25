/*
using System.Data;
using LorAuto.Card;
using LorAuto.Card.Model;
using LorAuto.Client.Model;
using LorAuto.Extensions;
using LorAuto.Plugin.Model;
using LorAuto.Plugin.Types;
using Python.Runtime;

namespace LorAuto.Plugin.Holders.Python;

internal sealed class PythonStrategyPluginWrapper : StrategyPlugin
{
    private readonly PyObject _pyInstance;

    public override PluginInfo PluginInformation { get; }

    public PythonStrategyPluginWrapper(PyObject instance, PluginInfo pluginInfo)
    {
        _pyInstance = instance;
        PluginInformation = pluginInfo;
    }

    public override List<InGameCard> GetPlayableHandCards(BoardCards boardCards, int mana, int spellMana)
    {
        using Py.GILState gilState = Py.GIL();
        using PyObject result = _pyInstance.InvokeMethod(nameof(GetPlayableHandCards), _pyInstance, boardCards, mana, spellMana);

        if (result.IsNone())
            return base.GetPlayableHandCards(boardCards, mana, spellMana);

        if (!PyList.IsListType(result))
            throw new DataException("Invalid return data type.");

        using PyList resultAsList = PyList.AsList(result);
        return resultAsList.Select(item => item.As<InGameCard>()).ToList();
    }

    public override List<InGameCard> Mulligan(IEnumerable<InGameCard> mulliganCards)
    {
        using Py.GILState gilState = Py.GIL();
        using PyObject result = _pyInstance.InvokeMethod(nameof(Mulligan), _pyInstance, mulliganCards);

        if (result.IsNone())
            throw new DataException("'None' are not accepted as a return type.");

        if (!PyList.IsListType(result))
            throw new DataException("Invalid return data type.");

        using PyList resultAsList = PyList.AsList(result);
        return resultAsList.Select(item => item.As<InGameCard>()).ToList();
    }

    public override (InGameCard HandCard, CardTargetSelector? Target)? PlayHandCard(GameBoardData boardData, EGameState gameState, int mana, int spellMana)
    {
        using Py.GILState gilState = Py.GIL();
        using PyObject result = _pyInstance.InvokeMethod(nameof(PlayHandCard), _pyInstance, boardData, gameState, mana, spellMana);

        if (result.IsNone())
            throw new DataException("'None' are not accepted as a return type.");

        if (!PyTuple.IsTupleType(result))
            throw new DataException("Invalid return data type.");

        using PyTuple resultAsTuple = PyTuple.AsTuple(result);
        var inGameCard = resultAsTuple[0].As<InGameCard>();
        var target = resultAsTuple[1].As<CardTargetSelector>();

        return (inGameCard, target);
    }

    public override Dictionary<InGameCard, InGameCard> Block(GameBoardData boardData, out List<CardTargetSelector>? spellsToUse)
    {
        using Py.GILState gilState = Py.GIL();

        var gg = new List<CardTargetSelector>();
        using PyObject result = _pyInstance.InvokeMethod(nameof(Block), _pyInstance, boardData, gg);

        if (result.IsNone())
            throw new DataException("'None' are not accepted as a return type.");

        if (!PyDict.IsDictType(result))
            throw new DataException("Invalid return data type.");

        using var resultAsDict = result.As<PyDict>();
        using PyIterable dictItems = resultAsDict.Items();

        spellsToUse = gg;
        return dictItems.ToDictionary(item => item[0].As<InGameCard>(), item => item[1].As<InGameCard>());
    }

    public override EGamePlayAction RespondToOpponentAction(GameBoardData boardData, EGameState gameState, int mana, int spellMana)
    {
        using Py.GILState gilState = Py.GIL();
        using PyObject result = _pyInstance.InvokeMethod(nameof(RespondToOpponentAction), _pyInstance, boardData, gameState, mana, spellMana);

        if (result.IsNone())
            throw new DataException("'None' are not accepted as a return type.");

        return result.AsEnum<EGamePlayAction>();
    }

    public override EGamePlayAction AttackTokenUsage(GameBoardData boardData, int mana, int spellMana)
    {
        using Py.GILState gilState = Py.GIL();
        using PyObject result = _pyInstance.InvokeMethod(nameof(AttackTokenUsage), _pyInstance, boardData, mana, spellMana);

        if (result.IsNone())
            throw new DataException("'None' are not accepted as a return type.");

        return result.AsEnum<EGamePlayAction>();
    }

    public override List<InGameCard> Attack(GameBoardData boardData, List<InGameCard> playerBoardCards)
    {
        using Py.GILState gilState = Py.GIL();
        using PyObject result = _pyInstance.InvokeMethod(nameof(Attack), _pyInstance, boardData, playerBoardCards);

        if (result.IsNone())
            throw new DataException("'None' are not accepted as a return type.");

        if (!PyList.IsListType(result))
            throw new DataException("Invalid return data type.");

        using PyList resultAsList = PyList.AsList(result);
        return resultAsList.Select(item => item.As<InGameCard>()).ToList();
    }
}
*/


