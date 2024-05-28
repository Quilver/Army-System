using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace InfluenceMap
{
    public class Node
    {
        public readonly Vector2 nodeSize;
        public readonly Vector2 position;
        public List<Node> adjacentNodes;
        public Node(float size, Vector2 position)
        {
            //size *= 0.75f;
            this.nodeSize = new(size, size);
            this.position = position;
            adjacentNodes= new List<Node>();
        }
        bool occupied = false;
        public void UpdateNode()
        {
            if(Physics2D.OverlapBox(position, nodeSize, 0))
                occupied= true;
            else
                occupied= false;
            //occupied= true;
        }
        public void DrawGridGizmo(bool highlight = false, bool drawPaths = false)
        {
            Gizmos.color = Color.white;
            if(occupied)
                Gizmos.color= Color.red;
            else if (highlight)
                Gizmos.color = Color.yellow;
            else
                Gizmos.color = Color.white;
            if (drawPaths)
                foreach (var node in adjacentNodes)
                    Gizmos.DrawLine(position, node.position);
            Gizmos.DrawSphere(position, 0.3f);
            Gizmos.DrawWireCube(position, nodeSize);
            //
        }
    }
}