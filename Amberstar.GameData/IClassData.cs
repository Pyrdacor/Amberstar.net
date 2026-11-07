namespace Amberstar.GameData;

/// <summary>
/// Class information which is used by the guilds (CLASSDAT.DAT in the original).
/// </summary>
public interface IClassData
{
	/// <summary>
	/// The released source code has 20 (MaxLevel in PLACES.ASM), the programs of the game allow 50
	/// (cmpi.b #50,Level_nr at $b044 in the German Atari ST program). The class data has the experience of 90 levels.
	/// </summary>
	public const int MaxLevel = 50;

	Class Class { get; }

	/// <summary>
	/// Maximum skill value for every skill.
	/// </summary>
	byte GetSkillMaxValue(Skill skill);

	/// <summary>
	/// Experience points which are needed to advance from the given level to the next one.
	/// </summary>
	uint GetExperienceForNextLevel(int level);

	/// <summary>
	/// The attacks per round are the level divided by this value (0: no change).
	/// </summary>
	word AttacksPerRoundLevel { get; }

	/// <summary>
	/// Additional hit points per level (the stamina adds 10% of its value as well).
	/// </summary>
	word HitPointsPerLevel { get; }

	/// <summary>
	/// Additional spell points per level (the intelligence adds 5% of its value as well).
	/// </summary>
	word SpellPointsPerLevel { get; }

	/// <summary>
	/// Additional spell learning points per level (the intelligence adds 5% of its value as well).
	/// </summary>
	word SpellLearningPointsPerLevel { get; }

	/// <summary>
	/// Spell schools of the class. If none, the class can't use magic.
	/// </summary>
	SpellSchoolFlags SpellSchools { get; }
}
