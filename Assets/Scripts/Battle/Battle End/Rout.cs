using UnityEngine;

namespace BattleEnd
{
    // Ends the battle when every troop of the army is out of the battle (routed or wiped out).
    public class Rout : MonoBehaviour
    {
        [SerializeField] bool WinCondition;
        [SerializeField] Army _army;

        void Start()
        {
            if (_army == null) Debug.LogError($"{gameObject.name} has not been assigned army for rout exit");
            BattleEvents.TroopDefeated += CheckTroopsRemaining;
        }

        void OnDestroy() => BattleEvents.TroopDefeated -= CheckTroopsRemaining;

        void CheckTroopsRemaining(Troop defeated)
        {
            if (_army == null || Battle.Instance == null) return;
            if (_army.ActiveTroopCount > 0) return;
            BattleEvents.TroopDefeated -= CheckTroopsRemaining;
            Battle.Instance.EndBattle(WinCondition);
        }
    }
}
