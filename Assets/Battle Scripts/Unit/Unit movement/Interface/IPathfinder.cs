using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    public interface IPathfinder
    {
        public List<Vector2> GetPath(Vector2 position);
    }
}