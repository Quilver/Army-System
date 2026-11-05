using UnityEngine;

// Puts troops straight at a spot instead of ordering them to march there: the deployment phase (DeploymentZone).
public interface ITroopPlacement
{
    // Places the troop at once, holding a line of the given length facing the given way. The placement may move the
    // spot (to keep the troop inside an area). Returns false if this troop cannot be placed here.
    bool Place(Troop troop, Vector2 center, Vector2 facing, float length);
}
