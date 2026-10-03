using System.Collections.Generic;
using UnityEngine;

namespace ArmySystem.AI.V2
{
    public interface IForceGroupingService
    {
        ForceGrouping GroupByThreshold(
            IReadOnlyList<UnitSnapshot> units,
            IEdgeWeight edgeWeight,
            float threshold,
            IWorldView world);

        ForceGrouping GroupByCount(
            IReadOnlyList<UnitSnapshot> units,
            IEdgeWeight edgeWeight,
            int groupCount,
            IWorldView world);
    }

    public interface IEdgeWeight
    {
        float Calculate(UnitSnapshot first, UnitSnapshot second, IWorldView world);
    }

    public interface IForceGroupAnalyzer
    {
        void Analyze(ForceGroup group, Vector2 observerPosition, IWorldView world);
    }

    public sealed class ForceGrouping
    {
        public IReadOnlyList<ForceGroup> Groups { get; set; }
        public IReadOnlyList<GapEdge> GapEdges { get; set; }
    }

    public sealed class ForceGroup
    {
        public IReadOnlyList<UnitSnapshot> Units { get; set; }
        public Vector2 WeightedCenter { get; set; }
        public Vector2 Facing { get; set; }
        public FlankPair Flanks { get; set; }
        public float Strength { get; set; }
        public float RangedStrength { get; set; }
        public float Frontage { get; set; }
    }

    public readonly struct Flank
    {
        public Flank(UnitSnapshot unit, Vector2 position)
        {
            Unit = unit;
            Position = position;
        }

        public UnitSnapshot Unit { get; }
        public Vector2 Position { get; }
    }

    public readonly struct FlankPair
    {
        public FlankPair(Flank left, Flank right)
        {
            Left = left;
            Right = right;
        }

        public Flank Left { get; }
        public Flank Right { get; }
    }

    public readonly struct GapEdge
    {
        public GapEdge(
            UnitSnapshot first,
            UnitSnapshot second,
            float weight,
            float defendedInfluence)
        {
            First = first;
            Second = second;
            Weight = weight;
            DefendedInfluence = defendedInfluence;
        }

        public UnitSnapshot First { get; }
        public UnitSnapshot Second { get; }
        public float Weight { get; }
        public float DefendedInfluence { get; }
        public Vector2 Midpoint => (First.Position + Second.Position) * 0.5f;
    }
}
