using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Troop cards (spec §6.1), drawn with IMGUI by TroopPlayerOrders:
//   Selected card  the first selected troop, bottom-left corner; its side panels extend to the right.
//   Hover card     the troop under the cursor (any team), bottom-right corner; its side panels extend to the left. It is
//                  not drawn for the selected troop. It stays up for HoverGraceSeconds after the cursor leaves the troop,
//                  and for as long as the cursor is over the card, so the cursor can reach it and click a portrait.
// Main block, top to bottom:
//   - title: every character's name joined with " + ", then (living soldiers / starting soldiers); wraps when long;
//   - the characters' portraits side by side; clicking a portrait shows or hides that character's side panel;
//   - Morale bar (blue, Effective Morale / initial, led by the Morale icon) and Shock bar (red) below it, on the same
//     scale, so the troop breaks when the red bar passes the blue one (spec §5.3); a white tick on the Shock bar marks
//     the current break point. The Shock label is drawn after the tick, at the bar's left end, or at its right end when
//     the tick would fall on the label (low morale puts the tick at the left);
//   - formation state.
// Side panel (one per character whose portrait was clicked; hidden by default): character name, class icon (the class
// name when the class has no icon), living / starting soldiers, one column of soldier stats (HP, Defence, Armour,
// Support, Charge, Aggression), each equipped weapon with its weapon class icon, skill and damage, armour name.
// Stats are shown by their icon (GameIcons, SU-91) rather than their name; the Armour row shows the armour icon, then
// Deflect, Bruise and Bleed each after its own icon. The name of an icon is shown in a tooltip while the cursor is over
// it. An icon that is missing falls back to the name as text.
// Sizes are set for a 1080 px tall screen and scale with the screen height. The legacy uGUI unit card
// (BattleUI.UnitCard and the Canvas's SelectedUnit object) is hidden, since these cards replace it.
public sealed class TroopCardHud
{
    const float ReferenceHeight = 1080f;
    const float MinScale = 0.8f;
    const float MaxScale = 2.5f;
    const float Margin = 12f;
    const float Gap = 6f;
    const float Padding = 10f;
    const float MainWidth = 320f;
    const float PortraitMinWidth = 150f;  // the main block widens when several portraits would be narrower than this
    const float TitleHeight = 42f;        // one line of the title; a long troop name wraps and makes the card taller
    const float PortraitHeight = 240f;
    const float BarHeight = 28f;
    const float BarGap = 4f;
    const float SideWidth = 300f;
    const float LineHeight = 30f;
    const float AccentHeight = 4f;
    const float BorderWidth = 3f;
    const float SoldierSpriteFraction = 0.55f;   // fallback soldier sprite, as a fraction of the portrait cell
    const float HoverGraceSeconds = 1f;
    const float IconFraction = 0.85f;            // icon size as a fraction of the row height
    const float IconTextGap = 6f;                // between an icon and its value
    const float ArmourPartGap = 12f;             // between the Deflect, Bruise and Bleed groups
    const float TooltipPadding = 6f;
    const float TooltipOffset = 16f;
    const float BarTextPadding = 8f;             // between a bar's label and the end of the bar it sits at
    const float BreakTickWidth = 2f;
    const float LabelTickClearance = 6f;         // the tick must be this far past the label for the label to stay left
    const float NoTick = -1f;
    const float LegacySearchInterval = 1f;
    const string LegacyCardObjectName = "SelectedUnit";

    static readonly Color PanelColor = new(0.07f, 0.07f, 0.09f, 0.88f);
    static readonly Color CellColor = new(1f, 1f, 1f, 0.07f);
    static readonly Color HighlightColor = new(1f, 1f, 1f, 0.08f);
    static readonly Color CaptionColor = new(1f, 1f, 1f, 0.6f);
    static readonly Color LowColor = new(1f, 0.45f, 0.35f);
    static readonly Color MoraleColor = new(0.22f, 0.48f, 1f);
    static readonly Color ShockColor = new(0.9f, 0.18f, 0.18f);
    static readonly Color BreakTickColor = new(1f, 1f, 1f, 0.9f);
    static readonly Color TooltipColor = new(0.04f, 0.04f, 0.05f, 0.95f);
    static readonly Color TooltipBorderColor = new(1f, 1f, 1f, 0.25f);

    sealed class CharacterTally
    {
        public CharacterProfile Character;
        public int Living;
        public int Total;
    }

    readonly List<CharacterTally> _tallies = new();
    readonly HashSet<(int Troop, int Character)> _expanded = new();   // side panels opened by clicking a portrait
    readonly StringBuilder _text = new(64);
    readonly GUIContent _content = new();
    readonly List<Rect> _rects = new();      // GUI-space rects of the cards drawn last frame, for Contains
    readonly List<Rect> _drawing = new();    // rects of the cards being drawn this frame
    Rect _hoverBounds;
    Troop _selected;
    Troop _hover;
    float _hoverLostTime = -1f;
    float _scale = -1f;
    float _nextLegacySearch;
    string _tooltip;                         // name of the icon under the cursor, drawn after the cards
    GUIStyle _title, _sideTitle, _caption, _cellCaption, _value, _body, _bodyValue, _barText, _barTextRight, _tooltipStyle;

    // Called every frame after the hover troop is known. mouseScreen has its origin at the bottom-left.
    public void Update(IReadOnlyList<Troop> selected, Troop hovered, Vector2 mouseScreen)
    {
        HideLegacyCards();
        _selected = selected.Count > 0 ? selected[0] : null;

        bool overHoverCard = _hover != null && _hoverBounds.Contains(TroopScreenSpace.ToGui(mouseScreen));
        if (hovered != null)
        {
            _hover = hovered;
            _hoverLostTime = -1f;
        }
        else if (overHoverCard || _hover == null)
        {
            _hoverLostTime = -1f;
        }
        else if (_hoverLostTime < 0f)
        {
            _hoverLostTime = Time.unscaledTime;
        }
        else if (Time.unscaledTime - _hoverLostTime > HoverGraceSeconds)
        {
            _hover = null;
            _hoverLostTime = -1f;
        }
    }

    // True when the screen position (origin bottom-left) is over a card, so clicks on it are not orders.
    public bool Contains(Vector2 screenPosition)
    {
        Vector2 gui = TroopScreenSpace.ToGui(screenPosition);
        foreach (var rect in _rects)
            if (rect.Contains(gui)) return true;
        return false;
    }

    public void Draw()
    {
        bool repaint = Event.current.type == EventType.Repaint;
        if (repaint) _drawing.Clear();
        EnsureStyles();
        _tooltip = null;

        if (_selected != null) DrawCard(_selected, true, repaint, out _);
        if (_hover != null && _hover != _selected) DrawCard(_hover, false, repaint, out _hoverBounds);
        else _hoverBounds = Rect.zero;
        DrawTooltip();

        if (!repaint) return;
        _rects.Clear();
        _rects.AddRange(_drawing);
    }

    // --- Card layout ---

    void DrawCard(Troop troop, bool anchorLeft, bool record, out Rect bounds)
    {
        Tally(troop);
        float s = _scale;
        float margin = Margin * s;
        float gap = Gap * s;
        float pad = Padding * s;
        int portraits = Mathf.Max(1, _tallies.Count);
        float mainWidth = Mathf.Max(MainWidth * s, 2f * pad + portraits * PortraitMinWidth * s + (portraits - 1) * gap);
        float sideWidth = SideWidth * s;
        string title = TroopTitle(troop);
        float titleHeight = Mathf.Max(TitleHeight * s, _title.CalcHeight(new GUIContent(title), mainWidth - 2f * pad));
        float height = titleHeight
            + (AccentHeight + Padding + PortraitHeight + Gap + 2f * BarHeight + BarGap + Gap + LineHeight + Padding) * s;
        float y = Screen.height - margin - height;
        float mainX = anchorLeft ? margin : Screen.width - margin - mainWidth;

        var main = new Rect(mainX, y, mainWidth, height);
        DrawMainBlock(main, troop, title, titleHeight, pad, gap);
        bounds = main;
        if (record) _drawing.Add(main);

        int slot = 0;
        foreach (var tally in _tallies)
        {
            if (!IsExpanded(troop, tally.Character)) continue;
            float x = anchorLeft
                ? mainX + mainWidth + gap + slot * (sideWidth + gap)
                : mainX - (slot + 1) * (sideWidth + gap);
            var side = new Rect(x, y, sideWidth, height);
            DrawSidePanel(side, troop, tally, pad);
            bounds = Union(bounds, side);
            if (record) _drawing.Add(side);
            slot++;
        }
    }

    // Every character's name joined with " + ", then (living / starting soldiers).
    string TroopTitle(Troop troop)
    {
        _text.Clear();
        foreach (var tally in _tallies)
        {
            if (_text.Length > 0) _text.Append(" + ");
            _text.Append(tally.Character.CharacterName);
        }
        if (_text.Length == 0) _text.Append(troop.name);
        _text.Append(" (").Append(troop.LivingSoldierCount).Append('/').Append(troop.SoldierCount).Append(')');
        return _text.ToString();
    }

    // Title, portraits, Morale and Shock bars, formation.
    void DrawMainBlock(Rect area, Troop troop, string title, float titleHeight, float pad, float gap)
    {
        float s = _scale;
        Fill(area, PanelColor);
        Fill(new Rect(area.x, area.y, area.width, AccentHeight * s), troop.ArmyColor);

        float x = area.x + pad;
        float y = area.y + (AccentHeight + Padding) * s;
        float inner = area.width - 2f * pad;
        GUI.Label(new Rect(x, y, inner, titleHeight), title, _title);
        y += titleHeight;

        DrawPortraits(new Rect(x, y, inner, PortraitHeight * s), troop, gap);
        y += (PortraitHeight + Gap) * s;

        // Same scale for both bars (initial Effective Morale = full width): the troop breaks when Shock passes EM.
        float initial = Mathf.Max(1f, troop.InitialEffectiveMorale);
        float morale = Mathf.Max(0f, troop.EffectiveMorale);
        float barHeight = BarHeight * s;
        var moraleBar = new Rect(x, y, inner, barHeight);
        string moraleText = troop.EffectiveMorale.ToString("0") + " / " + troop.InitialEffectiveMorale.ToString("0");
        Sprite moraleIcon = GameIcons.For(CharacterStat.Morale);
        DrawBar(moraleBar, morale / initial, MoraleColor, moraleIcon != null ? moraleText : "Morale  " + moraleText,
            moraleIcon, GameIcons.NameOf(CharacterStat.Morale));
        y += barHeight + BarGap * s;
        var shockBar = new Rect(x, y, inner, barHeight);
        DrawBar(shockBar, troop.Shock / initial, ShockColor, "Shock  " + troop.Shock.ToString("0"),
            tickFraction: morale / initial);
        y += barHeight + Gap * s;

        var formationRow = new Rect(x, y, inner, LineHeight * s);
        GUI.Label(formationRow, "Formation", _caption);
        // A disordered troop (SU-104) shows the seconds left before it can move again.
        string formation = troop.IsDisordered
            ? "Disordered " + Mathf.CeilToInt(troop.DisorderRemaining) + "s"
            : troop.FormationMode.ToString();
        DrawValue(formationRow, formation, _bodyValue, troop.FormationMode == FormationMode.Rout || troop.IsDisordered);
    }

    // One cell per character, side by side. Clicking a cell shows or hides that character's side panel.
    void DrawPortraits(Rect row, Troop troop, float gap)
    {
        Event current = Event.current;
        if (_tallies.Count == 0)
        {
            Fill(row, CellColor);
            GUI.Label(row, "No portrait", _cellCaption);
            return;
        }

        float cellWidth = (row.width - (_tallies.Count - 1) * gap) / _tallies.Count;
        for (int i = 0; i < _tallies.Count; i++)
        {
            CharacterProfile character = _tallies[i].Character;
            var cell = new Rect(row.x + i * (cellWidth + gap), row.y, cellWidth, row.height);
            bool expanded = IsExpanded(troop, character);

            if (current.type == EventType.MouseDown && current.button == 0 && cell.Contains(current.mousePosition))
            {
                Toggle(troop, character);
                current.Use();
            }

            Fill(cell, CellColor);
            if (character.Portrait != null)
                DrawSprite(cell, character.Portrait, Color.white);
            else if (character.SoldierSprite != null)
                DrawSprite(Shrink(cell, SoldierSpriteFraction), character.SoldierSprite, troop.ArmyColor);
            else
                GUI.Label(cell, character.CharacterName, _cellCaption);

            if (cell.Contains(current.mousePosition)) Fill(cell, HighlightColor);
            if (expanded) Outline(cell, troop.ArmyColor, BorderWidth * _scale);
        }
    }

    // Character name and class icon, living / starting soldiers, one column of soldier stats, weapons, armour.
    void DrawSidePanel(Rect area, Troop troop, CharacterTally tally, float pad)
    {
        float s = _scale;
        CharacterProfile character = tally.Character;
        Fill(area, PanelColor);
        Color army = troop.ArmyColor;
        Fill(new Rect(area.x, area.y, area.width, AccentHeight * s), new Color(army.r, army.g, army.b, 0.6f));

        float x = area.x + pad;
        float inner = area.width - 2f * pad;
        float line = LineHeight * s;
        float y = area.y + (AccentHeight + Padding) * s;

        string count = tally.Living + " / " + tally.Total;
        float countWidth = _value.CalcSize(new GUIContent(count)).x;
        GUI.Label(new Rect(x, y, Mathf.Max(0f, inner - countWidth - pad), TitleHeight * s), character.CharacterName, _sideTitle);
        DrawValue(new Rect(x + inner - countWidth, y, countWidth, TitleHeight * s), count, _value,
            tally.Total > 0 && tally.Living * 2 < tally.Total);
        y += TitleHeight * s;
        Sprite classIcon = character.ClassIcon;
        if (classIcon != null) Icon(new Rect(x, y, line, line), classIcon, character.ClassName);
        else GUI.Label(new Rect(x, y, inner, line), character.ClassName, _caption);
        y += line * 1.3f;

        ArmourProfile armour = character.Armour;
        string defence = character.Defence.ToString();
        if (armour != null && armour.DefenceBonus != 0) defence += "+" + armour.DefenceBonus;
        StatRow(x, ref y, inner, line, CharacterStat.MaxHealth, character.MaxHealth.ToString());
        StatRow(x, ref y, inner, line, CharacterStat.Defence, defence);
        ArmourRow(x, ref y, inner, line, armour);
        StatRow(x, ref y, inner, line, CharacterStat.Support, character.Support.ToString());
        StatRow(x, ref y, inner, line, CharacterStat.Charge, character.Charge.ToString());
        StatRow(x, ref y, inner, line, CharacterStat.Aggression, character.Aggression.ToString());
        y += line * 0.4f;

        // The melee weapon, then the ranged weapon if any, each with the character's skill in its class
        // (0 = unskilled) and its damage.
        MeleeWeaponProfile melee = character.MeleeWeapon;
        WeaponRow(x, ref y, inner, line, character, melee, DamageRules.RawDamage(character.Strength, melee.Damage, 0, false));
        RangedWeaponProfile ranged = character.RangedWeapon;
        if (ranged != null)
            WeaponRow(x, ref y, inner, line, character, ranged, RangedRules.RawDamage(ranged.Damage));
        GUI.Label(new Rect(x, y, inner, line), armour != null ? armour.DisplayName : "No armour", _caption);
    }

    // The weapon class icon (its skill name on hover), the character's skill in that class (0 = unskilled), the weapon's
    // name, then its damage. Without an icon: "Name (skill)". Damage shown is, for melee, Strength + weapon damage
    // (spec §4.6, before Domination); for ranged, the weapon's damage only (Q-5).
    void WeaponRow(float x, ref float y, float width, float height, CharacterProfile character, WeaponProfile weapon,
        int damage)
    {
        var row = new Rect(x, y, width, height);
        int skill = character.SkillFor(weapon.Class);
        Sprite icon = GameIcons.For(weapon.Class);
        if (icon != null)
        {
            Rect iconRect = IconRect(x, y, height);
            Icon(iconRect, icon, GameIcons.NameOf(weapon.Class));
            float textX = iconRect.xMax + IconTextGap * _scale;
            GUI.Label(new Rect(textX, y, Mathf.Max(0f, width - (textX - x)), height), skill + "   " + weapon.DisplayName, _body);
        }
        else
        {
            GUI.Label(row, weapon.DisplayName + " (" + skill + ")", _body);
        }
        GUI.Label(row, damage + " dmg", _bodyValue);
        y += height;
    }

    // The stat's icon (its name when there is no icon) on the left, the value on the right.
    void StatRow(float x, ref float y, float width, float height, CharacterStat stat, string value)
    {
        var rect = new Rect(x, y, width, height);
        Sprite icon = GameIcons.For(stat);
        if (icon != null) Icon(IconRect(x, y, height), icon, GameIcons.NameOf(stat));
        else GUI.Label(rect, GameIcons.NameOf(stat), _caption);
        GUI.Label(rect, value, _bodyValue);
        y += height;
    }

    // The armour icon (tooltip: Armour and the armour's name), then the Deflect : Bruise : Bleed ratio (spec §1.6, SU-89)
    // on the right, each part after its own icon. No armour shows the unarmoured ratio (SU-90). Falls back to
    // "Armour  D : B : B" when an icon is missing.
    void ArmourRow(float x, ref float y, float width, float height, ArmourProfile armour)
    {
        var rect = new Rect(x, y, width, height);
        int deflect = armour != null ? armour.Deflect : MitigationResolver.UnarmouredDeflect;
        int bruise = armour != null ? armour.Bruise : MitigationResolver.UnarmouredBruise;
        int bleed = armour != null ? armour.Bleed : MitigationResolver.UnarmouredBleed;

        Sprite armourIcon = GameIcons.For(ArmourIcon.Armour);
        string armourName = GameIcons.NameOf(ArmourIcon.Armour);
        if (armourIcon != null)
            Icon(IconRect(x, y, height), armourIcon, armourName + ": " + (armour != null ? armour.DisplayName : "none"));
        else
            GUI.Label(rect, armourName, _caption);

        Sprite deflectIcon = GameIcons.For(ArmourIcon.Deflect);
        Sprite bruiseIcon = GameIcons.For(ArmourIcon.Bruise);
        Sprite bleedIcon = GameIcons.For(ArmourIcon.Bleed);
        if (deflectIcon != null && bruiseIcon != null && bleedIcon != null)
        {
            float gap = ArmourPartGap * _scale;
            float right = IconValue(rect.xMax, y, height, bleedIcon, GameIcons.NameOf(ArmourIcon.Bleed), bleed.ToString());
            right = IconValue(right - gap, y, height, bruiseIcon, GameIcons.NameOf(ArmourIcon.Bruise), bruise.ToString());
            IconValue(right - gap, y, height, deflectIcon, GameIcons.NameOf(ArmourIcon.Deflect), deflect.ToString());
        }
        else
        {
            GUI.Label(rect, deflect + " : " + bruise + " : " + bleed, _bodyValue);
        }
        y += height;
    }

    // Draws the value right-aligned at `right` with its icon just left of it. Returns the icon's left edge.
    float IconValue(float right, float y, float height, Sprite icon, string name, string value)
    {
        _content.text = value;
        float textWidth = _bodyValue.CalcSize(_content).x;
        GUI.Label(new Rect(right - textWidth, y, textWidth, height), value, _bodyValue);
        float size = height * IconFraction;
        float iconX = right - textWidth - IconTextGap * _scale - size;
        Icon(new Rect(iconX, y + (height - size) * 0.5f, size, size), icon, name);
        return iconX;
    }

    // --- Side panel toggles ---

    bool IsExpanded(Troop troop, CharacterProfile character) =>
        _expanded.Contains((troop.GetInstanceID(), character.GetInstanceID()));

    void Toggle(Troop troop, CharacterProfile character)
    {
        var key = (troop.GetInstanceID(), character.GetInstanceID());
        if (!_expanded.Remove(key)) _expanded.Add(key);
    }

    // Living and starting soldiers per character, in the troop's character order.
    void Tally(Troop troop)
    {
        _tallies.Clear();
        if (troop.Characters != null)
            foreach (var character in troop.Characters)
                if (character != null && Find(character) == null)
                    _tallies.Add(new CharacterTally { Character = character });

        foreach (var soldier in troop.Soldiers)
        {
            if (soldier.Character == null) continue;
            CharacterTally tally = Find(soldier.Character);
            if (tally == null)
            {
                tally = new CharacterTally { Character = soldier.Character };
                _tallies.Add(tally);
            }
            tally.Total++;
            if (soldier.IsAlive) tally.Living++;
        }
    }

    CharacterTally Find(CharacterProfile character)
    {
        foreach (var tally in _tallies)
            if (tally.Character == character) return tally;
        return null;
    }

    // --- Legacy card ---

    // The scene's uGUI unit card sits where the selected card is drawn, so it is hidden.
    void HideLegacyCards()
    {
        if (Time.unscaledTime < _nextLegacySearch) return;
        _nextLegacySearch = Time.unscaledTime + LegacySearchInterval;
        foreach (var rect in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (rect.name == LegacyCardObjectName) rect.gameObject.SetActive(false);
    }

    // --- Drawing helpers ---

    void EnsureStyles()
    {
        float scale = Mathf.Clamp(Screen.height / ReferenceHeight, MinScale, MaxScale);
        if (_title != null && Mathf.Approximately(scale, _scale)) return;
        _scale = scale;
        _title = Style(30, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        _title.wordWrap = true;
        _sideTitle = Style(24, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        _caption = Style(20, FontStyle.Normal, TextAnchor.MiddleLeft, CaptionColor);
        _value = Style(24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        _body = Style(21, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
        _bodyValue = Style(21, FontStyle.Bold, TextAnchor.MiddleRight, Color.white);
        _cellCaption = Style(18, FontStyle.Normal, TextAnchor.MiddleCenter, CaptionColor);
        _barText = Style(18, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        _barText.padding = new RectOffset(Mathf.RoundToInt(BarTextPadding * scale), 0, 0, 0);
        _barTextRight = Style(18, FontStyle.Bold, TextAnchor.MiddleRight, Color.white);
        _barTextRight.padding = new RectOffset(0, Mathf.RoundToInt(BarTextPadding * scale), 0, 0);
        _tooltipStyle = Style(18, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
    }

    GUIStyle Style(float size, FontStyle fontStyle, TextAnchor anchor, Color color)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(size * _scale),
            fontStyle = fontStyle,
            alignment = anchor,
            wordWrap = false,
            clipping = TextClipping.Clip,
            richText = false,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };
        style.normal.textColor = color;
        return style;
    }

    // A track with a filled part (fraction 0-1 of its width) and a label over it, led by an icon when one is given.
    // With a tick (tickFraction 0-1 of the width; NoTick for none) a white tick is drawn there, then the label: at the
    // left end, or at the right end when the tick would fall on it (LabelAtRightEnd).
    void DrawBar(Rect rect, float fraction, Color color, string label, Sprite icon = null, string iconName = null,
        float tickFraction = NoTick)
    {
        Fill(rect, CellColor);
        Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), color);
        var labelRect = rect;
        if (icon != null)
        {
            float size = rect.height * IconFraction;
            var iconRect = new Rect(rect.x + 4f * _scale, rect.y + (rect.height - size) * 0.5f, size, size);
            Icon(iconRect, icon, iconName);
            labelRect.xMin = iconRect.xMax;
        }
        GUIStyle labelStyle = _barText;
        if (tickFraction >= 0f)
        {
            float tickX = rect.x + rect.width * Mathf.Clamp01(tickFraction);
            float tickWidth = BreakTickWidth * _scale;
            Fill(new Rect(tickX - 0.5f * tickWidth, rect.y, tickWidth, rect.height), BreakTickColor);
            _content.text = label;
            float labelWidth = _barText.CalcSize(_content).x;   // includes the left padding
            if (LabelAtRightEnd(tickX - labelRect.xMin, labelWidth, LabelTickClearance * _scale))
                labelStyle = _barTextRight;
        }
        GUI.Label(labelRect, label, labelStyle);
    }

    // True when a bar's label, normally at its left end and labelWidth wide, would be covered by a tick tickOffset from
    // the label's left edge (within clearance past the label), so the label goes to the bar's right end. Internal for
    // tests.
    internal static bool LabelAtRightEnd(float tickOffset, float labelWidth, float clearance) =>
        tickOffset <= labelWidth + clearance;

    // A square icon filling the height of a row of the given height, at the row's left edge.
    Rect IconRect(float x, float y, float height)
    {
        float size = height * IconFraction;
        return new Rect(x, y + (height - size) * 0.5f, size, size);
    }

    // Draws the icon; while the cursor is over it, its name becomes the tooltip.
    void Icon(Rect rect, Sprite sprite, string name)
    {
        DrawSprite(rect, sprite, Color.white);
        if (!string.IsNullOrEmpty(name) && rect.Contains(Event.current.mousePosition)) _tooltip = name;
    }

    // The name of the icon under the cursor, in a small box above and right of the cursor, kept on screen.
    void DrawTooltip()
    {
        if (string.IsNullOrEmpty(_tooltip)) return;
        float s = _scale;
        _content.text = _tooltip;
        Vector2 size = _tooltipStyle.CalcSize(_content);
        float pad = TooltipPadding * s;
        float offset = TooltipOffset * s;
        Vector2 mouse = Event.current.mousePosition;
        var rect = new Rect(0f, 0f, size.x + 2f * pad, size.y + 2f * pad);
        rect.x = Mathf.Clamp(mouse.x + offset, 0f, Mathf.Max(0f, Screen.width - rect.width));
        rect.y = mouse.y - rect.height - 0.5f * offset;
        if (rect.y < 0f) rect.y = mouse.y + offset;
        Fill(rect, TooltipColor);
        Outline(rect, TooltipBorderColor, Mathf.Max(1f, s));
        GUI.Label(new Rect(rect.x + pad, rect.y + pad, size.x, size.y), _content, _tooltipStyle);
    }

    void DrawValue(Rect rect, string text, GUIStyle style, bool warn)
    {
        Color previous = GUI.contentColor;
        if (warn) GUI.contentColor = LowColor;
        GUI.Label(rect, text, style);
        GUI.contentColor = previous;
    }

    static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    static void Outline(Rect rect, Color color, float width)
    {
        Fill(new Rect(rect.x, rect.y, rect.width, width), color);
        Fill(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
        Fill(new Rect(rect.x, rect.y, width, rect.height), color);
        Fill(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
    }

    // Draws the sprite as large as fits in the area, keeping its aspect ratio.
    static void DrawSprite(Rect area, Sprite sprite, Color tint)
    {
        Texture2D texture = sprite.texture;
        if (texture == null) return;
        Rect pixels;
        try { pixels = sprite.textureRect; }      // throws for tightly packed atlas sprites
        catch { pixels = sprite.rect; }
        var uv = new Rect(pixels.x / texture.width, pixels.y / texture.height,
            pixels.width / texture.width, pixels.height / texture.height);

        float aspect = pixels.width / Mathf.Max(1f, pixels.height);
        float width = area.width;
        float height = width / aspect;
        if (height > area.height)
        {
            height = area.height;
            width = height * aspect;
        }
        var fit = new Rect(area.x + (area.width - width) * 0.5f, area.y + (area.height - height) * 0.5f, width, height);

        Color previous = GUI.color;
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(fit, texture, uv, true);
        GUI.color = previous;
    }

    static Rect Shrink(Rect rect, float fraction)
    {
        float width = rect.width * fraction;
        float height = rect.height * fraction;
        return new Rect(rect.center.x - width * 0.5f, rect.center.y - height * 0.5f, width, height);
    }

    static Rect Union(Rect a, Rect b) =>
        Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
}
