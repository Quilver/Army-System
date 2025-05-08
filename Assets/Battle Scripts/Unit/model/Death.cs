using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    class Death : IDeath
    {

        [SerializeField, Range(3f, 120f)]
        float fadeoutTime = 6;
        [SerializeField]
        SpriteRenderer sprite;
        public override void Die()
        {
            Dead?.Invoke();
            GetComponent<IUnitData>().Unit.DeadModel?.Invoke(transform);
            sprite.color = Desaturate(0.4f, sprite.color);
            GetComponent<Collider2D>().enabled = false;
            foreach (var joint in GetComponents<Joint2D>())
                Destroy(joint);
            Invoke("Remove", fadeoutTime);
        }
        Color Desaturate(float percentage, Color color)
        {
            float L = 0.3f * color.r + 0.6f * color.g + 0.1f * color.b;
            return new Color(color.r + percentage * (L - color.r),
                color.g + percentage * (L - color.g),
                color.b + percentage * (L - color.b),
                color.a *= 1 - percentage);
        }
        void Remove()
        {
            Destroy(gameObject);
        }

    }
}