using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace Formation
{
    class DisplaySquareFormation : MonoBehaviour
    {
        [SerializeField]    
        SpriteRenderer _sprite;
        IShape _shape;
        [SerializeField]
        Color Valid, Invalid;
        
        void Start ()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _shape = GetComponent<IShape>();
            _sprite.enabled = true;
        }
        void Update()
        {
            transform.localPosition = _shape.OffsetFromUnit;
            transform.localScale = _shape.SizeOfFormation;
        }
        private void OnDisable()
        {
            _sprite.enabled = false;
        }
        public void UpdateColor(bool valid)
        {
            if(_sprite==null) _sprite = GetComponent<SpriteRenderer>();
            if(valid)
                _sprite.color = Valid;
            else _sprite.color = Invalid;
            _sprite.color= valid ? Valid : Invalid;
        }
    }
}