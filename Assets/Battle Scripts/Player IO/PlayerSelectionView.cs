using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerSelectionView : MonoBehaviour
{
    bool _selected;
    IUnit _unit;
    MovementSystem.IMoveOrders _moveOrders;
    Light2D _light;
    AudioSource _unitAudioSource;
    [SerializeField]
    List<AudioClip> _unitSelected, _unitReceivedOrder, _unitFlees, _unitDies;
    private void Awake()
    {
        _unit = GetComponentInParent<IUnit>();
        _light = GetComponent<Light2D>();
        _unitAudioSource = GetComponent<AudioSource>();
        _light.enabled = false;
        _moveOrders = GetComponentInParent<MovementSystem.IMoveOrders>();
        _moveOrders.OrderReceived += Order;
        PlayerSelectUnits.SelectUnit += Selection;
        _unit.StateChanged += Flee;
        _unit.UnitDestroyed += Death;
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
            PlayRandomisedClip(_unitSelected);
            _light.enabled = true;
        }
    }
    void Order()
    {
        PlayRandomisedClip(_unitReceivedOrder);
    }
    void Flee(UnitState state)
    {
        if (state != UnitState.Fleeing) return;
        PlayRandomisedClip(_unitFlees);
    }
    void PlayRandomisedClip(List<AudioClip> clips)
    {
        _unitAudioSource.volume = Random.Range(0.9f, 1);
        _unitAudioSource.pitch = Random.Range(0.9f, 1.1f);
        _unitAudioSource.clip = clips[Random.Range(0, clips.Count - 1)];
        _unitAudioSource.Play();
    }
    [SerializeField, Range(1, 5)]
    float DeathScreamLength = 3;
    void Death()
    {
        transform.parent = null;
        _light.enabled=false;   
        PlayRandomisedClip(_unitDies);
        PlayerSelectUnits.SelectUnit -= Selection;
        _moveOrders.OrderReceived -= Order;
        _unit.StateChanged -= Flee;
        _unit.UnitDestroyed -= Death;
        Invoke("_Death", DeathScreamLength);
    }
    void _Destroy()=>Destroy(gameObject);
    
}
