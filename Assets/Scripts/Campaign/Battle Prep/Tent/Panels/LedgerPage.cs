using System.Collections.Generic;
using Campaign;
using CampaignUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // One page of the Roster Ledger: one character (spec TentHub.md §4.2, SU-54). Shows class, level and XP, soldier
    // stats against their class caps, weapon skills, equipment and cost to field.
    // Stats, the class and weapon skills are shown by their icons (SU-91), each naming itself in the tooltip on hover;
    // the Armour row (Deflect : Bruise : Bleed, each after its icon) sits below Defence. The icon rows are built at
    // runtime over the scene's text fields, which are hidden. A missing icon falls back to its name as text.
    // XP shows as XpNotches, one per point of the level (SU-115), built over the scene's XP bar. Stats include the points
    // gained on level-ups (RosterCharacter.Stat). The post-battle book also shows a page as it was before the battle's
    // rewards (a CharacterSnapshot) and animates it: SetLevel, HighlightRoll and SetRollValue.
    public class LedgerPage : MonoBehaviour
    {
        const float StatRowHeight = 22f;     // six rows fill the 132 px stat columns
        const float StatIconSize = 18f;
        const float SkillIconSize = 20f;
        const float ClassIconSize = 26f;
        const float IconSpacing = 5f;
        const float GroupGap = 10f;          // between the weapon skills, and between the armour ratio parts
        const string CapColor = "#8a7a62";

        static readonly CharacterStat[] LeftStats =
            { CharacterStat.Numbers, CharacterStat.MaxHealth, CharacterStat.Morale, CharacterStat.Discipline, CharacterStat.Speed };
        static readonly CharacterStat[] RightStats =     // Armour goes after Defence (index 0)
            { CharacterStat.Defence, CharacterStat.Strength, CharacterStat.Support, CharacterStat.Charge, CharacterStat.Aggression };

        [SerializeField] GameObject _content;
        [SerializeField] TextMeshProUGUI _empty, _pageNumber;
        [SerializeField] Image _portrait;
        [SerializeField] TextMeshProUGUI _name, _class, _level;
        [SerializeField] RectTransform _xpFill;
        [SerializeField] TextMeshProUGUI _statsLeft, _statsRight, _skills, _equipment, _footer;

        // Built by BuildIcons.
        bool _iconsBuilt;
        IconBadge _classBadge;
        IconText[] _leftStats, _rightStats;
        IconText _armour;
        IconText[] _armourParts;   // Deflect, Bruise, Bleed
        TextMeshProUGUI _skillsHeader;
        Transform _skillsRow;
        readonly List<IconText> _skillParts = new();
        readonly Dictionary<WeaponClass, IconText> _skillLines = new();
        XpNotches _notches;
        Color _textColor;

        RosterCharacter _roster;

        public XpNotches Notches => _notches;

        // Hides the character and the blank-page text (the page is used for something else).
        public void Hide()
        {
            _content.SetActive(false);
            _empty.gameObject.SetActive(false);
        }

        public void ShowEmpty(int pageNumber)
        {
            _content.SetActive(false);
            _empty.gameObject.SetActive(true);
            _pageNumber.text = pageNumber.ToString();
        }

        public void Show(RosterCharacter roster, int pageNumber) => Show(roster, pageNumber, null);

        // Shows the character with level, XP and stats from shown (as they were at that moment), or as they are now.
        public void Show(RosterCharacter roster, int pageNumber, CharacterSnapshot shown)
        {
            _roster = roster;
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
            BuildIcons();
            _class.text = _classBadge.Set(c.ClassIcon, c.ClassName) ? "" : c.ClassName;
            SetLevel(shown != null ? shown.Level : roster.level, shown != null ? shown.Xp : roster.xp);

            for (int i = 0; i < LeftStats.Length; i++)
                SetStat(_leftStats[i], LeftStats[i], StatValue(roster, shown, LeftStats[i]), c.CapFor(LeftStats[i]));
            for (int i = 0; i < RightStats.Length; i++)
                SetStat(_rightStats[i], RightStats[i], StatValue(roster, shown, RightStats[i]), c.CapFor(RightStats[i]));
            foreach (var line in _leftStats) ResetHighlight(line);
            foreach (var line in _rightStats) ResetHighlight(line);

            var eq = c.Equipment;
            var melee = eq?.MeleeWeapon;
            var ranged = eq?.RangedWeapon;
            var armour = eq?.Armour;
            SetArmour(armour);
            SetSkills(roster, shown);

            _equipment.text = string.Join("\n",
                melee != null ? $"<b>Melee</b>   {melee.DisplayName} <color=#6b5a45>({melee.Class}, +{melee.Damage} damage, +{melee.ChargeBonus} charge)</color>" : "<b>Melee</b>   none",
                ranged != null ? $"<b>Ranged</b>   {ranged.DisplayName} <color=#6b5a45>({ranged.Class}, range {ranged.Range:0})</color>" : "<b>Ranged</b>   none",
                armour != null ? $"<b>Armour</b>   {armour.DisplayName} <color=#6b5a45>(+{armour.DefenceBonus} defence)</color>" : "<b>Armour</b>   none");

            bool deployed = BattleReport.DeployedCharacters != null && BattleReport.DeployedCharacters.Contains(roster);
            _footer.text = $"Cost to field: <b>{roster.costToField} gold</b>" +
                           (deployed ? "   ·   <color=#3f6212><b>Deployed</b></color>" : "");
        }

        static int StatValue(RosterCharacter roster, CharacterSnapshot shown, CharacterStat stat) =>
            shown != null && shown.Stats.TryGetValue(stat, out int value) ? value : roster.Stat(stat);

        static int SkillValue(RosterCharacter roster, CharacterSnapshot shown, WeaponClass weaponClass) =>
            shown != null && shown.Skills.TryGetValue(weaponClass, out int value) ? value : roster.SkillLevel(weaponClass);

        static void SetStat(IconText line, CharacterStat stat, int value, int cap, string suffix = "") =>
            line.Set(GameIcons.For(stat), GameIcons.NameOf(stat), $"<b>{value}</b><color={CapColor}> / {cap}</color>{suffix}");

        // --- Animation (post-battle book, SU-115) ---

        public void SetLevel(int level, int xp)
        {
            BuildIcons();
            _level.text = $"Level {level}   ·   {xp}/{RosterCharacter.XpPerLevel} XP";
            _notches.Set(xp);
        }

        IconText LineOf(StatRoll roll)
        {
            if (roll.IsWeaponSkill) return _skillLines.TryGetValue(roll.WeaponClass, out var skill) ? skill : null;
            for (int i = 0; i < LeftStats.Length; i++) if (LeftStats[i] == roll.Stat) return _leftStats[i];
            for (int i = 0; i < RightStats.Length; i++) if (RightStats[i] == roll.Stat) return _rightStats[i];
            return null;
        }

        // Glows the stat's icon and tints its value, or puts it back to normal.
        public void HighlightRoll(StatRoll roll, bool on, Color glow)
        {
            IconText line = LineOf(roll);
            if (line == null) return;
            UiGlow.Set(line.Badge.Root.GetComponent<Image>(), glow, on ? 1f : 0f);
            line.Text.color = on ? glow : _textColor;
        }

        // Shows the stat's new value, followed by suffix (e.g. a green +1).
        public void SetRollValue(StatRoll roll, int value, string suffix)
        {
            IconText line = LineOf(roll);
            if (line == null || _roster == null || _roster.character == null) return;
            var c = _roster.character;
            if (roll.IsWeaponSkill)
                line.Set(GameIcons.For(roll.WeaponClass), GameIcons.NameOf(roll.WeaponClass),
                    $"<b>{value}</b><color={CapColor}>/{c.CapFor(roll.WeaponClass)}</color>{suffix}");
            else SetStat(line, roll.Stat, value, c.CapFor(roll.Stat), suffix);
        }

        void ResetHighlight(IconText line)
        {
            UiGlow.Clear(line.Badge.Root.GetComponent<Image>());
            line.Text.color = _textColor;
        }

        // The armour icon (tooltip: Armour and the armour's name), then Deflect, Bruise and Bleed each after its icon.
        // No armour shows the unarmoured ratio (SU-90).
        void SetArmour(ArmourProfile armour)
        {
            string name = GameIcons.NameOf(ArmourIcon.Armour);
            Sprite icon = GameIcons.For(ArmourIcon.Armour);
            _armour.Set(icon, icon != null ? name + ": " + (armour != null ? armour.DisplayName : "none") : name, "");

            int[] parts =
            {
                armour != null ? armour.Deflect : MitigationResolver.UnarmouredDeflect,
                armour != null ? armour.Bruise : MitigationResolver.UnarmouredBruise,
                armour != null ? armour.Bleed : MitigationResolver.UnarmouredBleed
            };
            for (int i = 0; i < parts.Length; i++)
            {
                var part = ArmourIcon.Deflect + i;
                _armourParts[i].Set(GameIcons.For(part), GameIcons.NameOf(part), $"<b>{parts[i]}</b>");
            }
        }

        // "Weapon skills", then each listed skill as its weapon class icon and level / cap.
        void SetSkills(RosterCharacter roster, CharacterSnapshot shown)
        {
            var c = roster.character;
            int count = 0;
            _skillLines.Clear();
            foreach (var skill in c.WeaponSkills)
            {
                if (count == _skillParts.Count)
                    _skillParts.Add(TentIcons.Pair(_skillsRow, "Skill " + count, SkillIconSize, _skills, GroupGap));
                _skillParts[count].Set(GameIcons.For(skill.Class), GameIcons.NameOf(skill.Class),
                    $"<b>{SkillValue(roster, shown, skill.Class)}</b><color={CapColor}>/{c.CapFor(skill.Class)}</color>");
                ResetHighlight(_skillParts[count]);
                _skillLines[skill.Class] = _skillParts[count];
                count++;
            }
            for (int i = count; i < _skillParts.Count; i++) _skillParts[i].Hide();
            _skillsHeader.text = count > 0 ? "<b>Weapon skills</b>" : "<b>Weapon skills</b>   none";
        }

        // Replaces the text stats, class and weapon skills with icon rows over the same places (once per page).
        void BuildIcons()
        {
            if (_iconsBuilt) return;
            _iconsBuilt = true;
            _textColor = _statsLeft.color;

            // XP notches over the old XP bar, a little taller (SU-115).
            var bar = (RectTransform)_xpFill.parent;
            if (bar.TryGetComponent(out Image barImage)) barImage.enabled = false;
            _xpFill.gameObject.SetActive(false);
            bar.sizeDelta = new Vector2(bar.sizeDelta.x, Mathf.Max(bar.sizeDelta.y, 16f));
            _notches = XpNotches.Create(bar, RosterCharacter.XpPerLevel, 4f);

            _classBadge = TentIcons.BadgeAtLeft(_class.transform, "Class Icon", ClassIconSize);

            var left = TentIcons.Overlay(_statsLeft.rectTransform, "Stat Icons Left");
            _leftStats = new IconText[LeftStats.Length];
            for (int i = 0; i < LeftStats.Length; i++)
                _leftStats[i] = TentIcons.Pair(TentIcons.Row(left, LeftStats[i].ToString(), i, StatRowHeight, IconSpacing),
                    LeftStats[i].ToString(), StatIconSize, _statsLeft);
            _statsLeft.gameObject.SetActive(false);

            var right = TentIcons.Overlay(_statsRight.rectTransform, "Stat Icons Right");
            _rightStats = new IconText[RightStats.Length];
            int row = 0;
            for (int i = 0; i < RightStats.Length; i++)
            {
                _rightStats[i] = TentIcons.Pair(TentIcons.Row(right, RightStats[i].ToString(), row++, StatRowHeight, IconSpacing),
                    RightStats[i].ToString(), StatIconSize, _statsRight);
                if (RightStats[i] != CharacterStat.Defence) continue;
                var armourRow = TentIcons.Row(right, "Armour", row++, StatRowHeight, IconSpacing);
                _armour = TentIcons.Pair(armourRow, "Armour", StatIconSize, _statsRight, GroupGap);
                _armourParts = new IconText[3];
                for (int p = 0; p < 3; p++)
                    _armourParts[p] = TentIcons.Pair(armourRow, (ArmourIcon.Deflect + p).ToString(), StatIconSize, _statsRight,
                        p < 2 ? GroupGap : 0f);
            }
            _statsRight.gameObject.SetActive(false);

            _skillsRow = TentIcons.Row(TentIcons.Overlay(_skills.rectTransform, "Skill Icons"), "Skills", 0,
                _skills.rectTransform.sizeDelta.y, IconSpacing);
            _skillsHeader = TentIcons.Text(_skillsRow, "Header", _skills);
            TentIcons.Spacer(_skillsRow, "Header Gap", GroupGap);
            _skills.gameObject.SetActive(false);
        }
    }
}
