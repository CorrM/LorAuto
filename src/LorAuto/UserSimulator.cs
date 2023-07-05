﻿using System.Diagnostics;
using GregsStack.InputSimulatorStandard;
using GregsStack.InputSimulatorStandard.Native;
using LorAuto.Card.Model;
using LorAuto.Client.Model;
using LorAuto.Extensions;
using LorAuto.Game;
using PInvoke;

namespace LorAuto;

public sealed class UserSimulator
{
    private readonly StateMachine _stateMachine;
    private readonly InputSimulator _input;
    private readonly (double, double)[] _selectDeckAi;
    private readonly (double, double)[] _selectDeckPvp;

    public UserSimulator(StateMachine stateMachine)
    {
        _stateMachine = stateMachine;
        _input = new InputSimulator();
        
        _selectDeckAi = new (double, double)[] { (0.04721, 0.33454), (0.15738, 0.33401), (0, 0), (0.33180, 0.30779), (0.83213, 0.89538) };
        _selectDeckPvp = new (double, double)[] { (0.04721, 0.33454), (0.15738, 0.25), (0, 0), (0.33180, 0.30779), (0.83213, 0.89538) };
    }

    private void ForegroundIfGameNot()
    {
        _stateMachine.UpdateClientInfo();

        if (_stateMachine.GameIsForeground)
            return;
        
        User32.SetForegroundWindow(_stateMachine.GameWindowHandle);
    }
    
    public void SelectDeck(GameRotationType gameRotationType, bool isPvp)
    {
        ForegroundIfGameNot();

        (double, double) gameTypePos = gameRotationType switch
        {
            GameRotationType.Standard => (0.70989, 0.05),
            GameRotationType.Eternal => (0.81970, 0.05),
            _ => throw new UnreachableException()
        };

        foreach ((double xRatio, double yRatio) in isPvp ? _selectDeckPvp : _selectDeckAi)
        {
            double xr;
            double yr;

            if (xRatio == 0 && yRatio == 0 && isPvp)
            {
                xr = gameTypePos.Item1;
                yr = gameTypePos.Item2;
            }
            else if (xRatio == 0 && yRatio == 0)
            {
                // vs AI there is not Standard or Eternal
                continue;
            }
            else
            {
                xr = xRatio;
                yr = yRatio;
            }

            (double x, double y) = (_stateMachine.WindowLocation.X + (xr * _stateMachine.WindowSize.Width), _stateMachine.WindowLocation.Y + (yr * _stateMachine.WindowSize.Height));
            _input.Mouse.MoveMouseSmooth(x, y)
                .LeftButtonClick()
                .Sleep(Random.Shared.Next(700, 1000));
        }

        Thread.Sleep(1000);

        // Handle "Matchmaking has failed" error
        (double, double) okButtonPos = (_stateMachine.WindowLocation.X + 0.5 * _stateMachine.WindowSize.Width, _stateMachine.WindowLocation.Y + 0.546 * _stateMachine.WindowSize.Height);
        _input.Mouse.MoveMouseSmooth((int)okButtonPos.Item1, (int)okButtonPos.Item2)
            .LeftButtonClick();
    }
    
    public void PlayCard(InGameCard card)
    {
        ForegroundIfGameNot();

        int x = _stateMachine.WindowLocation.X + card.TopCenterPos.X;
        int y = _stateMachine.WindowLocation.Y + _stateMachine.WindowSize.Height - card.TopCenterPos.Y;

        _input.Mouse.MoveMouseSmooth(x, y);
        Thread.Sleep(500); // Wait for the card maximize animation

        _input.Mouse.MoveMouseSmooth(x, y)
            .LeftButtonDown();

        int newY = y - 3 * _stateMachine.WindowSize.Height / 7;
        _input.Mouse.MoveMouseSmooth(x, newY);
        Thread.Sleep(300);

        _input.Mouse.MoveMouseSmooth(x, newY)
            .LeftButtonUp();

        Thread.Sleep(300);
        if (card.Type != GameCardType.Spell)
            return;

        Thread.Sleep(1000);
        _input.Keyboard.KeyPress(VirtualKeyCode.SPACE);
    }

    public void ClickCard(InGameCard card)
    {
        ForegroundIfGameNot();
        
        int cx = _stateMachine.WindowLocation.X + card.TopCenterPos.X;
        int cy = _stateMachine.WindowLocation.Y + _stateMachine.WindowSize.Height - card.TopCenterPos.Y;

        _input.Mouse.MoveMouseSmooth(cx, cy)
            .LeftButtonClick();
    }

    public void BlockCard(InGameCard card, InGameCard cardToBeBlocked)
    {
        (int, int) posSrc = (_stateMachine.WindowLocation.X + card.TopCenterPos.X, _stateMachine.WindowLocation.Y + _stateMachine.WindowSize.Height - card.TopCenterPos.Y);
        (int, int) posDest = (_stateMachine.WindowLocation.X + cardToBeBlocked.TopCenterPos.X, _stateMachine.WindowLocation.Y + _stateMachine.WindowSize.Height - cardToBeBlocked.TopCenterPos.Y);

        ForegroundIfGameNot();
        
        _input.Mouse.MoveMouseSmooth(posSrc.Item1, posSrc.Item2)
            .LeftButtonDown()
            .MoveMouseSmooth(posDest.Item1, posDest.Item2)
            .LeftButtonUp();
    }

    public void CommitOrPassOrSkipTurn()
    {
        ForegroundIfGameNot();
        
        Thread.Sleep(Random.Shared.Next(400, 600));
        
        _input.Keyboard.KeyPress(VirtualKeyCode.SPACE);
    }
    
    public void GameEndContinueAndReplay()
    {
        Thread.Sleep(4000);
        
        double continueBtnPosX = _stateMachine.WindowLocation.X + (_stateMachine.WindowSize.Width * 0.66);
        double continueBtnPosY = _stateMachine.WindowLocation.Y + (_stateMachine.WindowSize.Height * 0.90);

        for (int i = 0; i < 16; i++)
        {
            ForegroundIfGameNot();
            
            _stateMachine.UpdateGameDataAsync().GetAwaiter().GetResult();
            if (_stateMachine.GameState == EGameState.MenusDeckSelected)
                break;
            
            _input.Mouse.MoveMouseSmooth(continueBtnPosX, continueBtnPosY)
                .LeftButtonClick();
            
            Thread.Sleep(1500);
        }

        Thread.Sleep(1000);
    }

    public void ResetMousePosition()
    {
        double mouseX = _stateMachine.WindowLocation.X + (_stateMachine.WindowSize.Width * 0.5);
        double mouseY = _stateMachine.WindowLocation.Y + (_stateMachine.WindowSize.Height * 0.5);
        
        _input.Mouse.MoveMouseSmooth(mouseX, mouseY);
    }
}
