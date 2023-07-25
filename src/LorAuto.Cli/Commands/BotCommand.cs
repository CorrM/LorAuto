using System.CommandLine;
using LorAuto.Bot;
using LorAuto.Bot.Model;
using LorAuto.Card;
using LorAuto.Client;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace LorAuto.Cli.Commands;

public sealed class BotCommand : RootCommand
{
    public BotCommand() : base("Bot for Legends of Runeterra")
    {
        var gameRotationOpt = new Option<EGameRotation>("-r", () => EGameRotation.Standard, "Game rotation to pick.");
        var strategyOpt = new Option<string>("-s", () => "generic", "Strategy bot will use.");
        var gamePortOpt = new Option<int>("-p", () => 21337, "Game client third party endpoints port.");
        var noPvpGameOpt = new Option<bool>("--noPVP", () => false, "Play game against pvp or AI.");
        var overlayOpt = new Option<bool>("--overlay", () => false, "Show overlay on top of LoR that help to indicate and debug.");

        AddOption(gameRotationOpt);
        AddOption(strategyOpt);
        AddOption(gamePortOpt);
        AddOption(noPvpGameOpt);
        AddOption(overlayOpt);

        this.SetHandler(async context =>
        {
            EGameRotation gameRotation = context.ParseResult.GetValueForOption(gameRotationOpt);
            string strategy = context.ParseResult.GetValueForOption(strategyOpt)!;
            int gamePort = context.ParseResult.GetValueForOption(gamePortOpt);
            bool isPvpGame = !context.ParseResult.GetValueForOption(noPvpGameOpt);
            bool overlay = context.ParseResult.GetValueForOption(overlayOpt);
            CancellationToken token = context.GetCancellationToken();

            context.ExitCode = await CommandHandler(gameRotation, strategy, gamePort, isPvpGame, overlay, token);
        });
    }

    private async Task<int> CommandHandler(
        EGameRotation gameRotation,
        string strategy,
        int gamePort,
        bool isPvpGame,
        bool showOverlay,
        CancellationToken ct)
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
        using var stateMachine = new StateMachine(cardSetsManager, gamePort);
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
            overlay = new BotOverlay(stateMachine);

            Log.Logger.Information("Overlay starts");
            overlay.Start();
        }

        // # BOT
        using ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger);
        ILogger<LorBot> botLogger = loggerFactory.CreateLogger<LorBot>();

        var botParams = new LorBotParams()
        {
            StateMachine = stateMachine,
            StrategyPluginName = strategy,
            GameRotation = gameRotation,
            IsPvp = isPvpGame,
            Logger = botLogger,
        };

        using var bot = new LorBot(botParams);
        while (!ct.IsCancellationRequested)
        {
            //stateMachine.UpdateGameDataAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            try
            {
                await bot.ProcessAsync(ct).ConfigureAwait(false);
                await Task.Delay(8, ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }
        }

        // # Clean
        await Log.CloseAndFlushAsync().ConfigureAwait(false);
        overlay?.Dispose();

        return 0;
    }
}
