using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SoftBody
{
    class UnitCreator : MonoBehaviour
    {
        #region Setup Values
        [SerializeField]
        GameObject ModelPrefab;
        [SerializeField, Range(1, 4)]
        float ModelSize;
        [SerializeField, Range(1, 32)]
        int modelCount, Width;

        #endregion

        #region Unit Details
        public int Files
        {
            get
            {
                if (models == null || models.Count >= Width || models.Count == 0) return Width;
                return models.Count;
            }
        }
        public int Ranks
        {
            get
            {
                return (modelCount % Files > 0) ? modelCount / Files + 1 : modelCount / Files;
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

        #endregion
        #region Orders
        PathToTarget toTarget;
        PathToPosition toPosition;
        public void MoveTo(Vector2 position)
        {
            toPosition.targetLocation = position;
            toPosition.enabled = true;
            toTarget.enabled=false;
        }
        public void MoveTo(Transform target)
        {
            toTarget.Activate(target.position, target);
            toPosition.enabled = false;
        }

        #endregion

        List<Model> models;
        // Start is called before the first frame update
        void Awake()
        {
            models = new();
            for (int i = 0; i < modelCount; i++) { 
                var model = Instantiate(ModelPrefab);
                model.transform.position = GetModelPos(i);
                Model unitComponent = model.GetComponent<Model>();
                unitComponent.Setup(GetComponentsInChildren<Rigidbody2D>(), transform);
                models.Add(unitComponent);
            }
            unitCollider = GetComponent<BoxCollider2D>();
            toTarget = GetComponentInChildren<PathToTarget>();
            toPosition = GetComponentInChildren<PathToPosition>();
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

