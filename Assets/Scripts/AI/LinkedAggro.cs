using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#region Enemy Behaviours
public class LinkedAggro : MonoBehaviour
{
    [SerializeField]
    public List<Aggroed> links;
    UnitR _unit;
    [SerializeField]
    float thinkingSpeed;
    float _time = 0;
    // Start is called before the first frame update
    void Start()
    {
        _unit= GetComponent<UnitR>();
    }

    // Update is called once per frame
    void Update()
    {
        _time += Time.deltaTime;
        if (_time > thinkingSpeed)
        {
            _time = 0;
            AggroLogic();
        }
    }
    void AggroLogic()
    {
        foreach (var ally in links)
        {
            if(ally.enemy != null)
                _unit.Movement.MoveTo(ally.enemy.Movement.position.Location);
        }
    }
}
#endregion

