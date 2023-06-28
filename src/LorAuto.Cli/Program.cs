using LorAuto;
using LorAuto.Client;
using LorAuto.Game;
using LorAuto.Strategies;

var cardSetsManager = new CardSetsManager("CardSets");

Console.Write("Downloading missing card sets ... ");
//await cardSetsManager.DownloadMissingCardSetsAsync().ConfigureAwait(false);
Console.WriteLine("Done");

Console.Write("Load card sets ... ");
await cardSetsManager.LoadCardSetsAsync().ConfigureAwait(false);
Console.WriteLine("Done");

using var gameClientApi = new GameClientApi();
using var stateMachine = new StateMachine(cardSetsManager, gameClientApi);

bool targetHandle = stateMachine.Start();
if (!targetHandle)
{
    Console.WriteLine("Legends of Runeterra isn't running!");
    return -1;
}

while (!stateMachine.IsReady())
    Thread.Sleep(8);

var bot = new Bot(stateMachine, new Generic(), GameStyleType.Standard, false);

while (true)
    bot.Run();

return 0;
