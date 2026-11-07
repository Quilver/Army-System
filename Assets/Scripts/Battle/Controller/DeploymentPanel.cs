using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The deployment phase's UI: a hint and the Start Battle button, shown only while the battle is deploying. The button
// ends deployment (Battle.StartBattle). Lives on the battle canvas, so it finds the battle at runtime.
// The hint spans the canvas width with a margin and wraps or shrinks to fit (it was a fixed 1100 wide on the battle
// canvas's 960-wide reference resolution, so both ends of the sentence were cut off).
public sealed class DeploymentPanel : MonoBehaviour
{
    const string HintName = "Hint";
    const float HintMargin = 0.04f;      // of the canvas width, each side
    const float HintMinFontSize = 14f;

    [SerializeField, Tooltip("Shown while deploying, hidden otherwise.")] GameObject _content;
    [SerializeField] Button _startButton;

    Battle _battle;

    void Start()
    {
        FitHint();
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
        hint.textWrappingMode = TextWrappingModes.Normal;
        hint.fontSizeMax = hint.fontSize;
        hint.fontSizeMin = Mathf.Min(HintMinFontSize, hint.fontSize);
        hint.enableAutoSizing = true;
    }

    void OnPhaseChanged(BattlePhase phase)
    {
        if (_content != null) _content.SetActive(phase == BattlePhase.Deployment);
    }
}
