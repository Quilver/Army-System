using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // An overlay panel opened from a tent object (spec TentHub.md §4). One open at a time; Esc / right click / the
    // close button return to the hub (TentHubController).
    public abstract class TentPanel : MonoBehaviour
    {
        [SerializeField] protected Button _closeButton;

        bool _wired;

        // The tent object that opened this panel, reselected on close for keyboard / controller users.
        public TentHotspot Opener { get; set; }

        // Selected when the panel opens, for keyboard / controller users.
        public virtual Selectable FirstSelectable => _closeButton;

        // Builds the panel's contents once, at scene start, while the panel is still inactive.
        public virtual void Initialise() { }

        public void Open()
        {
            gameObject.SetActive(true);
            if (!_wired)
            {
                _wired = true;
                if (_closeButton != null)
                    _closeButton.onClick.AddListener(() => TentHubController.Instance.ClosePanel());
                Wire();
            }
            OnOpened();
        }

        public void Close()
        {
            OnClosed();
            gameObject.SetActive(false);
        }

        // Add runtime button listeners here (runs on first open).
        protected virtual void Wire() { }
        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }

        // Shows pages[index], hides the rest, and colours the matching tab.
        protected static void ShowPage(int index, GameObject[] pages, Button[] tabs)
        {
            for (int i = 0; i < pages.Length; i++)
                if (pages[i] != null) pages[i].SetActive(i == index);
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null || tabs[i].targetGraphic == null) continue;
                tabs[i].targetGraphic.color = i == index ? TabSelected : TabNormal;
            }
        }

        static readonly Color TabSelected = new(0.49f, 0.18f, 0.07f, 1f);
        static readonly Color TabNormal = new(0.36f, 0.27f, 0.2f, 1f);
    }
}
