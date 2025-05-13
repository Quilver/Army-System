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
            _models = new();
            _stats= GetComponentInParent<IUnit>().Stats;
            SpawnUnit();
        }
        public void SpawnUnit()
        {
            for (int i = 0; i < _formationData.ModelCount; i++)
            {
                GameObject model = Instantiate(_stats.ModelPrefab[0]);
                _models.Add(model);
                model.GetComponent<ModelComponents.IUnitData>().Setup(GetComponentInParent<IUnit>());
                model.transform.position = _position.GetModelPosition(i);
                model.GetComponent<ModelComponents.IModelFormation>().SetUp(GetComponentsInChildren<Rigidbody2D>());
                ModelComponents.ModelContainer.AddModel(model.transform);
            }

        }

    }
}
