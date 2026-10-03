using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Cutscene
{
    [CreateAssetMenu(menuName ="Cutscenes/Dialogue")]
    public class Dialogue : ScriptableObject
    {
        public bool leftOrRight;
        public Sprite Portrait;
        public string CharacterName;
        [TextArea(2, 4)]
        public string dialogue;
    }
}