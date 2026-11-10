using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace EndGameUI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class FloatText : MonoBehaviour
    {
        [SerializeField, Range(0.5f, 3)] float _lifeTime = 1;
        [SerializeField, Range(0, 1)] float _endOpacity, _endSpeedFraction;
        [SerializeField, Range(1, 300)] float _speedModifier;
        Vector3 _velocity;
        float _speed, _remainingLife;
        TextMeshProUGUI TextBox;
        public static void FireText(FloatText prefab, Transform position, string text, float force)
        {
            var instance = Instantiate(prefab);
            instance.transform.SetParent(position);
            instance.transform.position = position.position;
            instance.Setup(text, force);
        }
        public void Setup(string text, float force)
        {
            TextBox=GetComponent<TextMeshProUGUI>();
            TextBox.text=text;
            _velocity = Random.insideUnitCircle;
            _velocity.y = Mathf.Abs(_velocity.y);
            _speed = force;
            _remainingLife=_lifeTime;
            StartCoroutine(Travel());
        }
        IEnumerator Travel()
        {
            yield return null;
            while (_remainingLife > 0)
            {
                _remainingLife -= Time.deltaTime;
                float fractionLeft = _remainingLife/ _lifeTime;
                float force = fractionLeft*_speed * _speedModifier;
                TextBox.rectTransform.position += Time.deltaTime * force * _velocity;
                TextBox.alpha = Mathf.Lerp(1, _endOpacity, fractionLeft);
                yield return new WaitForFixedUpdate();
            }
            Destroy(gameObject);
        }
    }
}
