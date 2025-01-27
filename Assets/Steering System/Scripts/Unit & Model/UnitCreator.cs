using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SoftBody
{
    public class UnitCreator : MonoBehaviour
    {
        [SerializeField]
        GameObject ModelPrefab;
        [SerializeField, Range(1, 4)]
        float ModelSize;
        [SerializeField, Range(1, 32)]
        int modelCount, Width;
        public int Ranks
        {
            get {
                return (modelCount % Width > 0) ? modelCount / Width + 1 : modelCount / Width; 
            }
        }
        Vector2 unitOffset
        {
            get
            {
                return new Vector2(transform.position.x, transform.position.y - (Ranks - 1) * ModelSize / 4);
            }
        }
        Vector2 UnitSize
        {
            get
            {
                return new Vector2(Width, Ranks) * ModelSize / 2;
            }
        }
        // Start is called before the first frame update
        void Awake()
        {
            for (int i = 0; i < modelCount; i++) { 
                var model = Instantiate(ModelPrefab);
                model.transform.position = GetModelPos(i);
                model.GetComponent<Model>().Setup(GetComponentsInChildren<Rigidbody2D>(), transform);
            }
            unitCollider = GetComponent<BoxCollider2D>();
            //Destroy(this);
            DrawGizmo = false;
        }
        BoxCollider2D unitCollider;
        // Update is called once per frame
        void Update()
        {
            unitCollider.offset = unitOffset-(Vector2)transform.position;
            unitCollider.size = UnitSize;
        }
        Vector2 GetModelPos(int modelIndex)
        {
            float x = modelIndex % Width;
            if (x % 2 == 0) x = -x / 2;
            else x = x / 2 + 0.5f;
            if (Width % 2 == 0) x -= 0.5f;
            //x -= (Width%2 ==0) ? Width/2 - 0.5f : Width/2;

            int y = (modelIndex / Width);
            Vector3 offset = new Vector3(x, -y) * ModelSize / 2;
            offset = Quaternion.AngleAxis(transform.eulerAngles.z, Vector3.forward) * offset;
            return transform.position + offset;
        }
        [SerializeField]
        bool DrawGizmo;
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 0, 1, 0.7f);
            if (!DrawGizmo) return;
            for (int i = 0; i < modelCount; i++) Gizmos.DrawSphere(GetModelPos(i), ModelSize / 5);
            DrawUnitBox();
        }
        void DrawUnitBox()
        {
            Gizmos.color = new Color(0, 0, 1, 0.3f);
            Gizmos.DrawCube(unitOffset, UnitSize);
        }
    }
}

