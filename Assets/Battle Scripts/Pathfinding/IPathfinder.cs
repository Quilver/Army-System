using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public interface IPathfinder
    {
        public Vector2 GoTowards(Vector2 destination);
        public Vector2 GoTowards(Transform target);
    }
}