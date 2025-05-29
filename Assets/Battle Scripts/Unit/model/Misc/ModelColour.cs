using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
namespace ModelComponents
{
    public class ModelColour : MonoBehaviour
    {
        [SerializeField]
        Color player, enemy, Attack;
        Color defaultColour;
        SpriteRenderer sprite;
        // Start is called before the first frame update
        void Start()
        {
            sprite = GetComponent<SpriteRenderer>();
            var data = GetComponent<ModelComponents.IUnitData>();
            var army = data.Unit.GetComponentInParent<Army>();
            GetComponentInChildren<IMeleeAttack>().Strike += StrikeAnim;
            sprite.color = army.ArmyColour;
            defaultColour = sprite.material.GetColor("_Color");
        }
        void StrikeAnim()
        {
            sprite.material.SetColor("_Color", Attack);
            StartCoroutine(Strike());
        }
        IEnumerator Strike()
        {
            yield return new WaitForSeconds(0.2f);
            float timeLeft = 0.15f;
            while(timeLeft > 0f)
            {
                timeLeft -= Time.fixedDeltaTime;
                sprite.material.SetColor("_Color", defaultColour);
                yield return new WaitForFixedUpdate();
            }
        }
        private void FixedUpdate()
        {
            var force = sprite.material.GetVector("_HitForce");
            sprite.material.SetVector("_HitForce", Vector3.MoveTowards(force, Vector3.zero, 2 * Time.deltaTime));
        }
    }
}