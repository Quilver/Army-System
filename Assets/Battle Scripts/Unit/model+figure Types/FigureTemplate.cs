using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Rigidbody2D))]
public abstract class FigureTemplate : MonoBehaviour
{
    protected UnitTemplate unit;
    public UnitTemplate Unit { get { return unit; } }
    protected Rigidbody2D body;

    #region Formation setup and update
    [SerializeField, Range(0.2f, 1)]
    float HoldDampRatio, MoveDampRatio;
    SpringJoint2D[] joints;
    public void Setup(Rigidbody2D[] pins, Transform unit)
    {
        int i = 1;
        joints = new SpringJoint2D[3];
        foreach (var joint in GetComponents<SpringJoint2D>())
        {
            joint.connectedBody = pins[i];
            joint.distance = Vector3.Distance(transform.position, pins[i].transform.position);
            joints[i - 1] = joint;
            i++;
        }
        this.unit = unit.GetComponent<SoftBody.SoftBodyUnit>();
        body = GetComponent<Rigidbody2D>();
        //ModelContainer.AddModel(this);
        InCombatWith = new();
        fieldOfView = unit.GetComponentInChildren<FieldofView>();
        GetComponent<SpriteRenderer>().color = (unit.GetComponentInParent<Army>().controller == Army.Controller.Player) ? Color.blue : Color.red;
        Invoke("Attack", Random.Range(0.1f, this.unit.Stats.AttackSpeed.CurrentStat / 10f));
    }
    #endregion
    #region Movement
    public void ResetPositionInFormation(Rigidbody2D[] pins, Vector2 position)
    {
        Debug.DrawLine(transform.position, position);
        for (int i = 0; i < 3; i++)
        {
            joints[i].distance = Vector3.Distance(position, pins[i + 1].transform.position);
        }
    }
    public void Move(bool moving)
    {
        foreach (var joint in joints)
            if (moving) joint.dampingRatio = MoveDampRatio;
            else joint.dampingRatio = HoldDampRatio;
    }
    #endregion
    #region Melee
    [SerializeField, Range(0.3f, 2)]
    float MeleeRadius = 1.2f;
    public bool InCombat
    {
        get
        {
            return InCombatWith != null && InCombatWith.Count != 0;
        }
    }
    [SerializeField]
    List<FigureTemplate> InCombatWith;
    //Checks if the model has charged an enemy
    private void OnCollisionEnter2D(Collision2D collision)
    {
        var collUnit = collision.gameObject.GetComponent<FigureTemplate>();
        if (collUnit == null || collUnit.unit == unit) return;
        if (InCombatWith.Contains(collUnit) || !Battle.Instance.Enemies(unit, collUnit.unit)) return;
        InCombatWith.Add(collUnit);
        //unit.StartFight(collUnit.unit);
        if (body.velocity.magnitude <= 1) return;
        collUnit.Hit(body.velocity.magnitude * body.mass, this);
    }
    //Randomly selects enemy that they are in melee with to do damage to
    void Attack()
    {
        Invoke("Attack", unit.Stats.AttackSpeed.CurrentStat / 10f);
        InCombatWith.RemoveAll(item => item == null);
        InCombatWith.RemoveAll(enemy => Vector2.Distance(transform.position, enemy.transform.position) > MeleeRadius);

        var target = InCombatWith[Random.Range(0, InCombatWith.Count)];
        float power = Random.Range(0f, unit.Stats.AttackPower.CurrentStat);
        target.body.AddForce((target.transform.position - transform.position).normalized * 100 * Mathf.Sqrt(power));
        target.Hit(power, this);
    }
    enum Facing
    {
        Front,
        Flank,
        Rear
    }
    //Checks if it is flanked
    Facing GetFace(Transform enemy)
    {
        var Angle = Vector2.SignedAngle(unit.transform.up, enemy.position - transform.position);
        if (Mathf.Abs(Angle) < 45) return Facing.Front;
        else if (Mathf.Abs(Angle) < 135) return Facing.Flank;
        else return Facing.Rear;
    }
    void Hit(float Power, FigureTemplate attacker)
    {
        if (GetFace(attacker.transform) == Facing.Rear) Power *= 5;
        else if (GetFace(attacker.transform) == Facing.Flank) Power *= 2;
        Hit(Power);
    }
    void Hit(float Power)
    {
        float defenceScore = Random.Range(0f, unit.Stats.Defence.CurrentStat);
        //if (defenceScore < Power) unit.GetComponent<UnitFormation>().Death(this);
    }
    #endregion
    #region Shooting
    FieldofView fieldOfView;
    public void Shoot(GameObject projectile, float power, Transform target)
    {
        if (fieldOfView == null) Debug.LogError(gameObject.name + " unit is missing field of view");
        var direction = target.position - transform.position;
        RaycastHit2D raycast2D;
        bool hit = false;
        for (int i = 0; i < 10; i++) {
            direction=Vector2.Lerp(unit.transform.up, (target.position - transform.position).normalized, i / 10f);
            raycast2D = Physics2D.Raycast(transform.position, direction, float.MaxValue, fieldOfView.SensorLayerMask);
            if (raycast2D && raycast2D.rigidbody.transform != target) continue;
            hit = true;
            break;
        }
        if(!hit)return;
        var shot = Instantiate(projectile);
        shot.transform.position = transform.position;
        direction = Quaternion.Euler(0f, 0f, Random.Range(-15, 15)) * direction;
        shot.GetComponent<Projectile>().Setup(direction, power, unit);
    }
    #endregion
}
