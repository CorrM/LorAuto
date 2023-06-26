using LorAuto.Client;

Console.Write("Downloading missing card sets ... ");

var cardSetsManager = new CardSetsManager("CardSets");

await cardSetsManager.DownloadMissingCardSetsAsync().ConfigureAwait(false);
await cardSetsManager.LoadCardSetsAsync().ConfigureAwait(false);

Console.WriteLine("Done");