﻿using System.CommandLine;
using System.CommandLine.Parsing;
using LorAuto.Bot;
using LorAuto.Bot.Model;
using LorAuto.Client;
using LorAuto.Game;
using LorAuto.OCR;
using LorAuto.Strategies;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace LorAuto.Cli.Commands;

public sealed class BotCommand : RootCommand
{
    public BotCommand() : base("Bot for Legends of Runeterra")
    {
        var gameRotationOpt = new Option<EGameRotation>("-r", () => EGameRotation.Standard, "Game rotation to pick.");
        var pvpGameOpt = new Option<bool>("-p", () => true, "Game pvp or AI.");
        var overlayOpt = new Option<bool>("--overlay", () => false, "Show overlay on top of LoR that help to indicate and debug.");
        
        AddOption(gameRotationOpt);
        AddOption(pvpGameOpt);
        AddOption(overlayOpt);
        
        this.SetHandler(async context =>
        {
            EGameRotation gameRotation = context.ParseResult.GetValueForOption(gameRotationOpt);
            bool isPvpGame = context.ParseResult.GetValueForOption(pvpGameOpt);
            bool overlay = context.ParseResult.GetValueForOption(overlayOpt);
            CancellationToken token = context.GetCancellationToken();
            
            context.ExitCode = await CommandHandler(isPvpGame, gameRotation, overlay, token);
        });
    }

    private async Task<int> CommandHandler(bool isPvpGame, EGameRotation gameRotation, bool showOverlay, CancellationToken ct)
    {
        // # Logger
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#endif
            .WriteTo.Console()
            .CreateLogger();

        // # Card sets
        var cardSetsManager = new CardSetsManager("CardSets");

        Log.Logger.Information("Downloading missing card sets starts");
        await cardSetsManager.DownloadMissingCardSetsAsync(ct).ConfigureAwait(false);
        Log.Logger.Information("Downloading missing card sets finished");

        Log.Logger.Information("Loading card sets");
        await cardSetsManager.LoadCardSetsAsync(ct).ConfigureAwait(false);
        Log.Logger.Information("Loading card sets finished");

        // # Game info and data
        using var stateMachine = new StateMachine(cardSetsManager);

        stateMachine.UpdateClientInfo();

        if (stateMachine.GameWindowHandle == IntPtr.Zero)
        {
            Log.Logger.Error("Legends of Runeterra isn't running!");
            return -1;
        }

        // # Game overlay
        BotOverlay? overlay = null;
        if (showOverlay)
        {
            Log.Logger.Information("Overlay starts");
            overlay = new BotOverlay(stateMachine);
            overlay.Start();
        }

        // # BOT
        Log.Logger.Information("Bot starts");
        using ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger);
        ILogger<LorBot> botLogger = loggerFactory.CreateLogger<LorBot>();
        var bot = new LorBot(stateMachine, new GenericStrategy(), gameRotation, isPvpGame, botLogger);

        while (!ct.IsCancellationRequested)
        {
            //stateMachine.UpdateGameDataAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            try
            {
                await bot.ProcessAsync(ct).ConfigureAwait(false);
                await Task.Delay(8, ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException e)
            {
            }
        }

        // # Clean
        await Log.CloseAndFlushAsync().ConfigureAwait(false);
        overlay?.Dispose();

        return 0;
    }
}
