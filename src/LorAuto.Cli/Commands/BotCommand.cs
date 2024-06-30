using System.CommandLine;
using LorAuto.Bot;
using LorAuto.Bot.Model;
using LorAuto.Card;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;

namespace LorAuto.Cli.Commands;

public sealed class BotCommand : RootCommand
{
    public BotCommand() : base("Bot for Legends of Runeterra")
    {
        var gameRotationOpt = new Option<GameRotation>("-r", () => GameRotation.Standard, "Game rotation to pick.");
        var strategyOpt = new Option<string>("-s", () => "generic", "Strategy bot will use.");
        var gamePortOpt = new Option<int>("-p", () => 21337, "Game client third party endpoints port.");
        var noPvpGameOpt = new Option<bool>("--noPVP", () => false, "Play game against pvp or AI.");
        var overlayOpt = new Option<bool>(
            "--overlay",
            () => false,
            "Show overlay on top of LoR that help to indicate and debug."
        );

        AddOption(gameRotationOpt);
        AddOption(strategyOpt);
        AddOption(gamePortOpt);
        AddOption(noPvpGameOpt);
        AddOption(overlayOpt);

        this.SetHandler(
            async context =>
            {
                GameRotation gameRotation = context.ParseResult.GetValueForOption(gameRotationOpt);
                string strategy = context.ParseResult.GetValueForOption(strategyOpt)!;
                int gamePort = context.ParseResult.GetValueForOption(gamePortOpt);
                bool isPvpGame = !context.ParseResult.GetValueForOption(noPvpGameOpt);
                bool overlay = context.ParseResult.GetValueForOption(overlayOpt);
                CancellationToken token = context.GetCancellationToken();

                context.ExitCode = await CommandHandler(
                    gameRotation,
                    strategy,
                    gamePort,
                    isPvpGame,
                    overlay,
                    token
                );
            }
        );
    }

    private async Task<int> CommandHandler(
        GameRotation gameRotation,
        string strategy,
        int gamePort,
        bool isPvpGame,
        bool debugOverlay,
        CancellationToken ct
    )
    {
        // # Logger
        await using Logger logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#endif
            .WriteTo.Console()
            .CreateLogger();

        using ILoggerFactory loggerFactory = new SerilogLoggerFactory(logger);
        ILogger<LorBot> botLogger = loggerFactory.CreateLogger<LorBot>();

        // # Card sets
        var cardSetsManager = new CardSetsManager("CardSets");

        botLogger.LogInformation("Downloading missing card sets");
        await cardSetsManager.DownloadMissingCardSetsAsync(ct).ConfigureAwait(false);

        botLogger.LogInformation("Loading card sets");
        await cardSetsManager.LoadCardSetsAsync(ct).ConfigureAwait(false);

        var botParams = new LorBotParams()
        {
            Logger = botLogger,
            DebugOverlay = debugOverlay,
            CardSets = cardSetsManager,
            GamePort = gamePort,
            StrategyPluginName = strategy,
            GameRotation = gameRotation,
            IsPvp = isPvpGame,
        };

        using var bot = new LorBot(botParams);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(8, ct).ConfigureAwait(false);

                bool processStatus = await bot.ProcessAsync(ct).ConfigureAwait(false);
                if (!processStatus)
                {
                    await Task.Delay(3000, ct).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
        }

        // # Clean
        await Log.CloseAndFlushAsync().ConfigureAwait(false);

        return 0;
    }
}
