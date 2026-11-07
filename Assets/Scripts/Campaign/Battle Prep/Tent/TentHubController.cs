using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BattlePrep.Tent
{
    // Runs the pre-battle tent (spec Docs/PreBattle/TentHub.md): Briefing -> Hub <-> Panel / Dialogue -> Launch.
    // Replaces PreparationContoller and its tabs.
    public class TentHubController : MonoBehaviour
    {
        public enum State { Briefing, Hub, Panel, Dialogue }

        static readonly string[] DefaultCourtNames = { "Tactician", "Captain", "Dispatch-bearer" };
        static readonly string[] DefaultCourtLines =
        {
            "The ground favours whoever holds the ridge, my prince. Study the map before you commit.",
            "The men are ready. Say the word and we march.",
            "A rider brought this at dawn. It can wait until after the battle, I think.",
        };

        public static TentHubController Instance { get; private set; }
        // This mission's preparation data (was PreparationContoller.Data).
        public static PreparationData Data { get; private set; }

        [SerializeField, Tooltip("Only used without a MissionCatalog (SU-119): one entry per level, in level order " +
                                 "(copied from the old PreparationContoller). The catalog's missions take precedence.")]
        List<PreparationData> _preparationData = new();

        [Header("Scene")]
        [SerializeField] CanvasGroup _hotspotLayer;
        [SerializeField] TentHotspot _firstHotspot;
        [SerializeField] GameObject _dispatchGlow;
        [SerializeField] GameObject _dimmer;
        [SerializeField] DialogueBox _dialogue;

        [Header("Panels")]
        [SerializeField] DebtsPanel _debts;
        [SerializeField] LedgerPanel _ledger;
        [SerializeField] MapPanel _map;
        [SerializeField] StubPanel _stub;

        TentPanel _openPanel;
        public State Current { get; private set; } = State.Briefing;

        [SerializeField, Tooltip("Mock campaign used when the scene is played without a CampaignDataManager (not from the main menu).")]
        TentSampleData _sample;

        [SerializeField, Tooltip("Adds every CampaignCharacter in Resources/" + CampaignCharacterFolder + " that is missing " +
                                 "from the campaign roster (mock phase, SU-56). Turn off once the campaign recruits characters.")]
        bool _fillRosterFromCampaignFolder = true;

        const string CampaignCharacterFolder = "Troop and Character Profiles/Characters/Campaign";
        const int DefaultCostToField = 25;

        public bool UsingSample { get; private set; }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Campaign data is read in Start, after every Awake, so a CampaignDataManager in this scene is ready.
        void Start()
        {
            if (!EnsureCampaign())
            {
                enabled = false;
                return;
            }

            int level = Campaign.CampaignDataManager.Data.CurrentLevel;
            var mission = Campaign.Missions.MissionCatalog.ByNumber(level);
            if (UsingSample && _sample.mission != null) Data = _sample.mission;
            else if (mission != null && mission.preparation != null) Data = mission.preparation;   // SU-119
            else if (_preparationData.Count > 0) Data = _preparationData[Mathf.Clamp(level - 1, 0, _preparationData.Count - 1)];
            else if (_sample != null && _sample.mission != null) Data = _sample.mission;
            else Debug.LogError("Tent: no PreparationData assigned on TentHubController.", this);
            BattleReport.DeployedCharacters = new();
            BattleReport.DeployedLeaders.Clear();
            BattleReport.DeployedTroops = new();
            EnsureRoster();
            // Characters joining at this mission (SU-119), then the ones it requires (Prince Julian) start deployed, free and locked (SU-117).
            TentDeployment.AddNewCharacters(Data, DefaultCostToField);
            TentDeployment.DeployMandatory(Data, DefaultCostToField);

            foreach (var panel in AllPanels())
                if (panel != null)
                {
                    panel.Initialise();
                    panel.gameObject.SetActive(false);
                }
            if (_dimmer != null) _dimmer.SetActive(false);
            if (_dispatchGlow != null) _dispatchGlow.SetActive(Data != null && Data.questAvailable);
            PlayBriefing();
        }

        // Played from the main menu, the persistent CampaignDataManager and BattleReport exist. Played directly in the
        // editor they do not: create them with the sample data instead (never saved).
        bool EnsureCampaign()
        {
            if (Campaign.CampaignDataManager.instance == null)
            {
                if (_sample == null)
                {
                    Debug.LogError("Tent: no CampaignDataManager. Start from the main menu, or assign Tent Sample Data " +
                                   "(Leveraged Wars > Pre-Battle > Create Tent Sample Data).", this);
                    return false;
                }
                var manager = new GameObject("Campaign Data (Tent sample)").AddComponent<Campaign.CampaignDataManager>();
                manager.UseSampleData(_sample.CloneCampaign());
                UsingSample = true;
                Debug.LogWarning("Tent: no CampaignDataManager in the scene, so the tent is using the sample campaign " +
                                 "(nothing is saved). Start from the main menu to use the real save.", this);
            }
            if (FindAnyObjectByType<BattleReport>() == null)
                new GameObject("Battle Report (Tent sample)").AddComponent<BattleReport>();
            return true;
        }

        IEnumerable<TentPanel> AllPanels()
        {
            yield return _debts;
            yield return _ledger;
            yield return _map;
            yield return _stub;
        }

        void Update()
        {
            switch (Current)
            {
                case State.Panel:
                    if (TentInput.CancelPressed) ClosePanel();
                    break;
                case State.Hub:
                    // Keyboard / controller: the first navigation press selects the first object (TentHub.md §3).
                    var es = EventSystem.current;
                    if (es != null && _firstHotspot != null && TentInput.NavigatePressed &&
                        (es.currentSelectedGameObject == null || !es.currentSelectedGameObject.activeInHierarchy))
                        es.SetSelectedGameObject(_firstHotspot.gameObject);
                    break;
            }
        }

        // The campaign roster on the new character system (SU-52, SU-56). An empty roster (a real save, or an old
        // sample) is filled from the sample's roster; then any CampaignCharacter in the campaign folder that is still
        // missing is added at level 1.
        void EnsureRoster()
        {
            var data = Campaign.CampaignDataManager.Data;
            data.Roster ??= new List<Campaign.RosterCharacter>();
            data.Roster.RemoveAll(r => r == null || r.character == null);

            if (data.Roster.Count == 0 && _sample != null && _sample.campaign?.Roster != null)
                foreach (var r in _sample.campaign.Roster)
                    if (r != null && r.character != null)
                        data.Roster.Add(new Campaign.RosterCharacter
                            { character = r.character, level = r.level, xp = r.xp, costToField = r.costToField });

            if (!_fillRosterFromCampaignFolder) return;
            var all = new List<CampaignCharacter>(Resources.LoadAll<CampaignCharacter>(CampaignCharacterFolder));
            all.Sort((x, y) => string.CompareOrdinal(x.CharacterName, y.CharacterName));
            int added = 0;
            foreach (var character in all)
            {
                if (data.Roster.Exists(r => r.character == character)) continue;
                data.Roster.Add(new Campaign.RosterCharacter { character = character, costToField = DefaultCostToField });
                added++;
            }
            if (added > 0)
                Debug.Log($"Tent: added {added} character(s) from Resources/{CampaignCharacterFolder} to the roster " +
                          $"({data.Roster.Count} in total).", this);
            if (data.Roster.Count == 0)
                Debug.LogWarning($"Tent: the roster is empty and no CampaignCharacter was found in Resources/{CampaignCharacterFolder}.", this);
        }

        // --- States ---

        void SetHubInput(bool on)
        {
            if (_hotspotLayer != null)
            {
                _hotspotLayer.interactable = on;
                _hotspotLayer.blocksRaycasts = on;
                if (!on)
                    foreach (var hotspot in _hotspotLayer.GetComponentsInChildren<TentHotspot>())
                        hotspot.ResetHover();
            }
            if (!on)
            {
                TentTooltip.Hide();
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            }
        }

        void EnterHub()
        {
            Current = State.Hub;
            _openPanel = null;
            if (_dimmer != null) _dimmer.SetActive(false);
            SetHubInput(true);
        }

        public void PlayBriefing()
        {
            if (_openPanel != null) ClosePanelSilently();
            Current = State.Briefing;
            SetHubInput(false);
            if (Data != null && Data.briefingFlow != null) _dialogue.Play(Data.briefingFlow, EnterHub);
            else if (Data != null) _dialogue.PlayLine(Data.briefingSpeaker, Data.briefing, null, false, EnterHub);
            else EnterHub();
        }

        public void OpenPanel(TentPanel panel)
        {
            if (panel == null) return;
            if (_openPanel != null) ClosePanelSilently();
            SetHubInput(false);
            Current = State.Panel;
            _openPanel = panel;
            if (_dimmer != null) _dimmer.SetActive(true);
            panel.Open();
            var first = panel.FirstSelectable;
            if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        public void ClosePanel()
        {
            if (_openPanel == null) return;
            var panel = _openPanel;
            ClosePanelSilently();
            EnterHub();
            // Give keyboard / controller users their place back.
            if (EventSystem.current != null && panel.Opener != null)
                EventSystem.current.SetSelectedGameObject(panel.Opener.gameObject);
        }

        void ClosePanelSilently()
        {
            _openPanel.Close();
            _openPanel = null;
            if (_dimmer != null) _dimmer.SetActive(false);
        }

        // --- Actions (TentHotspot) ---

        public void Perform(TentAction action, int courtIndex)
        {
            if (Current != State.Hub) return;
            TentHotspot opener = EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null
                ? EventSystem.current.currentSelectedGameObject.GetComponent<TentHotspot>() : null;
            switch (action)
            {
                case TentAction.Debts: Open(_debts, opener); break;
                case TentAction.Ledger: Open(_ledger, opener); break;
                case TentAction.Map: Open(_map, opener); break;
                case TentAction.Inventory:
                    _stub.Configure("Weapon Rack", "The army's arms and armour will be inspected and issued here.\n\n<i>Inventory is not built yet.</i>");
                    Open(_stub, opener);
                    break;
                case TentAction.Shop:
                    _stub.Configure("Merchant", "\"Fine steel, fair prices, my prince. Well... prices.\"\n\n<i>The equipment shop is not built yet.</i>");
                    Open(_stub, opener);
                    break;
                case TentAction.Court: TalkToCourt(courtIndex); break;
                case TentAction.Launch: TryLaunch(); break;
            }
        }

        void Open(TentPanel panel, TentHotspot opener)
        {
            if (panel == null) return;
            panel.Opener = opener;
            OpenPanel(panel);
        }

        public void OpenMapDeployment()
        {
            _map.Opener = null;
            OpenPanel(_map);
            _map.ShowDeployment();
        }

        public void TalkToCourt(int index)
        {
            var member = Data != null ? Data.Court(index) : null;
            Current = State.Dialogue;
            SetHubInput(false);
            string name = CourtName(index);
            if (member != null && member.dialogue != null) _dialogue.Play(member.dialogue, EnterHub);
            else
            {
                string line = member != null && !string.IsNullOrEmpty(member.defaultLine) ? member.defaultLine
                    : index >= 0 && index < DefaultCourtLines.Length ? DefaultCourtLines[index] : "...";
                _dialogue.PlayLine(name, line, member?.portrait, index == 2, EnterHub);
            }
        }

        // Launch if the deployment is valid. Otherwise the Tent Flaps tooltip already says why (TentHub.md §5).
        public bool TryLaunch() => LaunchRules.TryLaunch();

        // --- Court info ---

        public string CourtName(int index)
        {
            var member = Data != null ? Data.Court(index) : null;
            if (member != null && !string.IsNullOrEmpty(member.name)) return member.name;
            return index >= 0 && index < DefaultCourtNames.Length ? DefaultCourtNames[index] : "Courtier";
        }

        // Only the dispatch-bearer (index 2) carries the quest cue.
        public bool HasQuestCue(int index) => index == 2 && Data != null && Data.questAvailable;
    }
}
