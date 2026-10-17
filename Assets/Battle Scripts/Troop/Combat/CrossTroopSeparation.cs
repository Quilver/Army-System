using UnityEngine;

// Pushes soldiers of different troops (and allied threat-facing lines) apart through each soldier's external separation.
public static class CrossTroopSeparation
{
    const float CellSize = TroopConstants.NeighborRadius;

    public static void Apply(SoldierContactIndex index)
    {
        var contacts = index.Contacts;
        for (int i = 0; i < contacts.Count; i++)
            contacts[i].Soldier.ClearExternalSeparation();

        for (int i = 0; i < contacts.Count; i++)
        {
            SoldierContact a = contacts[i];
            Vector2Int cell = SoldierContactIndex.CellFor(a.Position);
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                if (!index.TryGetBucket(cell + new Vector2Int(x, y), out var bucket)) continue;
                foreach (int j in bucket)
                {
                    if (j <= i) continue;
                    SeparatePair(a, contacts[j]);
                }
            }
        }
    }

    static void SeparatePair(SoldierContact a, SoldierContact b)
    {
        if (a.Troop == b.Troop) return;
        if (a.Troop.TeamId == b.Troop.TeamId
            && !a.Troop.HasThreatFacingFormation
            && !b.Troop.HasThreatFacingFormation)
            return;
        Vector2 difference = a.Position - b.Position;
        float distance = difference.magnitude;
        if (distance <= 0.1f || distance >= CellSize) return;

        float falloff = (CellSize - distance) / CellSize;
        float aScale = a.Soldier.Profile != null ? a.Soldier.Profile.Separation / 1.8f : 1f;
        float bScale = b.Soldier.Profile != null ? b.Soldier.Profile.Separation / 1.8f : 1f;
        float teamScale = a.Troop.TeamId == b.Troop.TeamId ? 0.94f : 0.78f;
        Vector2 direction = difference / distance;
        a.Soldier.AddExternalSeparation((Vector2)a.Troop.transform.InverseTransformVector(direction * falloff * teamScale * aScale));
        b.Soldier.AddExternalSeparation((Vector2)b.Troop.transform.InverseTransformVector(-direction * falloff * teamScale * bScale));
    }
}