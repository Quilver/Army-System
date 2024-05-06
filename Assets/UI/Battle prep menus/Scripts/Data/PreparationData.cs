using EndGameUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BattlePrep
{
    [CreateAssetMenu(menuName = "Preparations/data")]
    public class PreparationData : ScriptableObject
    {
        [SerializeField, TextArea(5, 15)]
        string briefing;
        [SerializeField]
        List<UnitCard> units;
        [SerializeField]
        List<BuyOrders> buyOrders;

    }
}