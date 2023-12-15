using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Army/UnitType")]
public class UnitType : ScriptableObject
{
    [SerializeField] string _UnitName;
    public string UnitName { get { return _UnitName; } }
    [SerializeField] Sprite _UnitProfile;
    public Sprite UnitProfile { get { return _UnitProfile; } }
    [SerializeField] GameObject _Visual;
    public GameObject Visual { get { return _Visual; } }
    [SerializeField] UnitStats _UnitStats;
    public UnitStats Stats { get { return _UnitStats; } }
}
