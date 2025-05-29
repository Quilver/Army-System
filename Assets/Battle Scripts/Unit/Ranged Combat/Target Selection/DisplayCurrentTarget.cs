using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RangedWeapons
{
    [RequireComponent(typeof(LineRenderer))]
    public class DisplayCurrentTarget : MonoBehaviour
    {
        RangedWeapon rangedWeapon;
        LineRenderer lineRenderer;
        // Start is called before the first frame update
        void Start()
        {
            rangedWeapon = GetComponentInParent<RangedWeapon>();
            lineRenderer = GetComponent<LineRenderer>();
        }

        // Update is called once per frame
        void Update()
        {
            if (rangedWeapon.CurrentTarget != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, transform.parent.position);
                lineRenderer.SetPosition(1, rangedWeapon.CurrentTarget.transform.position);
            }
            else
                lineRenderer.enabled = false;
        }
    }
}