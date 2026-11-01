using System.Collections.Generic;
using Campaign;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // Roster Ledger as a book: one character per page, two pages per spread (spec TentHub.md §4.2, SU-54).
    // Turn pages with the corner buttons, Q / E, Page Up / Down or the gamepad shoulders.
    public class LedgerPanel : TentPanel
    {
        [SerializeField] LedgerPage _leftPage, _rightPage;
        [SerializeField] Button _previous, _next;
        [SerializeField] TextMeshProUGUI _spreadLabel;

        List<RosterCharacter> _roster = new();
        int _spread;

        int SpreadCount => Mathf.Max(1, (_roster.Count + 1) / 2);

        public override Selectable FirstSelectable => _next;

        public override void Initialise() => _roster = CampaignDataManager.Data.Roster ?? new List<RosterCharacter>();

        protected override void Wire()
        {
            _previous.onClick.AddListener(() => Turn(-1));
            _next.onClick.AddListener(() => Turn(1));
        }

        protected override void OnOpened() => ShowSpread(_spread);

        void Update()
        {
            if (TentInput.PagePrevPressed) Turn(-1);
            else if (TentInput.PageNextPressed) Turn(1);
        }

        void Turn(int direction) => ShowSpread(_spread + direction);

        void ShowSpread(int spread)
        {
            _spread = Mathf.Clamp(spread, 0, SpreadCount - 1);
            ShowPage(_leftPage, _spread * 2);
            ShowPage(_rightPage, _spread * 2 + 1);
            _previous.interactable = _spread > 0;
            _next.interactable = _spread < SpreadCount - 1;
            _spreadLabel.text = _roster.Count == 0 ? "The roster is empty"
                : $"{_roster.Count} characters   ·   pages {_spread * 2 + 1}-{_spread * 2 + 2} of {SpreadCount * 2}";
        }

        void ShowPage(LedgerPage page, int index)
        {
            if (index < _roster.Count && _roster[index] != null) page.Show(_roster[index], index + 1);
            else page.ShowEmpty(index + 1);
        }
    }
}
