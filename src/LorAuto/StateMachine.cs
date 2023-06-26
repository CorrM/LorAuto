using LorAuto.Client;

namespace LorAuto;

/// <summary>
/// Determines the game state and cards on board by using the LoR API and cv2 functionality
/// </summary>
public sealed class StateMachine
{
    private readonly CardSetsManager _cardSetsManager;

    public StateMachine(CardSetsManager cardSetsManager)
    {
        _cardSetsManager = cardSetsManager;
    }
}
