using TMPro;
using UnityEngine;

namespace EndGameUI
{
    // One line of figures on a post-battle page: a label and a value that counts up (SU-115).
    public sealed class RecordRow : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _label;
        [SerializeField] TextMeshProUGUI _value;

        public TextMeshProUGUI Value => _value;

        public void Set(string label, string value)
        {
            _label.text = label;
            _value.text = value;
        }
    }
}
