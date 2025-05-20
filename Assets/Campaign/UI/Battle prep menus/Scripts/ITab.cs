using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BattlePrep
{
    public abstract class ITab : MonoBehaviour
    {
        public abstract void Select(Color color, bool selected = false);
    }
}