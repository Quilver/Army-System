using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The deployment phase's UI: a hint and the Start Battle button, shown only while the battle is deploying. The button
// ends deployment (Battle.StartBattle). Lives on the battle canvas, so it finds the battle at runtime.
// The hint spans the canvas width with a margin and shrinks to fit on one line (it was a fixed 1100 wide on the battle
// canvas's 960-wide reference resolution, so both ends of the sentence were cut off; it then wrapped before it shrank,
// leaving its last word alone on a second line).
// The Start Battle button is made smaller and kept at the bottom-centre, between the troop cards in the bottom corners,
// its label shrinking to fit: at 280 x 72 it covered the deployment zone, which the camera also used to show low on
// the screen (DeploymentZone now frames the camera on the zone).
public sealed class DeploymentPanel : MonoBehaviour
{
    const string HintName = "Hint";
    const float HintMargin = 0.04f;      // of the canvas width, each side
    const float HintMinFontSize = 14f;
    const float ButtonMargin = 16f;      // reference-resolution units from the bottom edge of the canvas
    const float ButtonWidth = 180f;      // reference-resolution units (was 280 x 72)
    const float ButtonHeight = 48f;
    const float ButtonLabelMinFontSize = 14f;
    static readonly Vector2 BottomCentre = new(0.5f, 0f);

    [SerializeField, Tooltip("Shown while deploying, hidden otherwise.")] GameObject _content;
    [SerializeField] Button _startButton;

    Battle _battle;

    void Start()
    {
        FitHint();
        FitStartButton();
        _battle = Battle.Instance;
        if (_startButton != null) _startButton.onClick.AddListener(StartBattle);
        if (_battle != null) _battle.PhaseChanged += OnPhaseChanged;
        OnPhaseChanged(_battle != null ? _battle.Phase : BattlePhase.Fighting);
    }

    void OnDestroy()
    {
        if (_battle != null) _battle.PhaseChanged -= OnPhaseChanged;
        if (_startButton != null) _startButton.onClick.RemoveListener(StartBattle);
    }

    public void StartBattle()
    {
        if (_battle != null) _battle.StartBattle();
    }

    void FitHint()
    {
        Transform hintTransform = _content != null ? _content.transform.Find(HintName) : null;
        TMP_Text hint = hintTransform != null ? hintTransform.GetComponent<TMP_Text>() : null;
        if (hint == null) return;
        RectTransform rect = hint.rectTransform;
        rect.anchorMin = new Vector2(HintMargin, rect.anchorMin.y);
        rect.anchorMax = new Vector2(1f - HintMargin, rect.anchorMax.y);
        rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);
        rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
        hint.textWrappingMode = TextWrappingModes.NoWrap;
        hint.fontSizeMax = hint.fontSize;
        hint.fontSizeMin = Mathf.Min(HintMinFontSize, hint.fontSize);
        hint.enableAutoSizing = true;
    }

    // Bottom-centre, ButtonMargin up from the bottom edge, ButtonWidth x ButtonHeight; the label stays on one line and
    // shrinks (down to ButtonLabelMinFontSize) to fit the smaller button. Not a bottom corner: the troop cards are drawn
    // there and the hover card would hide the button.
    void FitStartButton()
    {
        if (_startButton == null) return;
        var rect = (RectTransform)_startButton.transform;
        rect.anchorMin = BottomCentre;
        rect.anchorMax = BottomCentre;
        rect.pivot = BottomCentre;
        rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
        rect.anchoredPosition = new Vector2(0f, ButtonMargin);

        TMP_Text label = _startButton.GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.fontSizeMax = label.fontSize;
        label.fontSizeMin = Mathf.Min(ButtonLabelMinFontSize, label.fontSize);
        label.enableAutoSizing = true;
    }

    void OnPhaseChanged(BattlePhase phase)
    {
        if (_content != null) _content.SetActive(phase == BattlePhase.Deployment);
    }
}
