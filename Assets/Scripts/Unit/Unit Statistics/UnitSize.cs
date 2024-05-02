using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]
public class UnitSize : MonoBehaviour
{
    [Range(1, 6)]
    public int UnitWidth;
    [Range(1, 24)]
    public int StartingSize;
    RegimentSizer regimentSizer;
    // Start is called before the first frame update
    void Start()
    {
        regimentSizer= GetComponentInChildren<RegimentSizer>();
    }

    // Update is called once per frame
    void Update()
    {
        GetComponentInChildren<RegimentSizer>().SetBox(UnitWidth, StartingSize);
    }
}
