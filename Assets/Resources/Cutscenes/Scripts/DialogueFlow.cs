using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Cutscene
{
    [CreateAssetMenu(menuName = "Cutscenes/Dialogue flow")]
    public class DialogueFlow : ScriptableObject
    {
        [SerializeField]
        List<Dialogue> dialogue;
        int sceneIndex = -1;
        public void Start()
        {
            sceneIndex= -1;
        }
        public Dialogue GetNextScene()
        {
            sceneIndex++;
            if(sceneIndex >= dialogue.Count) { return null; }
            return dialogue[sceneIndex];
        }
    }
}