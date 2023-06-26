namespace LorAuto.Client.Model;

[Serializable]
public sealed class GameResultApiRequest
{
    public required int GameID { get; init; }
    public required bool LocalPlayerWon { get; init; }
}
