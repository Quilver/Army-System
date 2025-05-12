using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem
{
    public abstract class ISelectUnits : MonoBehaviour
    {
        public abstract List<IUnit> GetUnitsToOrder { get; }
    }
}
