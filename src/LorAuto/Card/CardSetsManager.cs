using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using LorAuto.Card.Model;

namespace LorAuto.Card;

/// <summary>
/// Manages game card sets.
/// </summary>
public sealed class CardSetsManager
{
    private readonly static HttpClient _httpClient;

    private readonly string[] _forbiddenCardSets = { "set6ab" };
    private readonly string _cardSetsDirName;

    /// <summary>
    /// Indicates whether the card sets are loaded.
    /// </summary>
    public bool CardSetsLoaded { get; private set; }

    /// <summary>
    /// Dictionary of card sets, where the key is the card set name and the value is the corresponding <see cref="GameCardSet"/>.
    /// </summary>
    public Dictionary<string, GameCardSet> CardSets { get; }

    static CardSetsManager()
    {
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri("https://dd.b.pvp.net/latest/")
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CardSetsManager"/> class.
    /// </summary>
    /// <param name="cardSetsDirName">The directory name for card sets.</param>
    public CardSetsManager(string cardSetsDirName)
    {
        _cardSetsDirName = cardSetsDirName;
        CardSets = new Dictionary<string, GameCardSet>();
    }

    /// <summary>
    /// Gets the path for the card sets directory.
    /// </summary>
    /// <returns>The path for the card sets directory.</returns>
    private string GetCardSetsPath()
    {
        return Path.Combine(Environment.CurrentDirectory, _cardSetsDirName);
    }

    /// <summary>
    /// Downloads a card set asynchronously.
    /// </summary>
    /// <param name="cardSetName">The name of the card set to download.</param>
    /// <param name="addIndent">Indicates whether to add indentation to the downloaded JSON file.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DownloadCardSetAsync(string cardSetName, bool addIndent = false, CancellationToken ct = default)
    {
        string cardSetJson = await _httpClient.GetStringAsync($"{cardSetName}/en_us/data/{cardSetName}-en_us.json", ct)
            .ConfigureAwait(false);

        string cardSetsBasePath = Path.Combine(Environment.CurrentDirectory, _cardSetsDirName);
        if (!Directory.Exists(cardSetsBasePath))
            Directory.CreateDirectory(cardSetsBasePath);

        string cardSetPath = Path.Combine(cardSetsBasePath, $"{cardSetName}.json");

        if (addIndent)
        {
            var jsonObject = JsonSerializer.Deserialize<JsonArray>(cardSetJson);
            if (jsonObject is null)
                throw new UnreachableException();

            cardSetJson = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions() { WriteIndented = true });
        }

        await File.WriteAllTextAsync(cardSetPath, cardSetJson, ct);
    }

    /// <summary>
    /// Parses the cards in a card set asynchronously.
    /// </summary>
    /// <param name="cardSetName">The name of the card set to parse.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation. The task result is the parsed <see cref="GameCardSet"/>.</returns>
    private async Task<GameCardSet> ParseCardSetCardsAsync(string cardSetName, CancellationToken ct = default)
    {
        string setFileName = $"{cardSetName}.json";
        string cardSetPath = Path.Combine(GetCardSetsPath(), setFileName);
        if (!File.Exists(cardSetPath))
            throw new FileNotFoundException($"CardSet('{cardSetName}') not found.", setFileName);

        await using FileStream fileStream = File.OpenRead(cardSetPath);
        JsonArray? cardsInSetJson = await JsonSerializer.DeserializeAsync<JsonArray>(fileStream, cancellationToken: ct).ConfigureAwait(false);
        if (cardsInSetJson is null)
            throw new UnreachableException();

        var cardsInSet = new Dictionary<string, GameCard>(cardsInSetJson.Count);
        foreach (JsonNode? cardJson in cardsInSetJson)
        {
            if (cardJson is null)
                throw new UnreachableException();

            GameCard gameCard = GameCard.FromJson(cardJson);
            cardsInSet.Add(gameCard.CardCode, gameCard);
        }

        return new GameCardSet()
        {
            Name = cardSetName,
            Cards = cardsInSet
        };
    }

    /// <summary>
    /// Gets the names of existing card sets.
    /// </summary>
    /// <returns>An array of card set names.</returns>
    public string[] GetExistsCardSetsNames()
    {
        string cardSetsBasePath = GetCardSetsPath();

        return Directory.EnumerateFiles(cardSetsBasePath)
            .Select(s => Path.GetFileNameWithoutExtension(s)!)
            .ToArray();
    }

    /// <summary>
    /// Downloads missing card sets asynchronously.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DownloadMissingCardSetsAsync(CancellationToken ct = default)
    {
        string cardSetsBasePath = GetCardSetsPath();
        if (!Directory.Exists(cardSetsBasePath))
            Directory.CreateDirectory(cardSetsBasePath);

        byte[] coreBundlesBytes;
        try
        {
            coreBundlesBytes = await _httpClient.GetByteArrayAsync("core-en_us.zip", ct).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return;
        }

        var cardSets = new List<string>();

        using (var coreBundleStream = new MemoryStream(coreBundlesBytes))
        using (var zipArchive = new ZipArchive(coreBundleStream, ZipArchiveMode.Read, false))
        {
            foreach (ZipArchiveEntry entry in zipArchive.Entries)
            {
                const string imgSetsPath = "en_us/img/sets/";
                if (!entry.FullName.StartsWith(imgSetsPath))
                    continue;

                bool isDir = entry.FullName.EndsWith('/');
                if (isDir)
                    continue;

                string cardSetName = Path.GetFileNameWithoutExtension(entry.Name);
                cardSets.Add(cardSetName);
            }
        }

        string[] existsCardSetsNames = GetExistsCardSetsNames();

        IEnumerable<Task> tasks = cardSets
            .Where(setName => !existsCardSetsNames.Contains(setName) && !_forbiddenCardSets.Contains(setName))
            .Select(setName => Task.Run(async () => await DownloadCardSetAsync(setName, true, ct).ConfigureAwait(false), ct));

        await Task.WhenAll(tasks).ConfigureAwait(false);

        // CardSets are big json file that are serialized all in memory
        // so, just collect in case its in LOH (Large Object Heap)
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    /// <summary>
    /// Deletes the card sets.
    /// </summary>
    /// <returns><c>true</c> if the card sets were successfully deleted; otherwise, <c>false</c>.</returns>
    public bool DeleteCardSets()
    {
        string cardSetsPath = Path.Combine(Environment.CurrentDirectory, _cardSetsDirName);
        if (!Directory.Exists(cardSetsPath))
            return true;

        try
        {
            Directory.Delete(cardSetsPath, true);
        }
        catch (Exception)
        {
            return false;
        }

        CardSetsLoaded = false;
        CardSets.Clear();

        return true;
    }

    /// <summary>
    /// Loads the card sets asynchronously.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadCardSetsAsync(CancellationToken ct = default)
    {
        // Clear old data
        CardSetsLoaded = false;
        CardSets.Clear();

        // Load card sets
        foreach (string cardSetName in GetExistsCardSetsNames())
        {
            if (ct.IsCancellationRequested)
                throw new TaskCanceledException();

            GameCardSet cardSet = await ParseCardSetCardsAsync(cardSetName, ct).ConfigureAwait(false);
            CardSets.Add(cardSetName, cardSet);
        }

        CardSetsLoaded = true;
    }
}
