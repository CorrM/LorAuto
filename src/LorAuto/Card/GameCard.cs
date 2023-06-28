namespace LorAuto.Card;

public enum GameCardType
{
    Spell,
    Unit,
    Ability,
    Trap,
    Landmark,
    Equipment,
}

public enum GameCardKeyword
{
    Burst,
    QuickStrike,
    Fast,
    Support,
    Lifesteal,
    Elusive,
    Imbue,
    Ephemeral,
    Slow,
    Barrier,
    Skill,
    AuraVisualFakeKeyword,
    Challenger,
    Overwhelm,
    Fearsome,
    Regeneration,
    CantBlock,
    LastBreath,
    SpellOverwhelm,
    Fleeting,
    Tough,
    DoubleStrike,
    Autoplay,
    Focus,
    Attune,
    Deep,
    Immobile,
    Plunder,
    Scout,
    Vulnerable,
    Flow,
    LandmarkVisualOnly,
    SpellShield,
    Fury,
    Augment,
    Lurker,
    Countdown,
    Impact,
    Attach,
    Formidable,
    Equipment,
    Boon,
    Evolve,
    Brash
}

[Serializable]
public class GameCard
{
    public string Name { get; init; } = null!;
    public string CardCode { get; init; } = null!;
    public int Cost { get; init; }
    public int Attack { get; init; }
    public int Health { get; init; }
    public GameCardType Type { get; init; }
    public GameCardKeyword[] Keywords { get; init; } = null!;
    public string Description { get; init; } = null!;

    public override string ToString()
    {
        return $"Card({Name} ({Cost}) T: {Type:G} A: {Attack} H: {Health})";
    }
}
