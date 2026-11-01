using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BattlePrep.Tent.EditorTools
{
    // Creates the tent's sample data (mock campaign + mission) in Tent/Sample and assigns it to the Tent Hub:
    //  - briefing: a 5-line dialogue flow; court members with names, portraits and lines; the quest cue on;
    //  - loans: 3 loan offers, and 2 open repayments in the campaign;
    //  - roster: every CampaignCharacter in Characters/Campaign (8 today, so the 5-troop deploy cap can be tested),
    //    with mock level, XP and cost to field.
    // Used only when Battle Preparation is played without a CampaignDataManager. Running it again recreates the assets.
    public static class TentSampleDataCreator
    {
        public const string Folder = "Assets/Campaign/UI/Battle prep menus/Tent/Sample";
        public const string SamplePath = Folder + "/Tent Sample Data.asset";
        const string MissionPath = Folder + "/Sample Mission.asset";
        const string BriefingPath = Folder + "/Sample Briefing.asset";
        const string TacticianPath = Folder + "/Sample Tactician Talk.asset";
        const string CharacterFolder = "Assets/Resources/Troop and Character Profiles/Characters/Campaign";
        const string MissionImagePath = "Assets/Campaign/UI/Battle prep menus/00009-869237218.png";

        [MenuItem("Leveraged Wars/Pre-Battle/Create Tent Sample Data")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Campaign/UI/Battle prep menus/Tent", "Sample");
            foreach (var path in new[] { SamplePath, MissionPath, BriefingPath, TacticianPath })
                AssetDatabase.DeleteAsset(path);

            var characters = new List<CampaignCharacter>();
            foreach (var guid in AssetDatabase.FindAssets("t:CampaignCharacter", new[] { CharacterFolder }))
            {
                var c = AssetDatabase.LoadAssetAtPath<CampaignCharacter>(AssetDatabase.GUIDToAssetPath(guid));
                if (c != null) characters.Add(c);
            }
            characters.Sort((a, b) => string.CompareOrdinal(a.CharacterName, b.CharacterName));
            if (characters.Count == 0) Debug.LogWarning($"Tent sample: no CampaignCharacter assets in {CharacterFolder}.");

            CampaignCharacter Find(string nameContains)
            {
                foreach (var c in characters)
                    if (c.CharacterName.Contains(nameContains) || c.name.Contains(nameContains)) return c;
                return null;
            }
            Sprite Portrait(string nameContains) => Find(nameContains) != null ? Find(nameContains).Portrait : null;
            string NameOf(string nameContains, string fallback) => Find(nameContains) != null ? Find(nameContains).CharacterName : fallback;

            var julian = Portrait("Julian");
            var jagen = Portrait("Jagen");
            var lyra = Portrait("Lyra");
            var robin = Portrait("Robin");
            string julianName = NameOf("Julian", "Julian");
            string jagenName = NameOf("Jagen", "Jagen");
            string lyraName = NameOf("Lyra", "Lyra");
            string robinName = NameOf("Robin", "Robin");

            // --- Briefing (mock) ---
            var briefing = Flow(BriefingPath, new[]
            {
                Line(jagenName, jagen, false, "My prince, the scouts are back. A bandit warband has dug in at the river ford, right across the merchant road."),
                Line(julianName, julian, true, "The road that carries our grain, and our creditors' patience. How many?"),
                Line(jagenName, jagen, false, "Three bands of foot, a few archers in the reeds and riders on the far bank. Break their line and the rest will run."),
                Line(lyraName, lyra, false, "Mind the coffers, Highness. Borrow if you must, but every crown owed is prestige spent."),
                Line(julianName, julian, true, "Then we strike quickly. Ready the war table: no more than five companies, and we march at dawn."),
            });
            var tactician = Flow(TacticianPath, new[]
            {
                Line(lyraName, lyra, false, "The ford is narrow. Pin them on the bank and their riders cannot help."),
                Line(lyraName, lyra, false, "Archers on our left flank would cover the crossing nicely."),
            });

            // --- Mission (mock) ---
            var mission = ScriptableObject.CreateInstance<PreparationData>();
            mission.briefing = "Sample mission: a bandit warband holds the river ford on the merchant road. Break their line " +
                               "before their riders cross. Deploy between 2 and 5 troops.";
            mission.image = AssetDatabase.LoadAssetAtPath<Sprite>(MissionImagePath);
            mission.briefingFlow = briefing;
            mission.briefingSpeaker = jagenName;
            mission.questAvailable = true;
            mission.court = new[]
            {
                new CourtMember { name = lyraName, portrait = lyra, dialogue = tactician },
                new CourtMember { name = jagenName, portrait = jagen,
                    defaultLine = "The men are fed and the horses shod. Give the word and we march." },
                new CourtMember { name = robinName, portrait = robin,
                    defaultLine = "A rider brought this sealed dispatch at dawn. It bears the Santo bank's seal." },
            };
            mission.buyOrders = new List<SellOrders>
            {
                Loan("Santo family loan", 5, 120, 150, 2, "A quick loan from the Santo bank. Small, fast, repaid within two months."),
                Loan("Merchant guild bond", 10, 150, 300, 4, "The guild will fund the campaign if the road reopens. Steep interest."),
                Loan("Crown war levy", 25, 110, 500, 6, "The crown lends to commanders of standing. Needs a lot of prestige."),
            };
            AssetDatabase.CreateAsset(mission, MissionPath);

            // --- Campaign (mock) ---
            var campaign = new Campaign.CampaignData
            {
                Money = 200,
                MaxPrestige = 40,
                CurrentPrestige = 20,
                CurrentLevel = 1,
                BuyOrders = new List<BuyOrders>
                {
                    Repayment("Harvest loan", 120, 5, 1, "Last season's loan to feed the levies. Due next month."),
                    Repayment("Armourer's credit", 80, 3, 3, "Mail and helmets bought on credit before the last battle."),
                },
                Characters = new List<Campaign.PCWrapper>(),
                Roster = new List<Campaign.RosterCharacter>(),
            };
            int[] level = { 3, 1, 2, 4, 1, 2, 5, 1 };
            int[] xp = { 40, 75, 10, 55, 0, 90, 20, 60 };
            int[] cost = { 30, 20, 25, 45, 15, 35, 50, 25 };
            for (int i = 0; i < characters.Count; i++)
                campaign.Roster.Add(new Campaign.RosterCharacter
                {
                    character = characters[i],
                    level = level[i % level.Length],
                    xp = xp[i % xp.Length],
                    costToField = cost[i % cost.Length],
                });

            var sample = ScriptableObject.CreateInstance<TentSampleData>();
            sample.campaign = campaign;
            sample.mission = mission;
            AssetDatabase.CreateAsset(sample, SamplePath);
            AssetDatabase.SaveAssets();

            AssignToOpenTent(sample);
            Selection.activeObject = sample;
            Debug.Log($"Tent sample: created {SamplePath} with {campaign.Roster.Count} roster characters, " +
                      $"{mission.buyOrders.Count} loan offers and {campaign.BuyOrders.Count} open repayments.");
        }

        static void AssignToOpenTent(TentSampleData sample)
        {
            foreach (var hub in Object.FindObjectsByType<TentHubController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(hub);
                so.FindProperty("_sample").objectReferenceValue = sample;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(hub.gameObject.scene);
                EditorSceneManager.SaveScene(hub.gameObject.scene);
                Debug.Log($"Tent sample: assigned to {hub.name} in {hub.gameObject.scene.name}.", hub);
            }
        }

        static Cutscene.Dialogue Line(string speaker, Sprite portrait, bool right, string text)
        {
            var d = ScriptableObject.CreateInstance<Cutscene.Dialogue>();
            d.CharacterName = speaker;
            d.Portrait = portrait;
            d.leftOrRight = right;
            d.dialogue = text;
            return d;
        }

        // A DialogueFlow asset with its lines stored inside it.
        static Cutscene.DialogueFlow Flow(string path, Cutscene.Dialogue[] lines)
        {
            var flow = ScriptableObject.CreateInstance<Cutscene.DialogueFlow>();
            AssetDatabase.CreateAsset(flow, path);
            var so = new SerializedObject(flow);
            var list = so.FindProperty("dialogue");
            list.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i].name = $"{i + 1:00} {lines[i].CharacterName}";
                AssetDatabase.AddObjectToAsset(lines[i], flow);
                list.GetArrayElementAtIndex(i).objectReferenceValue = lines[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return flow;
        }

        static SellOrders Loan(string info, int prestige, int interest, int gold, int months, string details) => new()
        {
            orderInfo = info, PrestigeRequired = prestige, interestRate = interest, gold = gold, timeLeft = months, orderDetails = details,
        };

        static BuyOrders Repayment(string info, int cost, int prestige, int months, string details) => new()
        {
            orderInfo = info, Cost = cost, PrestigeReward = prestige, TimeLeft = months, orderDetails = details,
        };
    }
}
