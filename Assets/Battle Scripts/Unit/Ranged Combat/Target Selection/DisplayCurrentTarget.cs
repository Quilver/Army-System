using Shooting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RangedWeapons
{
    [RequireComponent(typeof(IRangedTargeter), typeof(LineRenderer))]
    public class DisplayCurrentTarget : MonoBehaviour
    {
        IRangedTargeter target;
        LineRenderer lineRenderer;
        // Start is called before the first frame update
        void Start()
        {
            target = GetComponent<IRangedTargeter>();
            lineRenderer = GetComponent<LineRenderer>();
        }

        // Update is called once per frame
        void Update()
        {
            if (target.Target != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, transform.parent.position);
                lineRenderer.SetPosition(1, target.Target.position);
            }
            else
                lineRenderer.enabled = false;
        }
    }
}