using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitR : MonoBehaviour
{
	#region Properties
	[SerializeField]
	UnitStatsR stats;
	[SerializeField]
	UnitState state;
	[SerializeField]
	UnitPositionR movement;
	List<ModelR> models;
    #endregion
    #region Initialise
    private void Start()
    {
        
    }
    #endregion
    #region Update

    #endregion
    #region Movement
    public void MoveTo(Vector2Int position)
    {

    }
    public void MoveTo(UnitR target)
    {

    }
    #endregion
    #region Combat and death

    #endregion
}
