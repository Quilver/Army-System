using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // A debt owed, pinned to the Debts book (spec TentHub.md §4.1, SU-53). Click to repay; click again to undo.
    // Same rules as the old BuyView.
    public class RepaymentCard : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] CanvasGroup _group;
        [SerializeField] TextMeshProUGUI _title, _terms, _status, _details;
        [SerializeField] GameObject _stamp;

        BuyOrders _order;
        bool _repaid;

        void Awake() => _button.onClick.AddListener(Toggle);

        public void Set(BuyOrders order)
        {
            _order = order;
            _title.text = order.orderInfo;
            _terms.text = $"Owe <b>{order.Cost} gold</b>  ·  due in {LoanCard.Months(order.TimeLeft)}";
            _details.text = order.orderDetails;
            Refresh();
        }

        void Toggle()
        {
            var data = Campaign.CampaignDataManager.Data;
            if (_repaid)
            {
                data.Money += _order.Cost;
                data.BuyOrders.Add(_order);
                _repaid = false;
            }
            else
            {
                if (_order.Cost > data.Money) return;
                data.Money -= _order.Cost;
                data.BuyOrders.Remove(_order);
                _repaid = true;
            }
            Refresh();
        }

        public void Refresh()
        {
            var data = Campaign.CampaignDataManager.Data;
            _stamp.SetActive(_repaid);
            bool can = _repaid || _order.Cost <= data.Money;
            _status.text = _repaid ? "Repaid. Click to undo."
                : can ? "Click to repay." : $"<color=#9a2b12>Not enough gold (you have {data.Money})</color>";
            _group.alpha = can ? 1f : 0.65f;
        }
    }
}
