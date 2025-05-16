using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MoraleSystem
{
    public class Morale : MonoBehaviour
    {
        Formation.IFormationData _formationData;
        IUnit _unit;
        [SerializeField, Range(3, 10)]
        float FleeTime;
        // Start is called before the first frame update
        void Start()
        {
            _formationData = GetComponentInChildren<Formation.IFormationData>();
            _unit = GetComponent<IUnit>();
            enabled = false;
        }
        public void TakeDamage()
        {
            //below third, shatter
            if (_formationData.ModelCount <= _unit.Stats.ModelCount / 3)
            {
                _unit.KillUnit();
                //Destroy(gameObject);
            }
            //below half, flee
            else if (_formationData.ModelCount <= _unit.Stats.ModelCount / 2)
            {
                EnterFlee();
            }

        }
        int BreakPoint()
        {
            float percentage = Mathf.Lerp(1, 0.4f, _unit.Stats.Leadership / 20f);
            return Mathf.RoundToInt(percentage * _unit.Stats.ModelCount);
            
        }
        void EnterFlee()
        {
            _unit.State = UnitState.Fleeing;
            enabled = true;
            _timeLeft = FleeTime;
        }
        void ExitFlee()
        {
            enabled = false;
            _unit.State = UnitState.Idle;
        }
        float _timeLeft;
        private void Update()
        {
            _timeLeft -= Time.deltaTime;
            if (_timeLeft < 0)
            {
                ExitFlee();
            }
        }
    }
}