using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Formation
{
    class BasicSpawner : MonoBehaviour, ISpawnModels
    {
        IFormationData _formationData;
        IModelPosition _position;
        StatSystem.Refactor.IUnitStatBlock _stats;
        List<GameObject> _models;

        public List<GameObject> Models => _models;

        void Start()
        {
            _formationData=GetComponent<FormationData>();
            _position=GetComponent<IModelPosition>();
            _stats= GetComponentInParent<IUnit>().Stats;
            SpawnUnit();
        }
        public void SpawnUnit()
        {
            _models = new(new GameObject[_formationData.ModelCount]);
            for (int i = 0; i < _formationData.ModelCount; i++)
            {
                GameObject model = Instantiate(_stats.ModelPrefab[0]);
                _models[i] = model;
                model.GetComponent<ModelComponents.IUnitData>().Setup(GetComponentInParent<IUnit>());
                model.transform.position = _position.GetModelPosition(i);
                model.GetComponent<ModelComponents.IModelFormation>().SetUp(transform.parent.GetComponentsInChildren<Rigidbody2D>(), _position.GetModelOffsetPosition(i), transform.parent);
                ModelComponents.ModelContainer.AddModel(model.transform);
            }

        }

    }
}
