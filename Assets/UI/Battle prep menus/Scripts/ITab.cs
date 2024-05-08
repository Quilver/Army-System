using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BattlePrep
{
    public interface ITab
    {
        public void Select(Color color, bool selected = false);
    }
}