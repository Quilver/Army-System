// The character stats that grow on level-up and that a class caps (campaign only). Numbers (soldiers brought into
// battle) counts as a stat here. WeaponSkill stands for every weapon skill the character lists: each listed skill uses
// the same growth chance, but caps are per weapon class (StatCaps.ForWeapon).
public enum CharacterStat
{
    Numbers,
    Speed,
    Defence,
    Strength,
    Support,
    Charge,
    Aggression,
    MaxHealth,
    Morale,
    Discipline,
    WeaponSkill,
}
