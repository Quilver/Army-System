using Campaign;
using UnityEngine;
using UnityEngine.SceneManagement;

// Bridge from a battle to the campaign (Tween system). While a battle runs it fills the battle report (time, kills,
// deaths); when the battle ends it loads the next scene: victory goes to Post battle (build index 2), defeat discards
// the campaign data and returns to the Main menu (index 0). Keep the indices in step with Build Settings.
static class BattleFlow
{
    const int VictorySceneIndex = 2;
    const int DefeatSceneIndex = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Subscribe()
    {
        Battle.Started += OnStarted;
        Battle.Ended += OnEnded;
        BattleEvents.SoldierKilled += OnSoldierKilled;
    }

    static void OnStarted(Battle battle)
    {
        if (!BattleReport.Exists) return;
        BattleReport.timeTaken = 0f;
        BattleReport.kills = 0;
        BattleReport.deaths = 0;
    }

    // Kills: enemy soldiers that died. Deaths: player soldiers that died.
    static void OnSoldierKilled(Troop victim, SoldierAgentState soldier, Troop attacker)
    {
        Battle battle = Battle.Instance;
        if (battle == null || battle.IsOver || !BattleReport.Exists || victim == null) return;
        Army army = victim.Army;
        if (army == battle.player) BattleReport.deaths++;
        else if (army == battle.enemy) BattleReport.kills++;
    }

    static void OnEnded(Battle battle, bool win)
    {
        if (BattleReport.Exists) BattleReport.timeTaken = battle.ElapsedTime;
        if (win)
        {
            SceneManager.LoadScene(VictorySceneIndex);
            return;
        }
        if (CampaignDataManager.instance != null) Object.Destroy(CampaignDataManager.instance.gameObject);
        SceneManager.LoadScene(DefeatSceneIndex);
    }
}
