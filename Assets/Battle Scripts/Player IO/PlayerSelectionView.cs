using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerSelectionView : MonoBehaviour
{
    bool _selected;
    IUnit _unit;
    Light2D _light;
    private void Awake()
    {
        _unit = GetComponentInParent<IUnit>();
        _light = GetComponent<Light2D>();
        _light.enabled = false;
        PlayerController.SelectUnit += Selection;
    }
    void Selection(IUnit unit)
    {
        if (_selected && unit != null)
        {
            _selected = false;
            _light.enabled = false;
        }
        else if (!_selected && unit == _unit)
        {
            _selected = true;
            _light.enabled = true;
        }
    }
    private void OnDestroy()
    {
        PlayerController.SelectUnit -= Selection;
    }
}
