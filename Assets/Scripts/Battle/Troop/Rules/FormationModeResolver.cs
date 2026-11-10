// Chooses a troop's formation state from its situation (spec §3.1). Pure C#. Priority: Rout > Chain > Line > Column > Idle.
// A chained troop that is moving stays in Chain (it marches in line shape), which is intended.
public static class FormationModeResolver
{
    public static FormationMode Resolve(bool routed, bool linked, bool enemyWithinSDistance, bool holdingLineOrder,
        bool marching)
    {
        if (routed) return FormationMode.Rout;
        bool inLine = enemyWithinSDistance || holdingLineOrder;
        if (inLine && linked) return FormationMode.Chain;
        if (inLine) return FormationMode.Line;
        if (marching) return FormationMode.Column;
        return FormationMode.Idle;
    }
}
