namespace Amberstar.GameData;

public record CharacterValue(word CurrentValue, word MaxValue, word BonusValue)
{
    public int TotalCurrent => CurrentValue + BonusValue;
    public int TotalMax => MaxValue + BonusValue;
}