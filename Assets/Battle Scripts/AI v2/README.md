# AI v2

AI v2 separates mission authoring, army planning, squad execution, world analysis,
and low-level unit orders.

```text
AI v2/
  Core/
    AIEnums.cs
    UnitCapabilities.cs
  World/
    AIContext.cs
    AISnapshots.cs
  Objectives/
    IAIObjective.cs
  Squads/
    AISquadDefinition.cs
    IAISquad.cs
  Tasks/
    AITriggers.cs
    SquadTasks.cs
  Orders/
    AIOrders.cs
  Planning/
    ArmyPlanning.cs
    ManeuverPlanning.cs
  Analysis/
    ForceAnalysis.cs
    PositionEvaluation.cs
    RoutePlanning.cs
```

## Command ownership

```text
Mission script or IArmyPlanner
             |
             v
       SquadTaskDefinition
             |
             v
          ISquadTask
             |
             v
      IUnitOrderService
             |
             v
 Existing UnitOrder / IMoveOrders
```

Only one layer chooses each kind of decision:

- The army planner chooses objectives, maneuvers, and squad assignments.
- A squad task chooses squad targets, routes, and formation positions.
- Unit-level execution handles movement, combat, and immediate survival.

## Extension model

Unit categories are not represented by a `UnitRole` enum. An `AIUnitProfile`
contains composable `IUnitCapability` objects. Planners use `IUnitRequirement`
objects to score suitability, allowing any future unit to act as a flanker,
ranged support, fixing force, or objective unit when its capabilities permit it.

Squad behaviours are not represented by a `SquadTaskType` enum. Serializable
`SquadTaskDefinition` subclasses create runtime `ISquadTask` instances. New
tasks can therefore be added without modifying a central switch statement.

## Maneuvers

The five maneuvers described by Sidran and Segre belong above squad tasks.
An `IManeuverPlanner` analyzes enemy groups and produces a coordinated
`ManeuverProposal` containing several `SquadAssignment` objects.

- Envelopment assigns fixing and flanking forces.
- Turning movement routes a force around enemy influence to an objective.
- Penetration routes forces through a selected gap or weak point.
- Infiltration searches for low-influence paths between groups.
- Frontal attack targets a strength-weighted enemy center and is a fallback.

Strategic routes should generate waypoints and utility scores. Existing Unity
NavMesh movement remains responsible for travel between those waypoints.

This folder currently defines contracts and data only. Concrete capabilities,
tasks, sensors, planners, and adapters to the existing order system are separate
implementation milestones.
