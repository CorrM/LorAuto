using LorAuto;
using LorAuto.Client;
using LorAuto.Game;
using LorAuto.Strategies;

// TODO: Before any keyboard press, check if game are foreground, maybe for mouse too

var cardSetsManager = new CardSetsManager("CardSets");

Console.Write("Downloading missing card sets ... ");
//await cardSetsManager.DownloadMissingCardSetsAsync().ConfigureAwait(false);
Console.WriteLine("Done");

Console.Write("Load card sets ... ");
await cardSetsManager.LoadCardSetsAsync().ConfigureAwait(false);
Console.WriteLine("Done");

using var gameClientApi = new GameClientApi();
var stateMachine = new StateMachine(cardSetsManager, gameClientApi);

stateMachine.UpdateClientInfo();
await stateMachine.UpdateGameDataAsync().ConfigureAwait(false);
if (stateMachine.GameWindowHandle == IntPtr.Zero)
{
    Console.WriteLine("Legends of Runeterra isn't running!");
    return -1;
}

var bot = new Bot(stateMachine, new Generic(), GameRotationType.Standard, false);

while (true)
    await bot.ProcessAsync().ConfigureAwait(false);

return 0;
