using InfluenceMap;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Pathfinding
{
    public class Waypoint
    {
        Vector2 _targetTile;
        readonly UnitR _targetUnit;
        public Waypoint(Vector2 tile)
        {
            _targetTile = tile;
        }
        public Waypoint(UnitR target)
        {
            _targetUnit = target;
        }
        public Stack<PositionR> GetPath(UnitPositionR unit)
        {

            if (_targetUnit != null)
            {
                var waypoints = Battle.Instance.highLevelMap.A_StarSearch(unit.position.Location, _targetUnit.LeadModelPosition);
                if (waypoints != null && waypoints.Count > 1)
                {
                    var waypoint = waypoints[waypoints.Count - 2];
                    return Pathfinder.Search(unit, unit.position, waypoint);
                }
                return Pathfinder.Search(unit, unit.position, _targetUnit);
            }
            else
            {
                var waypoints = Battle.Instance.highLevelMap.A_StarSearch(unit.position.Location, _targetTile);
                if (waypoints!=null && waypoints.Count > 1)
                {
                    var waypoint = waypoints[waypoints.Count - 2];
                    return Pathfinder.Search(unit, unit.position, waypoint);
                }
                return Pathfinder.Search(unit, unit.position, _targetTile);
            }
        }
        public static bool operator ==(Waypoint a, Waypoint b)
        {
            if (a is null && b is null) return true;
            else if (a is null || b is null) return false;
            if (a._targetUnit != null || a._targetUnit == b._targetUnit) return true;
            else if (a._targetTile == b._targetTile) return true;
            else return false;
        }
        public static bool operator !=(Waypoint a, Waypoint b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
    public struct WeightedNode<State>
    {
        public State state;
        public float weight;
    };
    public class MinHeap<T>
    {
        public class N
        {
            public float score;
            public T data;
        }
        public int size;
        public N[] mH;
        //bool working = true;
        public MinHeap(int size)
        {
            this.size = 0;
            mH = new N[size + 1];
        }
        //adds a node to the heap
        public void Insert(T info, float x)
        {
            N n = new N
            {
                data = info,
                score = x
            };
            //Debug.Log("inserting item to heap");
            mH[size] = n;
            BubbleUp(size);
            size++;
        }
        //
        void BubbleUp(int index)
        {
            while ((index > 0) && (mH[Parent(index)].score > mH[index].score))
            {
                int original_parent_pos = Parent(index);
                Swap(index, original_parent_pos);
                index = original_parent_pos;
            }
        }
        //gets node with the lowest value
        public T ExtractMin()
        {
            N min = mH[0];
            //Debug.Log("getting first item from heap");
            mH[0] = mH[size - 1];
            mH[size - 1] = null;
            size--;
            SinkDown(0);
            return min.data;
        }
        //gets parent index node of the current node
        static int Parent(int position)
        {
            return (position - 1) / 2;
        }
        //
        void SinkDown(int k)
        {
            int test;
            if (2 * k + 2 < size)
            {
                if (mH[2 * k + 1].score < mH[2 * k + 2].score) { test = 2 * k + 1; }
                else { test = 2 * k + 2; }
            }
            else if (2 * k + 1 < size)
            {
                test = 2 * k + 1;
            }
            else
            {
                return;
            }
            if (mH[k].score > mH[test].score)
            {
                Swap(k, test);
                SinkDown(test);
            }

        }
        //swaps the positition of 2 nodes in the array
        void Swap(int a, int b)
        {
            N temp = mH[a];
            mH[a] = mH[b];
            mH[b] = temp;
        }
        public void DisplayScores()
        {
            string nodes = "";

            for (int i = 0; i < size && i < 20; i++)
            {
                nodes += mH[i].score;
                nodes += " ";
            }
            Debug.Log(nodes);
        }
        bool Valid()
        {
            for (int i = 0; i < size; i++)
            {
                if (mH[0].score > mH[i].score)
                {
                    return false;
                }
            }
            return true;
        }
    }
}