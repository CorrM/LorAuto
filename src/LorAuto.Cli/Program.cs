using GameOverlay.Drawing;
using GameOverlay.Windows;
using LorAuto;
using LorAuto.Card.Model;
using LorAuto.Cli;
using LorAuto.Client;
using LorAuto.Game;
using LorAuto.Strategies;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

// # Logger
Log.Logger = new LoggerConfiguration()
#if DEBUG
    .MinimumLevel.Debug()
#endif
    .WriteTo.Console()
    .CreateLogger();

// # Terminate handle
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    Log.Logger.Warning("Please note that the application's shutdown process has been initiated and may take a few moments to complete");
    eventArgs.Cancel = true;
    cts.Cancel();
};

// # Card sets
var cardSetsManager = new CardSetsManager("CardSets");

Log.Logger.Information("Downloading missing card sets starts");
//await cardSetsManager.DownloadMissingCardSetsAsync().ConfigureAwait(false);
Log.Logger.Information("Downloading missing card sets finished");

Log.Logger.Information("Loading card sets");
await cardSetsManager.LoadCardSetsAsync().ConfigureAwait(false);
Log.Logger.Information("Loading card sets finished");

// Game info and data
using var gameClientApi = new GameClientApi();
var stateMachine = new StateMachine(cardSetsManager, gameClientApi);

stateMachine.UpdateClientInfo();

if (stateMachine.GameWindowHandle == IntPtr.Zero)
{
    Log.Logger.Error("Legends of Runeterra isn't running!");
    return -1;
}

await stateMachine.UpdateGameDataAsync(cts.Token).ConfigureAwait(false);

// Game overlay
Log.Logger.Information("Overlay starts");

using var overlay = new BotOverlay(stateMachine);
overlay.Start();

// BOT
using ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger);
var bot = new Bot(stateMachine, new Generic(), GameRotationType.Standard, false, loggerFactory.CreateLogger<Bot>());

while (!cts.IsCancellationRequested)
{
    //stateMachine.UpdateGameDataAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    //await bot.ProcessAsync(cts.Token).ConfigureAwait(false);
    await Task.Delay(8).ConfigureAwait(false);
}

// Clean
Log.CloseAndFlush();

return 0;
