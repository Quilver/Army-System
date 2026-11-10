using System;

// The frontage a troop is given from its total soldier count when it is made for a battle (SU-117). Pure C#.
//   up to 4 soldiers:  frontage = soldiers
//   up to 10:          half, rounded up
//   up to 16:          a third, rounded up
//   more than 16:      a quarter, rounded up
// Never below 1.
public static class TroopFrontageRule
{
    public static int ForSoldierCount(int soldiers)
    {
        if (soldiers <= 1) return 1;
        if (soldiers <= 4) return soldiers;
        if (soldiers <= 10) return CeilDiv(soldiers, 2);
        if (soldiers <= 16) return CeilDiv(soldiers, 3);
        return CeilDiv(soldiers, 4);
    }

    static int CeilDiv(int value, int divisor) => Math.Max(1, (value + divisor - 1) / divisor);
}
