using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface TakeDamageInterface
{
    public bool Wounded { get; }
    public void TakeDamage(Weapon weapon, int damage);
    public void Die();
}
