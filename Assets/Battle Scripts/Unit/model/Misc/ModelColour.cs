using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    public class ModelColour : MonoBehaviour
    {
        [SerializeField]
        Color player, enemy;
        // Start is called before the first frame update
        void Start()
        {
            var sprite = GetComponent<SpriteRenderer>();
            var data = GetComponent<ModelComponents.IUnitData>();
            var army = data.Unit.GetComponentInParent<Army>();
            sprite.color = army.ArmyColour;
        }

    }
}