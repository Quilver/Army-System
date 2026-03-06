using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace AISystem
{
    public interface IPerception
    {
        
    }
    public class Blackboard : MonoBehaviour
    {
        Dictionary<Type, IPerception> knowledge;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void UpdatePerception(IPerception perception)
        {
            if (!knowledge.ContainsKey(perception.GetType()))
                knowledge.Add(perception.GetType(), perception);
            else knowledge[perception.GetType()] = perception;
        }
        public T GetPerception<T>() where T : IPerception
        {
            return knowledge.TryGetValue(typeof(T), out var val) ? (T)val : default;
        }
    }
    public interface IUnit {
        IBehaviour lastBehaviour { get; set; }
    }
    public interface IFormation { }
    public interface IBehaviour
    {
        void Move(IUnit unit, IFormation formation);

    }
    public abstract class Enemies : IPerception{
        public abstract List<IUnit> GetEnemies(IUnit unit, float range);
        public abstract IUnit SelectTarget(List<IUnit> enemies);
    }
    public class Health : IPerception{ 
        public bool Vulnerable(IUnit unit) => false; 
        public bool Healthy(IUnit unit) => true; 
    }
    public class MoveToo: IPerception{
        public Vector2? Target(IUnit unit) => null;
    }
    
    
    class BasicUnitAI
    {
        void UpdateAI(IUnit unit, IFormation formation, Blackboard blackboard)
        {
            IBehaviour behaviour = unit.lastBehaviour;
            Health health = blackboard.GetPerception<Health>();
            Enemies enemies = blackboard.GetPerception<Enemies>();
            MoveToo moveToo = blackboard.GetPerception<MoveToo>();
            if (health == null || enemies == null || moveToo == null)
                Debug.LogError("Unitialised blackboard");
            //retreat
            if (health.Vulnerable(unit)) { }
            else if (!health.Healthy(unit)) { }
            //Attack
            else if (enemies.GetEnemies(unit, 4).Count > 0) { }
            //Move to location
            else if (moveToo.Target(unit) != null) { }
            //Idle
            else { }

        }
    }
}