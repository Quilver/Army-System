using UnityEngine;
using UnityEngine.UI;

// The deployment phase's UI: a hint and the Start Battle button, shown only while the battle is deploying. The button
// ends deployment (Battle.StartBattle). Lives on the battle canvas, so it finds the battle at runtime.
public sealed class DeploymentPanel : MonoBehaviour
{
    [SerializeField, Tooltip("Shown while deploying, hidden otherwise.")] GameObject _content;
    [SerializeField] Button _startButton;

    Battle _battle;

    void Start()
    {
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

    void OnPhaseChanged(BattlePhase phase)
    {
        if (_content != null) _content.SetActive(phase == BattlePhase.Deployment);
    }
}
