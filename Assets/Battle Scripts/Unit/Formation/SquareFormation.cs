using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Formation
{
    class SquareFormation : MonoBehaviour, IShape
    {
        IFormationData _formationData;
        IFormationData FormationData
        {
            get
            {
                if(_formationData == null) _formationData = GetComponent<IFormationData>();
                return _formationData;
            }
        }
        public Vector2 OffsetFromUnit => new Vector2(0, 0 - (Ranks - 2) * FormationData.ModelSize / 4);

        public Vector2 SizeOfFormation
        {
            get
            {
                return new Vector2(FormationData.Width, Ranks) * FormationData.ModelSize / 2;
            }
        }
        public int Ranks
        {
            get
            {
                if(FormationData.ModelCount == 0) return 0;
                int ranks = FormationData.ModelCount / FormationData.Width;
                if (FormationData.ModelCount % FormationData.Width == 0)
                    return ranks;
                else
                    return ranks + 1;
            }
        }
        BoxCollider2D _collider;
        void Update()
        {
            if(_collider == null) _collider = GetComponentInParent<BoxCollider2D>();
            _collider.offset = OffsetFromUnit;
            _collider.size=SizeOfFormation;
            
        }
    }
}