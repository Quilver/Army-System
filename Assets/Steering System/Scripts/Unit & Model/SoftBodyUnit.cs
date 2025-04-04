using StatSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace SoftBody
{
    public class SoftBodyUnit : UnitTemplate
    {
        #region Base Properties
        UnitFormation formation;
        public override int Files => (ModelCount < formation.Width) ? ModelCount : formation.Width;
        public override int ModelCount => formation.models.Count;
        public override float ModelSize => formation.ModelSize;
        [SerializeField]
        RegimentStats _unitStats;
        public override RegimentStats Stats => _unitStats;
        #endregion

        private void Start()
        {
            //
            formation = GetComponent<UnitFormation>();
            //
            GetComponent<Collider2D>().isTrigger=false;
            GetSteeringBehaviours();
            FinishedMoving += Breaks;
            Breaks(null);
            Transition += EnterCombat; Transition += EnterFlee; Transition += EnterMovement;
            FinishedMoving += ExitMovement;
            unitState = UnitState.Deployment;
        }
        private void OnDestroy()
        {
            FinishedMoving -= Breaks;
            Transition -= EnterCombat; Transition -= EnterFlee; Transition -= EnterMovement;
            FinishedMoving -= ExitMovement;
        }
        private void Update()
        {
            if (unitState == UnitState.Fighting)
            {
                foreach (var model in formation.models)
                {
                    if(model.InCombat) return;
                }
                unitState = UnitState.Idle;
            }
        }

        #region Movement StateMachine
        void EnterIdle(UnitState current, UnitState next) { 
            toTarget.enabled = false; toPosition.enabled = false; flee.enabled = false;
            circleRound.enabled = false; separate.enabled = false;
        }
        void ExitIdle(UnitState current, UnitState next) { 
        
        }
        void EnterMovement(UnitState current, UnitState next) {

            if (next != UnitState.Moving) return;
            separate.enabled = true;
            circleRound.enabled=true;   
            Breaks(null);
        }
        void ExitMovement(SteeringBehaviour behaviour) {
            unitState = UnitState.Idle;
        }
        void EnterCombat(UnitState current, UnitState next) {
            if(next != UnitState.Fighting)return;
            toPosition.enabled = false; flee.enabled=false;
            circleRound.enabled = false; separate.enabled = false;
        }
        void ExitCombat(UnitState current, UnitState next) { 

        }
        [SerializeField, Range(3, 15)]
        float FleeTime = 6;
        void EnterFlee(UnitState current, UnitState next) {
            if (next != UnitState.Fleeing) return;
            Invoke("ExitFlee", FleeTime);
            MoveTowards(transform.position, null);
            Breaks(null);
            GetComponentInChildren<PathToPosition>().enabled = false;
            GetComponentInChildren<Flee>().enabled=true;
        }
        void ExitFlee() {
            unitState= UnitState.Idle;
            Breaks(GetComponentInChildren<Flee>());
        }
        #endregion

        #region Steering behaviours
        //Move towards
        PathToTarget toTarget;
        PathToPosition toPosition;
        Flee flee;
        //Avoid collisions
        CircleRound circleRound;
        Separate separate;
        public Action<Vector2, Transform> MoveTowards;
        public Action<SteeringBehaviour> FinishedMoving;
        void GetSteeringBehaviours()
        {
            toPosition= GetComponentInChildren<PathToPosition>();
            toTarget= GetComponentInChildren<PathToTarget>();
            flee= GetComponentInChildren<Flee>();
            circleRound= GetComponentInChildren<CircleRound>();
            separate = GetComponentInChildren<Separate>();
        }
        #endregion

        void Breaks(SteeringBehaviour behaviour)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (behaviour != null) {
                rb.drag = 10;
                rb.angularDrag = 10;
                rb.mass = 10;
                foreach (var model in formation.models) model.Move(false);
            }
            else
            {
                rb.mass = 1;
                rb.drag = 1;
                rb.angularDrag = 2;
                foreach (var model in formation.models) model.Move(true);
            }
        }
        public override void MoveTo(Vector2 position)
        {
            if(unitState == UnitState.Fleeing) return;
            else if(unitState == UnitState.Deployment)
            {
                DeploymentMoveTo(position);
                return;
            }
            
            MoveTowards(position, null);
            toPosition.enabled = true;
            unitState = UnitState.Moving;
        }
        void DeploymentMoveTo(Vector2 position)
        {
            if (!Physics2D.OverlapPoint(position, LayerMask.GetMask("DeploymentZone"))) return;
            Collider2D[] collisions = new Collider2D[10];
            var oldPos = transform.position;
            transform.position = position;

            int numCollisions = GetComponent<Collider2D>().OverlapCollider(new ContactFilter2D(), collisions);
            if (numCollisions > 1) transform.position = oldPos;
        }
        public override void MoveTo(Transform target)
        {
            if (unitState == UnitState.Fleeing || unitState == UnitState.Deployment) return;
            var unitTarget = target.GetComponent<SoftBodyUnit>();

            if (unitTarget != null && !army.EnemyUnits.Contains(unitTarget))
                return;
            MoveTowards(target.position, target);
            toTarget.enabled = true;
            unitState = UnitState.Moving;
        }
        public override string ToString()
        {
            string description = _unitStats.UnitName + ": " + formation.models.Count + "/" + _unitStats.Stats()[0].CurrentStat+ "\n";
            description += "|Speed: " + Stats.MoveSpeed.CurrentStat;
            description += " | Power: " + Stats.AttackPower.CurrentStat;
            description += " | Defence: " + Stats.Defence.CurrentStat+ " |";
            return description;
        }
        public override void TakeDamage(int modelsRemaining)
        {
            if (modelsRemaining <= _unitStats.Stats()[0].CurrentStat / 3)
            {
                Debug.Log(gameObject.name + ": is broken");
                GetComponent<UnitFormation>().DestroyUnit();
                Die();
            }
            else if(modelsRemaining <= _unitStats.Stats()[0].CurrentStat/2)
            {
                unitState = UnitState.Fleeing;
            }
            
        }
        HashSet<SoftBodyUnit> enemies;
        public void StartFight(SoftBodyUnit unitTarget)
        {
            if (unitTarget != null && !army.EnemyUnits.Contains(unitTarget))
                return;
            if(enemies == null) enemies = new HashSet<SoftBodyUnit>();
            enemies.Add(unitTarget);
            this.unitState = UnitState.Fighting;
        }

        
    }
}