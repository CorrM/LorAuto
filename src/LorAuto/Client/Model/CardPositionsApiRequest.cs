﻿namespace LorAuto.Client.Model;

[Serializable]
public sealed class GameClientScreen
{
    public required int ScreenWidth { get; init; }
    public required int ScreenHeight { get; init; }
}

[Serializable]
public sealed class GameClientRectangle
{
    public required int CardID { get; init; }
    public required string CardCode { get; init; }
    public required int TopLeftX { get; init; }
    public required int TopLeftY { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required bool LocalPlayer { get; init; }
}

[Serializable]
public sealed class CardPositionsApiRequest
{
    public required string PlayerName { get; init; }
    public required string OpponentName { get; init; }
    public required string GameState { get; init; }
    public required GameClientScreen Screen { get; init; }
    public required List<GameClientRectangle> Rectangles { get; init; }
}