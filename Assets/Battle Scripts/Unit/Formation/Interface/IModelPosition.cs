using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Formation
{
    public interface IModelPosition 
    {
        Vector3 GetModelPosition(int index);
        Vector3 GetModelOffsetPosition(int index);
        void RemoveModel(Transform model);
    }
}