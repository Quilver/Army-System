using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace Formation
{
    class DeploymentSpawner : MonoBehaviour, ISpawnModels
    {
        IFormationData _formationData;
        IModelPosition _position;
        StatSystem.Refactor.IUnitStatBlock _stats;
        IShape _shape;
        List<GameObject> _models;
        [SerializeField]
        UnityEvent Deployed;
        public List<GameObject> Models => _models;

        void Start()
        {
            _formationData = GetComponent<IFormationData>();
            _position = GetComponent<IModelPosition>();
            _stats = GetComponentInParent<IUnit>().Stats;
            _shape = GetComponent<IShape>();
            Battle.Instance.Deploy += SpawnUnit;
            CallValidSpawn();
            GetComponentInParent<MovementSystem.IMoveOrders>().finishedMovement += CallValidSpawn;
            GetComponent<DisplaySquareFormation>().UpdateColor(spawn);
        }
        [SerializeField]
        bool spawn = true;
        Collider2D _collider;
        [SerializeField]
        Collider2D[] collision;
        void CallValidSpawn() {
            bool testSpawn = ValidSpawn();
            if(testSpawn == spawn)return;
            Battle.Instance.UpdateUnitDeployment(testSpawn);
            spawn = testSpawn;
            GetComponent<DisplaySquareFormation>().UpdateColor(spawn);
        } 
        bool ValidSpawn()
        {
            if (_collider == null) _collider = GetComponentInParent<Collider2D>();
            var hit = Physics2D.OverlapBoxAll(transform.position, _shape.SizeOfFormation, transform.parent.rotation.eulerAngles.z, 1 << 12);
            if (hit.Length != 1) return false;
            hit = Physics2D.OverlapBoxAll(transform.position, _shape.SizeOfFormation, transform.parent.rotation.eulerAngles.z, 1 << 6);
            if(hit.Length > 1) return false;
            else if(hit.Length == 1 && hit[0].transform != transform.parent) return false;
            var hit1 = Physics2D.OverlapBox(transform.position, _shape.SizeOfFormation, transform.parent.rotation.eulerAngles.z, 1 << 3);
            var hit2 = Physics2D.OverlapBox(transform.position, _shape.SizeOfFormation, transform.parent.rotation.eulerAngles.z, 1 << 8);
            return !hit1 && !hit2;
        }
        public void SpawnUnit()
        {
            int modelCount = _formationData.ModelCount;
            _models = new(new GameObject[_formationData.ModelCount]); 
            GetComponentInParent<IUnit>().State = UnitState.Idle;
            GetComponent<DisplaySquareFormation>().enabled = false;
            enabled= false;
            Deployed?.Invoke();
            for (int i = 0; i < modelCount; i++)
            {
                GameObject model = Instantiate(_stats.ModelPrefab[0]);
                _models[i]=model;
                model.GetComponent<ModelComponents.IUnitData>().Setup(GetComponentInParent<IUnit>());
                model.transform.position = _position.GetModelPosition(i);
                model.GetComponent<ModelComponents.IModelFormation>().SetUp(GetComponentsInChildren<Rigidbody2D>());
                ModelComponents.ModelContainer.AddModel(model.transform);
            }
        }

    }
}
