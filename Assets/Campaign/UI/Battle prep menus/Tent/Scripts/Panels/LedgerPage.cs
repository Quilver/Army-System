using System.Collections.Generic;
using Campaign;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // One page of the Roster Ledger: one character (spec TentHub.md §4.2, SU-54). Shows class, level and XP, soldier
    // stats against their class caps, weapon skills, equipment and cost to field.
    public class LedgerPage : MonoBehaviour
    {
        [SerializeField] GameObject _content;
        [SerializeField] TextMeshProUGUI _empty, _pageNumber;
        [SerializeField] Image _portrait;
        [SerializeField] TextMeshProUGUI _name, _class, _level;
        [SerializeField] RectTransform _xpFill;
        [SerializeField] TextMeshProUGUI _statsLeft, _statsRight, _skills, _equipment, _footer;

        public void ShowEmpty(int pageNumber)
        {
            _content.SetActive(false);
            _empty.gameObject.SetActive(true);
            _pageNumber.text = pageNumber.ToString();
        }

        public void Show(RosterCharacter roster, int pageNumber)
        {
            var c = roster.character;
            if (c == null)
            {
                ShowEmpty(pageNumber);
                return;
            }
            _content.SetActive(true);
            _empty.gameObject.SetActive(false);
            _pageNumber.text = pageNumber.ToString();

            _portrait.sprite = c.Portrait;
            _portrait.enabled = c.Portrait != null;
            _name.text = c.CharacterName;
            _class.text = c.ClassName;
            _level.text = $"Level {roster.level}   ·   {roster.xp}/{RosterCharacter.XpPerLevel} XP";
            _xpFill.anchorMax = new Vector2(roster.XpFraction, 1f);

            _statsLeft.text = string.Join("\n",
                Stat("Soldiers", c.Numbers, c.CapFor(CharacterStat.Numbers)),
                Stat("HP", c.MaxHealth, c.CapFor(CharacterStat.MaxHealth)),
                Stat("Morale", c.Morale, c.CapFor(CharacterStat.Morale)),
                Stat("Discipline", c.Discipline, c.CapFor(CharacterStat.Discipline)),
                Stat("Speed", c.Speed, c.CapFor(CharacterStat.Speed)));
            _statsRight.text = string.Join("\n",
                Stat("Defence", c.Defence, c.CapFor(CharacterStat.Defence)),
                Stat("Strength", c.Strength, c.CapFor(CharacterStat.Strength)),
                Stat("Support", c.Support, c.CapFor(CharacterStat.Support)),
                Stat("Charge", c.Charge, c.CapFor(CharacterStat.Charge)),
                Stat("Aggression", c.Aggression, c.CapFor(CharacterStat.Aggression)));

            var skills = new List<string>();
            foreach (var skill in c.WeaponSkills)
                skills.Add($"{skill.Class} <b>{skill.Level}</b><color=#8a7a62>/{c.CapFor(skill.Class)}</color>");
            _skills.text = "<b>Weapon skills</b>   " + (skills.Count > 0 ? string.Join("   ·   ", skills) : "none");

            var eq = c.Equipment;
            var melee = eq?.MeleeWeapon;
            var ranged = eq?.RangedWeapon;
            var armour = eq?.Armour;
            _equipment.text = string.Join("\n",
                melee != null ? $"<b>Melee</b>   {melee.DisplayName} <color=#6b5a45>({melee.Class}, +{melee.Damage} damage, +{melee.ChargeBonus} charge)</color>" : "<b>Melee</b>   none",
                ranged != null ? $"<b>Ranged</b>   {ranged.DisplayName} <color=#6b5a45>({ranged.Class}, range {ranged.Range:0})</color>" : "<b>Ranged</b>   none",
                armour != null ? $"<b>Armour</b>   {armour.DisplayName} <color=#6b5a45>(+{armour.DefenceBonus} defence)</color>" : "<b>Armour</b>   none");

            bool deployed = BattleReport.DeployedRoster != null && BattleReport.DeployedRoster.Contains(roster);
            _footer.text = $"Cost to field: <b>{roster.costToField} gold</b>" +
                           (deployed ? "   ·   <color=#3f6212><b>Deployed</b></color>" : "");
        }

        static string Stat(string label, int value, int cap) =>
            $"{label}<pos=62%><b>{value}</b><color=#8a7a62> / {cap}</color>";
    }
}
