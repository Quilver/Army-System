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
        public void RemoveModel()
        {
            throw new System.NotImplementedException();
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