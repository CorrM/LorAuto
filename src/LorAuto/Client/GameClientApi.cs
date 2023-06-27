using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using LorAuto.Client.Model;

namespace LorAuto.Client;

internal enum GameClientApiRequestType
{
    ActiveDeck,
    CardPositions,
    GameResult
}

public sealed class GameClientApi : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<GameClientApiRequestType, string> _requestsMap;

    public GameClientApi(int port = 21337)
    {
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}/"),
            Timeout = TimeSpan.FromSeconds(10)
        };
        
        _requestsMap = new Dictionary<GameClientApiRequestType, string>()
        {
            { GameClientApiRequestType.ActiveDeck, "static-decklist" },
            { GameClientApiRequestType.CardPositions, "positional-rectangles" },
            { GameClientApiRequestType.GameResult, "game-result" },
        };
    }

    private async Task<string> GetRequestAsync(GameClientApiRequestType apiRequestType, CancellationToken ct = default)
    {
        return await _httpClient.GetStringAsync(_requestsMap[apiRequestType], ct).ConfigureAwait(false);
    }
    
    public async Task<(ActiveDeckApiResult?, Exception?)> GetActiveDeckAsync(CancellationToken ct = default)
    {
        try
        {
            string requestData = await GetRequestAsync(GameClientApiRequestType.ActiveDeck, ct).ConfigureAwait(false);
            var activeDeckJson = JsonSerializer.Deserialize<JsonObject?>(requestData);

            if (activeDeckJson is null)
                throw new UnreachableException();

            JsonNode? cardsInDeckJNode = activeDeckJson["CardsInDeck"];
            List<GameClientCardInDeck>? cardInDecks;
            if (cardsInDeckJNode is null)
            {
                cardInDecks = null;
            }
            else
            {
                cardInDecks = cardsInDeckJNode.Deserialize<Dictionary<string, int>>()!
                    .Select(d => new GameClientCardInDeck() { Code = d.Key, Count = d.Value })
                    .ToList();
            }

            var activeDeck = new ActiveDeckApiResult()
            {
                DeckCode = activeDeckJson["DeckCode"]?.GetValue<string>(),
                CardsInDeck = cardInDecks
            };
            return (activeDeck, null);
        }
        catch (Exception e)
        {
            return (null, e);
        }
    }
    
    public async Task<(CardPositionsApiRequest?, Exception?)> GetCardPositionsAsync(CancellationToken ct = default)
    {
        try
        {
            string requestData = await GetRequestAsync(GameClientApiRequestType.CardPositions, ct).ConfigureAwait(false);
            var cardPositions = JsonSerializer.Deserialize<CardPositionsApiRequest?>(requestData);
        
            return (cardPositions, null);
        }
        catch (Exception e)
        {
            return (null, e);
        }
    }
    
    public async Task<(GameResultApiRequest?, Exception?)> GetGameResultAsync(CancellationToken ct = default)
    {
        try
        {
            string requestData = await GetRequestAsync(GameClientApiRequestType.GameResult, ct).ConfigureAwait(false);
            var gameResult = JsonSerializer.Deserialize<GameResultApiRequest?>(requestData);
        
            return (gameResult, null);
        }
        catch (Exception e)
        {
            return (null, e);
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
