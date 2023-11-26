using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public struct CombatStats
{
    public int leadership;
    public int wounds, toughness, armour;
    public int weaponSkill, strength;
}

public class CombatEngine: MonoBehaviour{
    public enum fight
    {
        Front,
        Flank,
        Rear
    }
    public struct AngleOfAttack
    {
        public fight f;
        public int contacts;
    }
    Unit unitA, unitB;
    int attacksA, attacksB;
    fight positionA, positionB;
    public CombatEngine create(Unit unit1, Unit unit2)
    {
        unitA = unit1;
        unitB = unit2;
        Debug.Log("Starting combat between " + unitA.gameObject.name + " and " + unitB.gameObject.name);
        unitA.inCombat = true;
        unitB.inCombat = true;
        AngleOfAttack angleA, angleB;
        angleA = unitA.numberOfContacts(unitB.gameObject);
        angleB = unitB.numberOfContacts(unitA.gameObject);
        positionA = angleA.f;//getAngle(unitA, unitB);
        //Debug.Log("Unit " + unitA.gameObject.name + " " + positionA);
        positionB = angleB.f;
        //Debug.Log("Unit " + unitB.gameObject.name + " "+ positionB);
        attacksA = angleA.contacts;
        if(positionA == fight.Front) { attacksA *= unitA.rankBonus(); }
        attacksB = angleB.contacts;
        if (positionB == fight.Front) { attacksB *= unitB.rankBonus(); }
        InvokeRepeating("melee", 0f, 1f);
        return this;
    }
    void OnDestroy()
    {
        Debug.Log("Ending combat");
        unitA.inCombat = false;
        unitB.inCombat = false;
        if (unitA.models == null)
        {
            Debug.Log("Deleting unit: " + unitA.gameObject.name);
            Destroy(unitA.gameObject);
        }
        if (unitB.models == null)
        {
            Debug.Log("Deleting unit: " + unitB.gameObject.name);
            Destroy(unitB.gameObject);
        }
    }
    public void melee()
    {
        if (unitA == null || unitB == null || unitA.models == null || unitB.models == null)
        {
            Destroy(this);
        }
        //Debug.Log("Updating");
        int aKills = combat(attacksA, unitA, unitB);
        int bKills = combat(attacksB, unitB, unitA);
        //Debug.Log("Kills: " + aKills + ", " + bKills);
        unitA.deaths(bKills);
        unitB.deaths(aKills);
        if(unitA.models == null || unitB.models == null)
        {
            Destroy(this);
        }
    }
    static int combat(int attacks, Unit attacker, Unit defender)
    {
        int casualties = 0;
        casualties = combat(attacker.Stats.WeaponSkill, (2 * defender.Stats.WeaponSkill) / 3, attacks);
        casualties = combat(attacker.Stats.AttackStrength, defender.Stats.Defence, casualties);
        return casualties;
    }
    //
    public static int save(int stat, int numberOfTests)
    {
        int successes = 0;
        float probability = stat;
        if (probability < 10) { probability = 10; }
        if (probability < 80) { probability = 80; }
        for (int i = 0; i < numberOfTests; i++)
        {
            if (probability >= Random.Range(0, 100)) { successes++; }
        }
        return successes;
    }
    public static int combat(int stat1, int stat2, int numberOfTests)
    {
        int successes = 0;
        float probability = (stat1 - stat2);
        if (probability < 10) { probability = 10; }
        if (probability < 80) { probability = 80; }
        for (int i = 0; i < numberOfTests; i++)
        {
            if(probability >= Random.Range(0, 100)) { successes++; }
        }
        return successes;
    }
    //called once at initialisation
    fight getAngle(Unit unit1, Unit unit2)
    {
        //Debug.Log("Getting angle of attack for unit: " + unit1.gameObject.name);
        Vector2 testTile = unit1.position - unit1.direction;
        if (Map.Instance.getTile(testTile).unit == unit2)
        {
            return fight.Front;
        }
        testTile = unit1.models[unit1.models.Count - 1].position - unit1.direction;
        if (Map.Instance.getTile(testTile).unit == unit2)
        {
            return fight.Rear;
        }
        else
        {
            return fight.Flank;
        }
    }
    public static float AngleBetweenVector2(Vector2 vec1, Vector2 vec2)
    {
        Vector2 diference = vec2 - vec1;
        float sign = (vec2.y < vec1.y) ? -1.0f : 1.0f;
        return Vector2.Angle(Vector2.right, diference) * sign;
    }
}
