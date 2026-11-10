using TMPro;
using UnityEngine;

namespace BattlePrep.Tent
{
    // Placeholder for systems not built yet: Inventory (Weapon Rack) and Shop (Merchant). Spec TentHub.md §4.4.
    public class StubPanel : TentPanel
    {
        [SerializeField] TextMeshProUGUI _title, _body;

        public void Configure(string title, string body)
        {
            _title.text = title;
            _body.text = body;
        }
    }
}
