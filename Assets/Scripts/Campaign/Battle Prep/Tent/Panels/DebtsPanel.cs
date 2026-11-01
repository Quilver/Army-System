using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // War Debts as a book (spec TentHub.md §4.1, SU-53): loan offers pinned to the left page, debts owed pinned to the
    // right page.
    //  - Loan offers: this mission's PreparationData.buyOrders.
    //  - Debts owed: repayment orders open when the tent opened (CampaignData.BuyOrders). As in the old Buy tab, loans
    //    taken in this tent are not listed for repayment until the next mission.
    public class DebtsPanel : TentPanel
    {
        [SerializeField] LoanCard _loanTemplate;
        [SerializeField] RepaymentCard _repaymentTemplate;
        [SerializeField] Transform _loanList, _repayList;
        [SerializeField] TextMeshProUGUI _loanEmpty, _repayEmpty, _treasury;

        readonly List<LoanCard> _loans = new();
        readonly List<RepaymentCard> _repayments = new();

        public override void Initialise()
        {
            _loanTemplate.gameObject.SetActive(false);
            _repaymentTemplate.gameObject.SetActive(false);

            var offers = TentHubController.Data != null ? TentHubController.Data.buyOrders : null;
            if (offers != null)
                for (int i = 0; i < offers.Count; i++)
                {
                    var card = Instantiate(_loanTemplate, _loanList);
                    card.gameObject.SetActive(true);
                    card.transform.localRotation = Quaternion.Euler(0, 0, Tilt(i));
                    card.Set(offers[i]);
                    _loans.Add(card);
                }
            _loanEmpty.gameObject.SetActive(_loans.Count == 0);

            var orders = Campaign.CampaignDataManager.Data.BuyOrders;
            if (orders != null)
            {
                var snapshot = new List<BuyOrders>(orders);
                for (int i = 0; i < snapshot.Count; i++)
                {
                    var card = Instantiate(_repaymentTemplate, _repayList);
                    card.gameObject.SetActive(true);
                    card.transform.localRotation = Quaternion.Euler(0, 0, -Tilt(i));
                    card.Set(snapshot[i]);
                    _repayments.Add(card);
                }
            }
            _repayEmpty.gameObject.SetActive(_repayments.Count == 0);
        }

        // Cards are stuck on slightly crooked.
        static float Tilt(int index) => index % 3 == 0 ? -1.2f : index % 3 == 1 ? 0.9f : -0.4f;

        public override Selectable FirstSelectable
        {
            get
            {
                foreach (var card in _loans) return card.GetComponent<Button>();
                return base.FirstSelectable;
            }
        }

        void Update()
        {
            foreach (var card in _loans) card.Refresh();
            foreach (var card in _repayments) card.Refresh();
            var data = Campaign.CampaignDataManager.Data;
            _treasury.text = $"Treasury: <b>{data.Money} gold</b>   ·   Prestige: <b>{data.CurrentPrestige}/{data.MaxPrestige}</b>";
        }
    }
}
