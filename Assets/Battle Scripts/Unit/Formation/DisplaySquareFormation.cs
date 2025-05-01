using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Formation
{
    class DisplaySquareFormation : MonoBehaviour
    {
        SpriteRenderer _spriteRenderer;
        IShape _shape;
        void Start ()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _shape = GetComponent<IShape>();
            _spriteRenderer.enabled = true;
        }
        void Update()
        {
            transform.localPosition = _shape.OffsetFromUnit;
            transform.localScale = _shape.SizeOfFormation;
        }
    }
}