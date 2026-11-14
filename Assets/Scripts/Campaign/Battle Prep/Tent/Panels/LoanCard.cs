using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // A loan offer pinned to the Debts book (spec TentHub.md §4.1, SU-53). Click to take the loan; click again to give it
    // back. Taking it needs and spends its prestige cost, gives its gold and adds a repayment order for its cost.
    // The offer is a LoanOffer made from a story-vault note (SU-146).
    public class LoanCard : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] CanvasGroup _group;
        [SerializeField] TextMeshProUGUI _title, _terms, _status, _details;
        [SerializeField] GameObject _stamp;

        LoanOffer _offer;
        bool _taken;

        void Awake() => _button.onClick.AddListener(Toggle);

        public void Set(LoanOffer offer)
        {
            _offer = offer;
            _title.text = offer.Title;
            string interest = offer.InterestPercent != 0 ? $"  ·  {offer.InterestPercent}% interest" : "";
            _terms.text = $"<b>+{offer.Gold} gold</b>{interest}  ·  repay {offer.Cost} in {Months(offer.Months)}";
            _details.text = offer.Details;
            Refresh();
        }

        void Toggle()
        {
            var data = Campaign.CampaignDataManager.Data;
            if (_taken)
            {
                // The loan's gold must still be in the chest to give it back (SU-53 assumption).
                if (data.Money < _offer.Gold) return;
                data.Money -= _offer.Gold;
                data.CurrentPrestige += _offer.PrestigeCost;
                data.BuyOrders.Remove(_offer.ToRepayment());
                _taken = false;
            }
            else
            {
                if (_offer.PrestigeCost > data.CurrentPrestige) return;
                data.Money += _offer.Gold;
                data.CurrentPrestige -= _offer.PrestigeCost;
                data.BuyOrders.Add(_offer.ToRepayment());
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
                can = data.Money >= _offer.Gold;
                _status.text = can ? "Taken. Click to give it back." : "<color=#9a2b12>Taken. The gold is already spent.</color>";
            }
            else
            {
                can = _offer.PrestigeCost <= data.CurrentPrestige;
                _status.text = can
                    ? $"Costs {_offer.PrestigeCost} prestige. Click to take."
                    : $"<color=#9a2b12>Needs {_offer.PrestigeCost} prestige (you have {data.CurrentPrestige})</color>";
            }
            _group.alpha = can || _taken ? 1f : 0.65f;
        }

        internal static string Months(int months) => months == 1 ? "1 month" : $"{months} months";
    }
}
