using UnityEngine;

// Puts troops straight at a spot instead of ordering them to march there: the deployment phase (DeploymentZone).
public interface ITroopPlacement
{
    // Places the troop at once, facing the given way. holdLine (a right-drag line) makes it hold a line of the given
    // length; otherwise (a right-click) it stands Idle (SU-101). The placement may move the spot (to keep the troop
    // inside an area). Returns false if this troop cannot be placed here.
    bool Place(Troop troop, Vector2 center, Vector2 facing, float length, bool holdLine);
}
