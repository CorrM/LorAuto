using GameOverlay.Drawing;
using GameOverlay.Windows;
using LorAuto;
using LorAuto.Card.Model;
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
var windowGfx = new Graphics()
{
    MeasureFPS = true,
    PerPrimitiveAntiAliasing = true,
    TextAntiAliasing = true
};

var window = new StickyWindow(stateMachine.GameWindowHandle, windowGfx)
{
    FPS = 60,
    IsTopmost = true,
    IsVisible = true,
    BypassTopmost = true
};

window.DrawGraphics += (sender, e) =>
{
    Graphics? gfx = e.Graphics;
    if (gfx is null)
        return;

    SolidBrush rBrush = gfx.CreateSolidBrush(255, 0, 0);
    SolidBrush bBrush = gfx.CreateSolidBrush(0, 0, 255);
    SolidBrush gBrush = gfx.CreateSolidBrush(0, 255, 0);

    InGameCard card = null!;
    lock (stateMachine)
    {
        if (stateMachine.CardsOnBoard.CardsAttackOrBlock.Count == 0)
        {
            gfx.ClearScene();
            return;
        }

        try
        {
            card = stateMachine.CardsOnBoard.CardsAttackOrBlock[0];
        }
        catch
        {
            gfx.ClearScene();
            return;
        }
    }
    
    if (card.Type is GameCardType.Spell or GameCardType.Ability)
        return;
    
    gfx.ClearScene();
    
    int x = card.Position.X;
    int y = stateMachine.WindowSize.Height - card.Position.Y;
    
    gfx.DrawRectangle(
        gBrush,
        x,
        y,
        x + card.Size.Width,
        y + card.Size.Height,
        2.0f);
    
    gfx.DrawRectangle(
        rBrush,
        x,
        y,
        x + (card.Size.Width / 2),
        y + (card.Size.Height / 4),
        1.0f);
    
    gfx.DrawRectangle(
        bBrush,
        x + (card.Size.Width / 2),
        y,
        x + card.Size.Width,
        y + (card.Size.Height / 4),
        1.0f);
};

window.Create();

// BOT
ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger);
var bot = new Bot(stateMachine, new Generic(), GameRotationType.Standard, false, loggerFactory.CreateLogger<Bot>());

while (!cts.IsCancellationRequested)
{
    lock (stateMachine)
    {
        stateMachine.UpdateGameDataAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    await bot.ProcessAsync(cts.Token).ConfigureAwait(false);
    await Task.Delay(8).ConfigureAwait(false);
}

// Clean
window.Dispose();
Log.CloseAndFlush();

return 0;
