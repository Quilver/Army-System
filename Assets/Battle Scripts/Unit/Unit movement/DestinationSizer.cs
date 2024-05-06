using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace UnitSizing
{
    public class DestinationSizer : MonoBehaviour
    {
        UnitPositionR unit;
        RegimentSizer regimentBox;
        // Start is called before the first frame update
        void Start()
        {
            var u = GetComponentInParent<UnitR>();
            unit = u.Movement;
            regimentBox = u.GetComponentInChildren<RegimentSizer>();
        }

        // Update is called once per frame
        void Update()
        {
            transform.position = Quaternion.Euler(0, 0, unit.position.Rotation) * MidPoint(unit.UnitWidth, unit.Ranks);
            transform.position += (Vector3)Midpoint;
            transform.rotation = Quaternion.Euler(0, 0, unit.position.Rotation);
            transform.localScale = regimentBox.transform.localScale;
        }
        Vector2 MidPoint(int width, int ranks)
        {
            float xOffset = -(width % 2 - 1) / 2f;
            float yOffset = -(ranks - 1) / 2f;
            return new(xOffset, yOffset);
        }
        Vector2 Midpoint
        {
            get
            {
                return (Vector2)unit.position.Location;
            }
        }
    }
}

