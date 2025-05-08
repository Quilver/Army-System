using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SoftBody
{
    public class ModelContainer : MonoBehaviour
    {
        static GameObject instance;
        public static void AddModel(Model model)
        {
            if (instance == null) 
                instance = new GameObject("Model holder");
            model.transform.parent = instance.transform;
        }
        public static void AddModel(Transform model)
        {
            if (instance == null)
                instance = new GameObject("Model holder");
            model.parent = instance.transform;
        }
    }
}

