using System.Collections.Generic;
using UnityEngine;

namespace BattlePrep
{
    // A loan offer for the tent's War Debts book, made from a markdown note (format in LoanMarkdown). Story-vault notes
    // with a Gold property get one automatically, next to the note, rebuilt whenever the note is saved. List it in a
    // mission's PreparationData.loans. Taking it gives Gold and spends Prestige Cost; the player then owes Cost.
    public class LoanOffer : Cutscene.MarkdownAsset
    {
        [SerializeField] string _title;
        [SerializeField, Tooltip("Gold received when the loan is taken.")] int _gold;
        [SerializeField, Tooltip("Gold to repay.")] int _cost;
        [SerializeField, Tooltip("Months until the repayment is due.")] int _months;
        [SerializeField, Tooltip("Prestige needed to take the loan, and spent when it is taken.")] int _prestigeCost;
        [SerializeField, TextArea(2, 6)] string _details;

        public string Title => _title;
        public int Gold => _gold;
        public int Cost => _cost;
        public int Months => _months;
        public int PrestigeCost => _prestigeCost;
        public string Details => _details;

        // How much more is repaid than received, in percent of the gold received.
        public int InterestPercent => _gold > 0 ? Mathf.RoundToInt((_cost - _gold) * 100f / _gold) : 0;

        // The debt the player owes once the loan is taken (CampaignData.BuyOrders).
        public BuyOrders ToRepayment() => new BuyOrders
        {
            orderInfo = _title, Cost = _cost, TimeLeft = _months, orderDetails = _details,
        };

        public void Set(LoanMarkdown.Result loan, List<string> warnings)
        {
            _title = loan.Title;
            _gold = loan.Gold;
            _cost = loan.Cost;
            _months = loan.Months;
            _prestigeCost = loan.PrestigeCost;
            _details = loan.Details;
            SetWarnings(warnings);
        }
    }
}
