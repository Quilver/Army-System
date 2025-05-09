using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    public class ModelContainer : MonoBehaviour
    {
        static GameObject instance;
        public static void AddModel(Transform model)
        {
            if (instance == null)
                instance = new GameObject("Model holder");
            model.parent = instance.transform;
        }
    }
}
