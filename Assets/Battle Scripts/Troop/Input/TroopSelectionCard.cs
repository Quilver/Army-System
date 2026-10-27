using System.Collections.Generic;
using System.Text;
using BattleUI;
using UnityEngine;
using UnityEngine.UI;

// Shows the first selected troop on the scene's unit card (BattleUI.UnitCard, bottom left): name, portrait and stats.
// The card is hidden while nothing is selected, and refreshed a few times a second so soldier counts stay current.
//   Name      the legacy stat block's unit name, otherwise the troop's GameObject name.
//   Portrait  the legacy stat block's portrait, otherwise the sprite of the troop's most numerous soldier type, tinted
//             in the army colour.
//   Stats     troop stats (spec §1.4) and one line per soldier type (spec §1.5, §1.6).
public sealed class TroopSelectionCard
{
    const float RefreshInterval = 0.25f;
    const float SearchInterval = 1f;

    sealed class ProfileTally
    {
        public SoldierProfile Profile;
        public int Living;
        public int Total;
    }

    readonly StringBuilder _text = new(512);
    readonly List<ProfileTally> _tallies = new();
    UnitCard _card;
    Graphic[] _graphics;
    Canvas _canvas;
    Troop _shown;
    float _nextRefresh;
    float _nextSearch;

    public void Show(IReadOnlyList<Troop> selected)
    {
        if (!FindCard()) return;
        Troop troop = selected.Count > 0 ? selected[0] : null;
        if (troop == null)
        {
            _shown = null;
            if (_card.gameObject.activeSelf) _card.SetContent(null, null, Color.white, false, string.Empty, string.Empty);
            return;
        }
        if (troop == _shown && Time.unscaledTime < _nextRefresh) return;
        _shown = troop;
        _nextRefresh = Time.unscaledTime + RefreshInterval;

        Tally(troop);
        Sprite portrait = Portrait(troop, out bool soldierSprite);
        _card.SetContent(troop, portrait, soldierSprite ? troop.ArmyColor : Color.white, soldierSprite,
            UnitName(troop), Stats(troop, selected.Count - 1));
    }

    // True when the screen position (origin bottom-left) is over the visible card, so clicks on it are not orders.
    public bool Contains(Vector2 screenPosition)
    {
        if (_card == null || !_card.isActiveAndEnabled || _graphics == null) return false;
        Camera camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
        foreach (var graphic in _graphics)
        {
            if (graphic == null || !graphic.isActiveAndEnabled) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, screenPosition, camera)) return true;
        }
        return false;
    }

    bool FindCard()
    {
        if (_card != null) return true;
        if (Time.unscaledTime < _nextSearch) return false;
        _nextSearch = Time.unscaledTime + SearchInterval;
        var cards = Object.FindObjectsByType<UnitCard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (cards.Length == 0) return false;
        _card = cards[0];
        _graphics = _card.GetComponentsInChildren<Graphic>(true);
        Canvas canvas = _card.GetComponentInParent<Canvas>(true);
        _canvas = canvas != null ? canvas.rootCanvas : null;
        _shown = null;
        return true;
    }

    // Living and total soldiers per soldier type, in roster order.
    void Tally(Troop troop)
    {
        _tallies.Clear();
        foreach (var soldier in troop.Soldiers)
        {
            if (soldier.Profile == null) continue;
            ProfileTally tally = null;
            foreach (var existing in _tallies)
                if (existing.Profile == soldier.Profile) tally = existing;
            if (tally == null)
            {
                tally = new ProfileTally { Profile = soldier.Profile };
                _tallies.Add(tally);
            }
            tally.Total++;
            if (soldier.IsAlive) tally.Living++;
        }
    }

    static string UnitName(Troop troop)
    {
        var legacy = troop.Stats;
        if (legacy != null && !string.IsNullOrEmpty(legacy.UnitName)) return legacy.UnitName;
        return troop.name;
    }

    Sprite Portrait(Troop troop, out bool soldierSprite)
    {
        soldierSprite = false;
        var legacy = troop.Stats;
        if (legacy != null && legacy.Portrait != null) return legacy.Portrait;

        ProfileTally largest = null;
        foreach (var tally in _tallies)
            if (tally.Profile.Sprite != null && (largest == null || tally.Total > largest.Total)) largest = tally;
        soldierSprite = largest != null;
        return largest != null ? largest.Profile.Sprite : null;
    }

    string Stats(Troop troop, int othersSelected)
    {
        _text.Clear();
        _text.Append("Soldiers ").Append(troop.LivingSoldierCount).Append('/').Append(troop.SoldierCount)
             .Append("   Move ").Append(troop.Move)
             .Append("   Morale ").Append(troop.MoraleState != null ? troop.MoraleState.BaseMorale : 0)
             .Append("   Discipline ").Append(troop.Discipline);
        if (othersSelected > 0) _text.Append("   (+").Append(othersSelected).Append(" more selected)");

        foreach (var tally in _tallies)
        {
            SoldierProfile profile = tally.Profile;
            _text.Append('\n').Append("<b>").Append(profile.ClassName).Append("</b> ")
                 .Append(tally.Living).Append('/').Append(tally.Total)
                 .Append(":  Spd ").Append(profile.Speed)
                 .Append("  Def ").Append(profile.Defence);
            if (profile.Armour != null && profile.Armour.DefenceBonus != 0) _text.Append('+').Append(profile.Armour.DefenceBonus);
            _text.Append("  Str ").Append(profile.Strength)
                 .Append("  Sup ").Append(profile.Support)
                 .Append("  Chg ").Append(profile.Charge)
                 .Append("  Agg ").Append(profile.Aggression)
                 .Append("  HP ").Append(profile.MaxHealth);

            _text.Append('\n').Append("    ");
            int weapons = 0;
            foreach (var weapon in profile.Weapons)
            {
                if (weapon == null) continue;
                if (weapons++ > 0) _text.Append(", ");
                _text.Append(weapon.DisplayName);
                int skill = profile.SkillFor(weapon.Class);
                if (skill > 0) _text.Append(" (skill ").Append(skill).Append(')');
            }
            if (weapons == 0) _text.Append("Unarmed");
            _text.Append("   ").Append(profile.Armour != null ? profile.Armour.DisplayName : "No armour");
        }
        return _text.ToString();
    }
}
