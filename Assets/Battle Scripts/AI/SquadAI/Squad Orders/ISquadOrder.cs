using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Squads
{
    [RequireComponent(typeof(ISquad), typeof(InfluenceMap))]
    public abstract class ISquadOrder : MonoBehaviour
    {
        public bool RunOnDeploy = true;
        protected ISquad squad;
        protected InfluenceMap map;
        void Start()
        {
            squad = GetComponent<ISquad>();
            map = GetComponent<InfluenceMap>();
            if (RunOnDeploy)
                Battle.Instance.Deploy += () => StartCoroutine(RunOrder());
            else
                StartCoroutine(RunOrder());
        }
        protected abstract IEnumerator RunOrder();
    }
}

