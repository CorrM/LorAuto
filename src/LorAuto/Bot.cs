using System.Diagnostics;
using GregsStack.InputSimulatorStandard;
using LorAuto.Client;
using LorAuto.Extensions;
using LorAuto.GameState;
using LorAuto.Strategies;

namespace LorAuto;

public enum GameType
{
    Standard,
    Eternal
}

/// <summary>
/// Plays the game, responsible for executing commands from <see cref="Strategy"/>
/// </summary>
public sealed class Bot
{
    private readonly StateMachine _stateMachine;
    private readonly GameClientApi _gameClientApi;
    private readonly Strategy _strategy;
    private readonly InputSimulator _input;
    
    private readonly (double, double)[] _selectDeckAi;
    private readonly (double, double)[] _selectDeckPvp;

    public Bot(StateMachine stateMachine, GameClientApi gameClientApi, Strategy strategy)
    {
        _stateMachine = stateMachine;
        _gameClientApi = gameClientApi;
        _strategy = strategy;
        _input = new InputSimulator();
        
        _selectDeckAi = new (double, double)[] { (0.04721, 0.33454), (0.15738, 0.33401), (0, 0)/*, (0.33180, 0.30779), (0.83213, 0.89538)*/ };
        _selectDeckPvp = new (double, double)[] { (0.04721, 0.33454), (0.15738, 0.25), (0, 0), /*(0.33180, 0.30779), (0.83213, 0.89538)*/ };
    }

    public void SelectDeck(GameType gameType, bool isPvp)
    {
        //while (true)
        //{
        //    Console.WriteLine($"X: {_input.Mouse.Position.X}, Y: {_input.Mouse.Position.Y}");
        //    Thread.Sleep(1000);
        //}
        
        (double, double) gameTypePos = gameType switch
        {
            GameType.Standard => (0.70989, 0.05),
            GameType.Eternal => (0.81770, 0.05),
            _ => throw new UnreachableException()
        };
        
        foreach ((double xRatio, double yRatio) in isPvp ? _selectDeckPvp : _selectDeckAi)
        {
            double xr;
            double yr;
            
            if (xRatio == 0 && yRatio == 0)
            {
                xr = gameTypePos.Item1;
                yr = gameTypePos.Item2;
            }
            else
            {
                xr = xRatio;
                yr = yRatio;
            }
            
            (double x, double y) = (_stateMachine.WindowLocation.X + (xr * _stateMachine.WindowSize.Width), _stateMachine.WindowLocation.Y + (yr * _stateMachine.WindowSize.Height));
            _input.Mouse
                .MoveMouseSmooth(x, y)
                .LeftButtonClick()
                .Sleep(Random.Shared.Next(700, 1000));
        }
    }
}
