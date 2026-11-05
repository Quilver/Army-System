using System.Collections.Generic;
using UnityEngine;

namespace BattleEnd
{
    // Ends the battle when enough of the army's troops are out of the battle (routed or wiped out): every troop by
    // default, or more than Rout Share of them (0.5 = over half, as in a rout mission).
    public class Rout : MonoBehaviour
    {
        [SerializeField] bool WinCondition;
        [SerializeField] Army _army;

        [SerializeField, Range(0f, 1f), Tooltip("The battle ends when more than this share of the army's troops are out " +
                                               "(routed or wiped out). 0.5 = over half. 1 = every troop.")]
        float _routShare = 1f;

        // Every troop that has been in the army, so troops that rout and leave the field still count towards the total.
        readonly HashSet<Troop> _seen = new();

        public float RoutShare => _routShare;

        void Start()
        {
            if (_army == null) Debug.LogError($"{gameObject.name} has not been assigned army for rout exit");
            else Remember();
            BattleEvents.TroopDefeated += CheckTroopsRemaining;
        }

        void OnDestroy() => BattleEvents.TroopDefeated -= CheckTroopsRemaining;

        void Remember()
        {
            foreach (var troop in _army.Troops) _seen.Add(troop);
        }

        void CheckTroopsRemaining(Troop defeated)
        {
            if (_army == null || Battle.Instance == null) return;
            Remember();

            int total = _seen.Count;
            int outOfBattle = 0;
            foreach (var troop in _seen)
                if (troop == null || troop.IsDefeated) outOfBattle++;

            bool everyTroop = outOfBattle >= total;
            bool enough = _routShare < 1f && outOfBattle > total * _routShare;
            if (!everyTroop && !enough) return;

            BattleEvents.TroopDefeated -= CheckTroopsRemaining;
            Battle.Instance.EndBattle(WinCondition);
        }
    }
}
