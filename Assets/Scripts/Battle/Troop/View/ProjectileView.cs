using System.Collections.Generic;
using UnityEngine;

// Draws projectiles in flight as short pooled sprites (spec §4.7). Renderers are reused, so shots do not allocate.
public sealed class ProjectileView
{
    const float Length = 0.6f;
    const float Thickness = 0.06f;

    readonly List<SpriteRenderer> _renderers = new();
    Transform _root;

    public void Draw(IReadOnlyList<TroopProjectiles.Projectile> projectiles, Transform parent)
    {
        if (_root == null)
        {
            _root = new GameObject("Projectiles").transform;
            _root.SetParent(parent, false);
        }
        while (_renderers.Count < projectiles.Count) _renderers.Add(Create());

        for (int i = 0; i < _renderers.Count; i++)
        {
            SpriteRenderer renderer = _renderers[i];
            bool used = i < projectiles.Count;
            renderer.enabled = used;
            if (!used) continue;
            var projectile = projectiles[i];
            Vector2 direction = projectile.To - projectile.From;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            renderer.transform.SetPositionAndRotation(projectile.Position, Quaternion.Euler(0f, 0f, angle));
            renderer.color = projectile.Shooter != null ? projectile.Shooter.ArmyColor : Color.white;
        }
    }

    public void Destroy()
    {
        if (_root != null) TroopObjects.Destroy(_root.gameObject);
        _renderers.Clear();
    }

    SpriteRenderer Create()
    {
        var shot = new GameObject("Projectile");
        shot.transform.SetParent(_root, false);
        shot.transform.localScale = new Vector3(Length, Thickness, 1f);
        var renderer = shot.AddComponent<SpriteRenderer>();
        renderer.sprite = SoldierSpriteLibrary.Square;
        renderer.sharedMaterial = SoldierSpriteLibrary.ModelMaterial;
        renderer.sortingLayerName = "Unit";
        renderer.sortingOrder = 2;
        return renderer;
    }
}
