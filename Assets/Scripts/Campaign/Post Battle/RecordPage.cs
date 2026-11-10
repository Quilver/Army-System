using System.Collections.Generic;
using AchievementSystem;
using TMPro;
using UnityEngine;

namespace EndGameUI
{
    // A page of the post-battle book (SU-115): a header, rows of figures and a grid of achievement stamps. Rows and stamps
    // are cloned from the templates the PostBattleBookBuilder made, each time the page is shown.
    public sealed class RecordPage : MonoBehaviour
    {
        [SerializeField] GameObject _content;
        [SerializeField] TextMeshProUGUI _header, _subtitle, _stampsHeader, _noStamps, _pageNumber;
        [SerializeField] RectTransform _rows;
        [SerializeField] RecordRow _rowTemplate;
        [SerializeField] RectTransform _stamps;
        [SerializeField] AchievementStamp _stampTemplate;
        [SerializeField] GameObject _divider;

        readonly List<GameObject> _spawned = new();
        int _rowCount;

        public void Show(string header, string subtitle, string stampsHeader, int pageNumber)
        {
            Clear();
            _content.SetActive(true);
            _header.text = header;
            _subtitle.text = subtitle;
            bool stamps = !string.IsNullOrEmpty(stampsHeader);
            _stampsHeader.gameObject.SetActive(stamps);
            _stampsHeader.text = stamps ? stampsHeader : "";
            _stamps.gameObject.SetActive(stamps);
            if (_divider != null) _divider.SetActive(stamps);
            _noStamps.gameObject.SetActive(false);
            _pageNumber.text = pageNumber.ToString();
        }

        public void Hide()
        {
            Clear();
            _content.SetActive(false);
        }

        public RecordRow AddRow(string label, string value = "")
        {
            var row = Instantiate(_rowTemplate, _rows);
            row.transform.SetSiblingIndex(_rowTemplate.transform.GetSiblingIndex() + 1 + _rowCount++);
            row.gameObject.SetActive(true);
            row.Set(label, value);
            _spawned.Add(row.gameObject);
            return row;
        }

        public AchievementStamp AddStamp(Achievement achievement, string reward)
        {
            var stamp = Instantiate(_stampTemplate, _stamps);
            stamp.gameObject.SetActive(true);
            stamp.Set(achievement, reward);
            _spawned.Add(stamp.gameObject);
            return stamp;
        }

        // Text shown in the stamp area when there is nothing to stamp.
        public void ShowNoStamps(string text)
        {
            _noStamps.text = text;
            _noStamps.gameObject.SetActive(true);
        }

        void Clear()
        {
            foreach (var spawned in _spawned)
                if (spawned != null) Destroy(spawned);
            _spawned.Clear();
            _rowCount = 0;
        }
    }
}
