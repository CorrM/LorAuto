namespace LorAuto.Client.Model;

[Serializable]
internal sealed class GameResultApiResponse
{
    public required int GameID { get; init; }
    public required bool LocalPlayerWon { get; init; }
}
