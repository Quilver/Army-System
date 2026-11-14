using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cutscene;
using CutsceneEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BattlePrep.Tent.EditorTools
{
    // Creates the tent's sample data (mock campaign + mission) in Tent/Sample and assigns it to the Tent Hub:
    //  - briefing and court talk: markdown notes (Sample Briefing.md, ...) and the Dialogue Scripts built from them;
    //    court members with names and portraits; the quest cue on;
    //  - loans: 3 loan notes in Sample/Loans and the Loan Offers built from them, and 2 open repayments in the campaign;
    //  - roster: every CampaignCharacter in Characters/Campaign (8 today, so the 5-troop deploy cap can be tested),
    //    with mock level, XP and cost to field.
    // Used only when Battle Preparation is played without a CampaignDataManager. Running it again recreates the assets.
    public static class TentSampleDataCreator
    {
        public const string Folder = "Assets/Campaign/UI/Battle prep menus/Tent/Sample";
        public const string SamplePath = Folder + "/Tent Sample Data.asset";
        const string MissionPath = Folder + "/Sample Mission.asset";
        const string LoanFolder = Folder + "/Loans";
        const string CharacterFolder = "Assets/Resources/Troop and Character Profiles/Characters/Campaign";
        const string MissionImagePath = "Assets/Campaign/UI/Battle prep menus/00009-869237218.png";

        [MenuItem("Leveraged Wars/Pre-Battle/Create Tent Sample Data")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Campaign/UI/Battle prep menus/Tent", "Sample");
            foreach (var path in new[] { SamplePath, MissionPath })
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.DeleteAsset(LoanFolder);
            foreach (var name in new[] { "Sample Briefing", "Sample Tactician Talk", "Sample Captain Talk", "Sample Dispatch Talk" })
            {
                AssetDatabase.DeleteAsset($"{Folder}/{name}.asset");
                AssetDatabase.DeleteAsset($"{Folder}/{name}.md");
            }

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

            // --- Briefing and court talk (mock), written as notes like the story vault's ---
            var briefing = Scene("Sample Briefing", new[]
            {
                (jagenName, jagen, "My prince, the scouts are back. A bandit warband has dug in at the river ford, right across the merchant road."),
                (julianName, julian, "The road that carries our grain, and our creditors' patience. How many?"),
                (jagenName, jagen, "Three bands of foot, a few archers in the reeds and riders on the far bank. Break their line and the rest will run."),
                (lyraName, lyra, "Mind the coffers, Highness. Borrow if you must, but every crown owed is prestige spent."),
                (julianName, julian, "Then we strike quickly. Ready the war table: no more than five companies, and we march at dawn."),
            });
            var tactician = Scene("Sample Tactician Talk", new[]
            {
                (lyraName, lyra, "The ford is narrow. Pin them on the bank and their riders cannot help."),
                (lyraName, lyra, "Archers on our left flank would cover the crossing nicely."),
            });
            var captain = Scene("Sample Captain Talk", new[]
            {
                (jagenName, jagen, "The men are fed and the horses shod. Give the word and we march."),
            });
            var dispatch = Scene("Sample Dispatch Talk", new[]
            {
                (robinName, robin, "A rider brought this sealed dispatch at dawn. It bears the Santo bank's seal."),
            });

            // --- Mission (mock) ---
            var mission = ScriptableObject.CreateInstance<PreparationData>();
            mission.image = AssetDatabase.LoadAssetAtPath<Sprite>(MissionImagePath);
            mission.briefingFlow = briefing;
            mission.questAvailable = true;
            mission.court = new[]
            {
                new CourtMember { name = lyraName, portrait = lyra, dialogue = tactician },
                new CourtMember { name = jagenName, portrait = jagen, dialogue = captain },
                new CourtMember { name = robinName, portrait = robin, dialogue = dispatch },
            };
            mission.loans = new List<LoanOffer>
            {
                Loan("Santo family loan", 5, 150, 330, 2, "A quick loan from the Santo bank. Small, fast, repaid within two months."),
                Loan("Merchant guild bond", 10, 300, 750, 4, "The guild will fund the campaign if the road reopens. *Steep interest.*"),
                Loan("Crown war levy", 25, 500, 1050, 6, "The crown lends to commanders of standing. Needs a lot of prestige."),
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
                Roster = new List<Campaign.RosterCharacter>(),
            };
            int[] level = { 3, 1, 2, 4, 1, 2, 5, 1 };
            int[] xp = { 4, 7, 1, 5, 0, 9, 2, 6 };   // out of RosterCharacter.XpPerLevel (10, SU-115)
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
                      $"{mission.loans.Count} loan offers and {campaign.BuyOrders.Count} open repayments.");
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

        // Writes a dialogue note (one text box per line) and builds its Dialogue Script.
        static DialogueScript Scene(string name, (string speaker, Sprite portrait, string text)[] lines)
        {
            var md = new StringBuilder();
            md.Append("---\ndialogue: true\n---\n");
            md.Append("%% Made by Leveraged Wars > Pre-Battle > Create Tent Sample Data; running it again rewrites this note. %%\n");
            foreach (var (speaker, portrait, text) in lines)
                md.Append('\n').Append(Heading(speaker, portrait)).Append('\n').Append(text).Append('\n');
            return (DialogueScript)StoryAssets.CreateFor(WriteNote($"{Folder}/{name}.md", md.ToString()), typeof(DialogueScript));
        }

        // "# ![Name](<Assets/.../Portrait.png#Sprite>)": a portrait outside the vault is linked by its project path.
        static string Heading(string speaker, Sprite portrait)
        {
            if (portrait == null) return "# " + speaker;
            string path = AssetDatabase.GetAssetPath(portrait);
            bool sheet = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Count() > 1;
            return $"# ![{speaker}](<{path}{(sheet ? "#" + portrait.name : "")}>)";
        }

        // Writes a loan note (named after the loan) and builds its Loan Offer.
        static LoanOffer Loan(string title, int prestigeCost, int gold, int cost, int months, string details)
        {
            if (!AssetDatabase.IsValidFolder(LoanFolder)) AssetDatabase.CreateFolder(Folder, "Loans");
            string md = $"---\nGold: {gold}\nCost: {cost}\nTime left: {months}\nPrestige Cost: {prestigeCost}\n---\n{details}\n";
            return (LoanOffer)StoryAssets.CreateFor(WriteNote($"{LoanFolder}/{title}.md", md), typeof(LoanOffer));
        }

        static string WriteNote(string path, string text)
        {
            File.WriteAllText(path, text);
            AssetDatabase.ImportAsset(path);
            return path;
        }

        static BuyOrders Repayment(string info, int cost, int prestige, int months, string details) => new()
        {
            orderInfo = info, Cost = cost, PrestigeReward = prestige, TimeLeft = months, orderDetails = details,
        };
    }
}
