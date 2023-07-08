using System.Drawing;
using LorAuto;
using LorAuto.Cli;
using LorAuto.Client;
using LorAuto.Game;
using LorAuto.OCR;
using LorAuto.Strategies;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Tesseract;

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
    
    if (!cts.IsCancellationRequested)
        cts.Cancel();
};

// # Card sets
var cardSetsManager = new CardSetsManager("CardSets");

Log.Logger.Information("Downloading missing card sets starts");
await cardSetsManager.DownloadMissingCardSetsAsync().ConfigureAwait(false);
Log.Logger.Information("Downloading missing card sets finished");

Log.Logger.Information("Loading card sets");
await cardSetsManager.LoadCardSetsAsync().ConfigureAwait(false);
Log.Logger.Information("Loading card sets finished");

// # OCR
using var ocrManager = new OcrManager(@"./TessData", "eng");

// Game info and data
using var gameClientApi = new GameClientApi();
var stateMachine = new StateMachine(cardSetsManager, gameClientApi, ocrManager);

stateMachine.UpdateClientInfo();

if (stateMachine.GameWindowHandle == IntPtr.Zero)
{
    Log.Logger.Error("Legends of Runeterra isn't running!");
    return -1;
}

// # Game overlay
Log.Logger.Information("Overlay starts");

using var overlay = new BotOverlay(stateMachine);
overlay.Start();

// # BOT
using ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger);
var bot = new Bot(stateMachine, new Generic(), GameRotationType.Standard, false, loggerFactory.CreateLogger<Bot>());

while (!cts.IsCancellationRequested)
{
    //stateMachine.UpdateGameDataAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    await bot.ProcessAsync(cts.Token).ConfigureAwait(false);
    await Task.Delay(8).ConfigureAwait(false);
}

// Clean
Log.CloseAndFlush();
cts.Dispose();

return 0;
