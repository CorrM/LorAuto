from enum import IntEnum


class EGameCardKeyword(IntEnum):
    """
    Represents the keywords or special abilities associated with game cards.
    """
    Burst = 0
    QuickStrike = 1
    Fast = 2
    Support = 3
    Lifesteal = 4
    Elusive = 5
    Imbue = 6
    Ephemeral = 7
    Slow = 8
    Barrier = 9
    Skill = 10
    AuraVisualFakeKeyword = 11
    Challenger = 12
    Overwhelm = 13
    Fearsome = 14
    Regeneration = 15
    CantBlock = 16
    LastBreath = 17
    SpellOverwhelm = 18
    Fleeting = 19
    Tough = 20
    DoubleStrike = 21
    Autoplay = 22
    Focus = 23
    Attune = 24
    Deep = 25
    Immobile = 26
    Plunder = 27
    Scout = 28
    Vulnerable = 29
    Flow = 30
    LandmarkVisualOnly = 31
    SpellShield = 32
    Fury = 33
    Augment = 34
    Lurker = 35
    Countdown = 36
    Impact = 37
    Attach = 38
    Formidable = 39
    Equipment = 40
    Boon = 41
    Evolve = 42
    Brash = 43