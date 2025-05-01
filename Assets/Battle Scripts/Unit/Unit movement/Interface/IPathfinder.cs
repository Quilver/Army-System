using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    public interface IPathfinder
    {
        public List<Vector2> GetPath(Vector2 position);
    }
}