using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem
{
    public abstract class IPossibleMoves : MonoBehaviour
    {
        public abstract List<UnitOrder> GetMoves(IUnit unit);
    }
    
}