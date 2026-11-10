using System.Collections.Generic;
using BattlePrep.Tent;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static BattlePrep.Tent.EditorTools.TentHubSceneBuilder;

namespace EndGameUI.EditorTools
{
    // Builds the post-battle book (SU-115) into the Post battle scene, in the style of the tent's Roster Ledger:
    //  - creates Assets/Resources/Campaign Tuning/Post Battle Tuning if it is missing;
    //  - switches off (does not delete) the old post-battle canvas, renamed "... (retired)";
    //  - creates the "Post Battle Book" canvas: the book, its left page (ledger page and report page), its right page
    //    (record page), page buttons, Continue, and the tooltip; and wires every reference.
    // Safe to run again: an existing "Post Battle Book" is replaced. Menu: Leveraged Wars > Post-Battle > Build Post-Battle Book.
    public static class PostBattleBookBuilder
    {
        const string ScenePath = "Assets/Resources/Levels/Menu levels/Post battle.unity";
        const string BookName = "Post Battle Book";
        const string RetiredSuffix = " (retired)";
        const string TuningFolder = "Assets/Resources/Campaign Tuning";

        [MenuItem("Leveraged Wars/Post-Battle/Build Post-Battle Book")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureTuningAsset();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var log = new List<string>();

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == BookName)
                {
                    Object.DestroyImmediate(root);
                    log.Add("Replaced the previous Post Battle Book.");
                }
                else if (root.GetComponentInChildren<EndGameFlow>(true) != null ||
                         root.GetComponentInChildren<AchievementManager>(true) != null ||
                         root.GetComponentInChildren<UnitManager>(true) != null)
                {
                    root.SetActive(false);
                    if (!root.name.EndsWith(RetiredSuffix)) root.name += RetiredSuffix;
                    log.Add($"Switched off the old post-battle UI '{root.name}' (delete it once the book is approved).");
                }
            }

            EnsureEventSystem(log);
            var book = BuildBook();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = book;
            log.Add("Post Battle Book built.");
            Debug.Log("Post-battle: " + string.Join("\n", log));
        }

        static void EnsureTuningAsset()
        {
            if (!AssetDatabase.IsValidFolder(TuningFolder)) AssetDatabase.CreateFolder("Assets/Resources", "Campaign Tuning");
            string path = $"{TuningFolder}/{PostBattleTuning.AssetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<PostBattleTuning>(path) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<PostBattleTuning>(), path);
            AssetDatabase.SaveAssets();
        }

        static GameObject BuildBook()
        {
            var root = new GameObject(BookName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.layer = LayerMask.NameToLayer("UI");
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            var book = root.AddComponent<PostBattleBook>();

            var backdrop = Img(root.transform, "Backdrop", new Color(0.08f, 0.06f, 0.05f, 1f));
            Stretch(backdrop.gameObject);

            BookFrame(root.transform, "Book", "After the Battle", out var cover, out var left, out var right, out var close);
            Object.DestroyImmediate(close.gameObject);

            // Left page: a character's ledger page, or the battle report on the first spread. They share the page number.
            var ledger = LedgerPageLayout(left);
            var pageNumber = left.Find("Page Number").GetComponent<TextMeshProUGUI>();
            var reportHolder = UI(left, "Report");
            Stretch(reportHolder);
            reportHolder.transform.SetSiblingIndex(ledger.transform.Find("Content").GetSiblingIndex() + 1);
            var leftRecord = RecordPageLayout((RectTransform)reportHolder.transform, pageNumber);

            // Right page: the record (figures and stamps).
            var rightRecord = RecordPageLayout(right, null);

            var previous = Btn(cover, "Previous", "< Previous", 15);
            TopLeft(previous.gameObject, 30, 604, 140, 30);
            var label = Label(cover, "Spread", "", 14, Cream, TextAlignmentOptions.Center);
            TopLeft(label.gameObject, 180, 604, 650, 30);
            var cont = Btn(cover, "Continue", "Continue", 16);
            TopLeft(cont.gameObject, 840, 604, 160, 30);
            cont.targetGraphic.color = Rust;
            var next = Btn(cover, "Next", "Next >", 15);
            TopLeft(next.gameObject, 1010, 604, 140, 30);

            BuildTooltip(root.transform);

            Set(book, "_ledgerPage", ledger);
            Set(book, "_leftRecord", leftRecord);
            Set(book, "_rightRecord", rightRecord);
            Set(book, "_previous", previous);
            Set(book, "_next", next);
            Set(book, "_continue", cont);
            Set(book, "_spreadLabel", label);
            Set(book, "_audio", audio);
            return root;
        }

        // A record page filling page: header and subtitle, then a column of rows, a divider, the stamps header and a grid
        // of stamps (548 x 556 page). Row and stamp templates stay inactive; RecordPage clones them.
        static RecordPage RecordPageLayout(RectTransform page, TextMeshProUGUI sharedPageNumber)
        {
            var content = UI(page, "Record");
            Stretch(content);
            var c = content.transform;

            var header = Label(c, "Header", "", 26, Ink, TextAlignmentOptions.MidlineLeft, true);
            TopLeft(header.gameObject, 28, 16, 492, 36);
            var subtitle = Label(c, "Subtitle", "", 14, Faded, TextAlignmentOptions.MidlineLeft);
            subtitle.fontStyle = FontStyles.Italic;
            TopLeft(subtitle.gameObject, 28, 52, 492, 22);
            Divider(c, 80);

            // Body: rows are inserted after the template, then divider, stamps header, "none" text and the grid.
            var body = UI(c, "Body");
            TopLeft(body, 28, 90, 492, 440);
            var column = body.AddComponent<VerticalLayoutGroup>();
            column.spacing = 4;
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            var row = UI(body.transform, "Row Template");
            Fixed(row, 30);
            var rowLabel = Label(row.transform, "Label", "Label", 17, Ink, TextAlignmentOptions.MidlineLeft);
            Stretch(rowLabel.gameObject, 0, 0, 160, 0);
            var rowValue = Label(row.transform, "Value", "0", 20, Ink, TextAlignmentOptions.MidlineRight, true);
            Stretch(rowValue.gameObject, 300, 0, 0, 0);
            var rowLine = Img(row.transform, "Underline", new Color(Ink.r, Ink.g, Ink.b, 0.12f));
            rowLine.raycastTarget = false;
            Anchored(rowLine.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(492, 1));
            var recordRow = row.AddComponent<RecordRow>();
            Set(recordRow, "_label", rowLabel);
            Set(recordRow, "_value", rowValue);
            row.SetActive(false);

            var gap = UI(body.transform, "Gap");
            Fixed(gap, 6);
            var divider = Img(body.transform, "Divider", new Color(Ink.r, Ink.g, Ink.b, 0.3f));
            divider.raycastTarget = false;
            Fixed(divider.gameObject, 2);
            var stampsHeader = Label(body.transform, "Stamps Header", "Achievements", 17, Ink, TextAlignmentOptions.MidlineLeft, true);
            Fixed(stampsHeader.gameObject, 28);
            var noStamps = Label(body.transform, "None", "", 15, Faded, TextAlignmentOptions.MidlineLeft);
            noStamps.fontStyle = FontStyles.Italic;
            Fixed(noStamps.gameObject, 24);
            noStamps.gameObject.SetActive(false);

            var stamps = UI(body.transform, "Stamps");
            var stampsLayout = stamps.AddComponent<LayoutElement>();
            stampsLayout.minHeight = 120;
            stampsLayout.flexibleHeight = 1;
            var grid = stamps.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(92, 118);
            grid.spacing = new Vector2(8, 6);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperLeft;
            var stampTemplate = StampTemplate(stamps.transform);

            TextMeshProUGUI number = sharedPageNumber;
            if (number == null)
            {
                number = Label(page, "Page Number", "", 13, Faded, TextAlignmentOptions.Center);
                number.fontStyle = FontStyles.Italic;
                Anchored(number.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(60, 22));
            }

            var record = content.AddComponent<RecordPage>();
            Set(record, "_content", content);
            Set(record, "_header", header);
            Set(record, "_subtitle", subtitle);
            Set(record, "_stampsHeader", stampsHeader);
            Set(record, "_noStamps", noStamps);
            Set(record, "_pageNumber", number);
            Set(record, "_rows", (RectTransform)body.transform);
            Set(record, "_rowTemplate", recordRow);
            Set(record, "_stamps", (RectTransform)stamps.transform);
            Set(record, "_stampTemplate", stampTemplate);
            Set(record, "_divider", divider.gameObject);
            content.SetActive(false);
            return record;
        }

        // An achievement stamp, 92 x 118 (the grid cell): the icon, its title and its reward, in a body that tilts and
        // scales as it is stamped. The icon names the achievement in the tooltip on hover.
        static AchievementStamp StampTemplate(Transform parent)
        {
            var root = UI(parent, "Stamp Template");
            var body = UI(root.transform, "Body");
            Stretch(body);
            var group = body.AddComponent<CanvasGroup>();
            group.blocksRaycasts = true;

            var icon = Img(body.transform, "Icon", Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = true;
            Anchored(icon.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(72, 72));
            icon.gameObject.AddComponent<IconTooltip>();

            var title = Label(body.transform, "Title", "", 12, Ink, TextAlignmentOptions.Top, true);
            title.textWrappingMode = TextWrappingModes.Normal;
            Anchored(title.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -76), new Vector2(92, 28));
            var reward = Label(body.transform, "Reward", "", 11, Faded, TextAlignmentOptions.Top);
            reward.textWrappingMode = TextWrappingModes.Normal;
            Anchored(reward.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -104), new Vector2(92, 16));

            var stamp = root.AddComponent<AchievementStamp>();
            Set(stamp, "_body", (RectTransform)body.transform);
            Set(stamp, "_group", group);
            Set(stamp, "_icon", icon);
            Set(stamp, "_title", title);
            Set(stamp, "_reward", reward);
            root.SetActive(false);
            return stamp;
        }

        // A child of a vertical layout with a fixed height.
        static void Fixed(GameObject go, float height)
        {
            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = height;
            layout.flexibleHeight = 0;
        }
    }
}
