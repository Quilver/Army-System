using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Formation
{
    class SquareModelPosition : MonoBehaviour, IModelPosition
    {
        IFormationData _data;
        public Vector3 GetModelPosition(int index)
        {
            if(_data == null) _data = GetComponent<IFormationData>();
            float x = index % _data.Width;
            if (x % 2 == 0) x = -x / 2;
            else x = x / 2 + 0.5f;
            if (_data.Width % 2 == 0) x -= 0.5f;


            int y = (index / _data.Width);
            Vector3 offset = new Vector3(x, -y) * _data.ModelSize / 2;
            offset = Quaternion.AngleAxis(transform.eulerAngles.z, Vector3.forward) * offset;
            return transform.position + offset;
        }
        void Start()=>DrawGizmo=false;
        public void RemoveModel(Transform model)
        {
            if(_data == null || !_data.Models.Contains(model.gameObject)) return;
            int index = _data.Models.IndexOf(model.gameObject);
            RemoveModelAtIndex(index);            
        }
        void RemoveModelAtIndex(int index)
        {
            var models = _data.Models;
            var Width = _data.Width;
            //shift up
            if (index + Width < models.Count)
            {
                models[index] = models[index + Width];
                models[index].GetComponent<ModelComponents.IModelFormation>().SetPosition(GetModelPosition(index));
                RemoveModelAtIndex(index + Width);
            }
            //shift right
            else if (index + 2 < models.Count)
            {
                models[index] = models[index + 2];
                models[index].GetComponent<ModelComponents.IModelFormation>().SetPosition(GetModelPosition(index));
                RemoveModelAtIndex(index + 2);
            }
            //done
            else models.RemoveAt(index);
        }
        [SerializeField]
        bool DrawGizmo;
        private void OnDrawGizmos()
        {
            if(!DrawGizmo) return;
            if (_data == null) _data = GetComponent<IFormationData>();
            for (int i = 0; i < _data.ModelCount; i++)
            {
                Gizmos.DrawSphere(GetModelPosition(i), _data.ModelSize/5);
            }
        }
    }
}