using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // Tactical Map (spec TentHub.md §4.3).
    //  - Mission: briefing text and image, mission number, Replay briefing.
    //  - Deployment: roster cards (click to deploy / undeploy, gold deducted and refunded, 2 to 5 characters; mandatory
    //    characters start deployed, free and locked), the deployed characters with their cost (drag one onto another to
    //    make a composite troop, SU-117), gold remaining and spent, readiness and March Out.
    public class MapPanel : TentPanel
    {
        [Header("Pages")]
        [SerializeField] Button _missionTab, _deploymentTab;
        [SerializeField] GameObject _missionPage, _deploymentPage;

        [Header("Mission")]
        [SerializeField] TextMeshProUGUI _missionTitle, _briefingText;
        [SerializeField] Image _missionImage;
        [SerializeField] Button _replayBriefing;

        [Header("Deployment")]
        [SerializeField] RosterCard _rosterCardTemplate;
        [SerializeField] Transform _rosterList, _deployedList;
        [SerializeField] DeployedTroopCard _deployedCardTemplate;
        [SerializeField] TextMeshProUGUI _deployedHeader, _gold, _readiness;
        [SerializeField] Button _marchOut;

        readonly List<RosterCard> _rosterCards = new();
        readonly List<DeployedTroopCard> _cards = new();
        int _shownVersion = -1;
        int _page;
        // A refused pairing's reason, shown in place of the readiness line for a moment (SU-117).
        string _notice;
        float _noticeUntil;
        const float NoticeSeconds = 2.5f;

        public override Selectable FirstSelectable => _missionTab != null ? _missionTab : base.FirstSelectable;

        public override void Initialise()
        {
            var data = TentHubController.Data;
            // "Mission 2: <title>" from the mission sheet (SU-119), else the number alone.
            var mission = Campaign.Missions.MissionCatalog.ByNumber(Campaign.CampaignDataManager.Data.CurrentLevel);
            _missionTitle.text = mission != null ? mission.DisplayName : $"Mission {Campaign.CampaignDataManager.Data.CurrentLevel}";
            _briefingText.text = data != null ? data.briefing : "";
            _missionImage.sprite = data != null ? data.image : null;
            _missionImage.enabled = _missionImage.sprite != null;

            _rosterCardTemplate.gameObject.SetActive(false);
            _deployedCardTemplate.gameObject.SetActive(false);
            // Right-aligned so a follower's narrower card shows indented under its leader (SU-117).
            var deployedLayout = _deployedList.GetComponent<VerticalLayoutGroup>();
            if (deployedLayout != null) deployedLayout.childAlignment = TextAnchor.UpperRight;
            var roster = Campaign.CampaignDataManager.Data.Roster;
            if (roster != null)
                foreach (var character in roster)
                {
                    if (character == null || character.character == null) continue;
                    var card = Instantiate(_rosterCardTemplate, _rosterList);
                    card.gameObject.SetActive(true);
                    card.Set(character);
                    _rosterCards.Add(card);
                }
        }

        protected override void Wire()
        {
            _missionTab.onClick.AddListener(() => Show(0));
            _deploymentTab.onClick.AddListener(() => Show(1));
            _replayBriefing.onClick.AddListener(() => TentHubController.Instance.PlayBriefing());
            _marchOut.onClick.AddListener(() => TentHubController.Instance.TryLaunch());
            DeployedTroopCard.PairRefused += ShowNotice;
        }

        void OnDestroy() => DeployedTroopCard.PairRefused -= ShowNotice;

        void ShowNotice(string reason)
        {
            _notice = reason;
            _noticeUntil = Time.unscaledTime + NoticeSeconds;
        }

        protected override void OnOpened()
        {
            Show(_page);
            RefreshDeployed(true);
        }

        public void ShowDeployment() => Show(1);

        void Show(int page)
        {
            _page = page;
            ShowPage(page, new[] { _missionPage, _deploymentPage }, new[] { _missionTab, _deploymentTab });
        }

        void Update() => RefreshDeployed(false);

        void RefreshDeployed(bool force)
        {
            foreach (var card in _rosterCards) card.Refresh();

            if (force || _shownVersion != TentDeployment.Version)
            {
                _shownVersion = TentDeployment.Version;
                // Leaders and solo characters in deployed order, each followed by the characters serving under it.
                var deployed = TentDeployment.DisplayOrder();
                while (_cards.Count < deployed.Count)
                    _cards.Add(Instantiate(_deployedCardTemplate, _deployedList));
                for (int i = 0; i < _cards.Count; i++)
                {
                    bool used = i < deployed.Count;
                    _cards[i].gameObject.SetActive(used);
                    if (used) _cards[i].Set(deployed[i]);
                }
                int troops = BattleReport.DeployedTroops != null ? BattleReport.DeployedTroops.Count : deployed.Count;
                _deployedHeader.text = $"Deployed characters ({deployed.Count}/{LaunchRules.MaximumDeployed})  ·  " +
                                       $"{troops} troop{(troops == 1 ? "" : "s")}";
            }

            _gold.text = $"Gold remaining  <b>{Campaign.CampaignDataManager.Data.Money}</b><pos=50%>Spent on troops  <b>{TentDeployment.GoldSpent}</b>";
            bool ready = LaunchRules.CanLaunch(out string reason);
            bool notice = !string.IsNullOrEmpty(_notice) && Time.unscaledTime < _noticeUntil;
            _readiness.text = notice ? _notice : reason;
            _readiness.color = ready && !notice ? new Color(0.25f, 0.45f, 0.15f) : new Color(0.6f, 0.2f, 0.1f);
            _marchOut.interactable = ready;
        }

    }
}
