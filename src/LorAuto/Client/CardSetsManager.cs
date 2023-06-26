using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LorAuto.Client;

public sealed class CardSetsManager
{
    private readonly string[] _forbiddenCardSets = { "set6ab" };
    private readonly string _cardSetsDirName;
    private readonly HttpClient _httpClient;

    public CardSetsManager(string cardSetsDirName)
    {
        _cardSetsDirName = cardSetsDirName;
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri("https://dd.b.pvp.net/latest/")
        };
    }

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
    
    public async Task DownloadMissingCardSetsAsync(CancellationToken ct = default)
    {
        string cardSetsBasePath = Path.Combine(Environment.CurrentDirectory, _cardSetsDirName);
        if (!Directory.Exists(cardSetsBasePath))
            Directory.CreateDirectory(cardSetsBasePath);

        byte[] coreBundlesBytes;
        try
        {
            coreBundlesBytes = await _httpClient.GetByteArrayAsync("core-en_us.zip", ct).ConfigureAwait(false);
        }
        catch (Exception e)
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

        string?[] existsCardSetsNames = Directory.EnumerateFiles(cardSetsBasePath)
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray();

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

        return true;
    }
}
