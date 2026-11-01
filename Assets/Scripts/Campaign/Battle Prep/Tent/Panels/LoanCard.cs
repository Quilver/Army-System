using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // A loan offer pinned to the Debts book (spec TentHub.md §4.1, SU-53). Click to take the loan; click again to give it
    // back. Same rules as the old SellView: needs prestige, gives gold, costs prestige, adds a repayment order.
    public class LoanCard : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] CanvasGroup _group;
        [SerializeField] TextMeshProUGUI _title, _terms, _status, _details;
        [SerializeField] GameObject _stamp;

        SellOrders _offer;
        bool _taken;

        void Awake() => _button.onClick.AddListener(Toggle);

        public void Set(SellOrders offer)
        {
            _offer = offer;
            _title.text = offer.orderInfo;
            var repayment = offer.Buy();
            _terms.text = $"<b>+{offer.gold} gold</b>  ·  {offer.interestRate}% interest  ·  repay {repayment.Cost} in {Months(offer.timeLeft)}";
            _details.text = offer.orderDetails;
            Refresh();
        }

        void Toggle()
        {
            var data = Campaign.CampaignDataManager.Data;
            if (_taken)
            {
                // The loan's gold must still be in the chest to give it back (SU-53 assumption).
                if (data.Money < _offer.gold) return;
                data.Money -= _offer.gold;
                data.CurrentPrestige += _offer.PrestigeRequired;
                data.BuyOrders.Remove(_offer.Buy());
                _taken = false;
            }
            else
            {
                if (_offer.PrestigeRequired > data.CurrentPrestige) return;
                data.Money += _offer.gold;
                data.CurrentPrestige -= _offer.PrestigeRequired;
                data.BuyOrders.Add(_offer.Buy());
                _taken = true;
            }
            Refresh();
        }

        public void Refresh()
        {
            var data = Campaign.CampaignDataManager.Data;
            _stamp.SetActive(_taken);
            bool can;
            if (_taken)
            {
                can = data.Money >= _offer.gold;
                _status.text = can ? "Taken. Click to give it back." : "<color=#9a2b12>Taken. The gold is already spent.</color>";
            }
            else
            {
                can = _offer.PrestigeRequired <= data.CurrentPrestige;
                _status.text = can
                    ? $"Costs {_offer.PrestigeRequired} prestige. Click to take."
                    : $"<color=#9a2b12>Needs {_offer.PrestigeRequired} prestige (you have {data.CurrentPrestige})</color>";
            }
            _group.alpha = can || _taken ? 1f : 0.65f;
        }

        internal static string Months(int months) => months == 1 ? "1 month" : $"{months} months";
    }
}
