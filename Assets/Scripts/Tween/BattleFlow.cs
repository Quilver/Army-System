using Deployment;
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
        BattleEvents.TroopBroke += OnTroopBroke;
    }

    static void OnStarted(Battle battle)
    {
        // Every battle tracks achievements, also a level whose scene has no AchievementController (SU-116).
        if (Object.FindAnyObjectByType<AchievementSystem.AchievementController>() == null)
            new GameObject("Achievement Controller").AddComponent<AchievementSystem.AchievementController>();
        if (!BattleReport.Exists) return;
        BattleReport.ResetBattleStats();
        foreach (var character in BattleReport.DeployedCharacters) BattleReport.RecordFor(character);
    }

    // Kills: enemy soldiers that died. Deaths: player soldiers that died. Each is also put on the record of the deployed
    // character whose troop lost the soldier or dealt the killing blow (SU-115).
    static void OnSoldierKilled(Troop victim, SoldierAgentState soldier, Troop attacker)
    {
        Battle battle = Battle.Instance;
        if (battle == null || battle.IsOver || !BattleReport.Exists || victim == null) return;
        Army army = victim.Army;
        if (army == battle.player)
        {
            BattleReport.deaths++;
            // In a composite troop the casualty is the fallen soldier's own character's (SU-117).
            var record = BattleReport.RecordFor(RosterTroops.CharacterOf(soldier?.Character) ?? RosterTroops.CharacterOf(victim));
            if (record != null) record.Casualties++;
        }
        else if (army == battle.enemy)
        {
            BattleReport.kills++;
            if (attacker != null && attacker.Army == battle.player)
            {
                var record = BattleReport.RecordFor(RosterTroops.CharacterOf(attacker));
                if (record != null) record.Kills++;
            }
        }
    }

    // Routed troops of each side; a routed character is marked on its record.
    static void OnTroopBroke(TroopBreakInfo info)
    {
        Battle battle = Battle.Instance;
        if (info.Kind != TroopBreakKind.Rout || battle == null || battle.IsOver || !BattleReport.Exists || info.Troop == null)
            return;
        if (info.Troop.Army == battle.player)
        {
            BattleReport.PlayerTroopsRouted++;
            // Every character in a routed composite troop routed with it (SU-117).
            foreach (var character in RosterTroops.CharactersOf(info.Troop))
                BattleReport.RecordFor(character).Routed = true;
        }
        else if (info.Troop.Army == battle.enemy) BattleReport.EnemyTroopsRouted++;
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
