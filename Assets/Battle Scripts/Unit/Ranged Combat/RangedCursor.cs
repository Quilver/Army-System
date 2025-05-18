using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(SpriteRenderer))]
public class RangedCursor : MonoBehaviour
{
    SpriteRenderer sprite;
    [SerializeField]
    Transform cursorToFollow;
    PlayerSelectUnits playerSelectUnits;
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        playerSelectUnits = cursorToFollow.GetComponent<PlayerSelectUnits>();
        PlayerSelectUnits.SelectUnit += UnitSelection;
        sprite.enabled = false;
        enabled = false;
        Battle.Instance.Deploy += () => enabled = true;
    }
    RangedWeapon rangedWeapon;
    void UnitSelection(IUnit unit)
    {
        rangedWeapon = unit.GetComponentInChildren<RangedWeapon>();
    }
    void Update()
    {
        if (rangedWeapon == null || rangedWeapon.gameObject.activeSelf == false || rangedWeapon.enabled == false || rangedWeapon._projectile == null)
        {
            sprite.enabled = false;
            return; 
        }
        sprite.enabled = true;
        transform.position = cursorToFollow.position;
        transform.localScale = rangedWeapon._projectile.Inaccuracy(Vector2.Distance(transform.position, rangedWeapon.transform.parent.position), rangedWeapon.accuracy) * Vector3.one;
    }
    private void OnDrawGizmos()
    {
        if (rangedWeapon == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, rangedWeapon.transform.parent.position);
    }
}
