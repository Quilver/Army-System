using System.Collections.Generic;
using UnityEngine;

// One side of a battle: the troops under this object. Gives its troops their colour and names the opposing army.
public class Army : MonoBehaviour
{
    public Color ArmyColour = Color.white;
    [SerializeField] Army _enemy;

    readonly List<Troop> _troops = new();

    public Army Enemy => _enemy;

    // Every troop under this army, including routed troops that have not left the field yet.
    public IReadOnlyList<Troop> Troops
    {
        get
        {
            _troops.Clear();
            GetComponentsInChildren(false, _troops);
            return _troops;
        }
    }

    // Troops still in the battle: not routed and not wiped out.
    public int ActiveTroopCount
    {
        get
        {
            int count = 0;
            foreach (var troop in Troops)
                if (!troop.IsDefeated) count++;
            return count;
        }
    }
}
