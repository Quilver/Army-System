using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BattlePrep.Tent.EditorTools
{
    // Builds the pre-battle tent hub (Docs/PreBattle/TentHub.md) into the Battle Preparation scene:
    //  - sets the placeholder art in Tent/Art to readable, uncompressed sprites;
    //  - copies the PreparationData list from the old PreparationContoller and deactivates the old tab canvas;
    //  - creates the "Tent Hub" canvas (tent objects, tooltip, panels, dialogue box) and wires every reference.
    // Safe to run again: an existing "Tent Hub" is replaced. Menu: Leveraged Wars > Pre-Battle > Build Tent Hub.
    public static class TentHubSceneBuilder
    {
        const string ScenePath = "Assets/Resources/Levels/Menu levels/Battle Preparation.unity";
        const string PrepDir = "Assets/Campaign/UI/Battle prep menus/";
        const string ArtDir = PrepDir + "Tent/Art/";
        const string LayoutPath = ArtDir + "TentLayout.json";
        const string HubName = "Tent Hub";

        static readonly Color Parchment = new Color32(0xF3, 0xEA, 0xD6, 0xFF);
        static readonly Color ParchmentDark = new Color32(0xE2, 0xD3, 0xB4, 0xFF);
        static readonly Color Ink = new Color32(0x2B, 0x21, 0x18, 0xFF);
        static readonly Color Wood = new Color32(0x5C, 0x45, 0x33, 0xFF);
        static readonly Color Cream = new Color32(0xFE, 0xF3, 0xC7, 0xFF);
        static readonly Color Rust = new Color32(0x7C, 0x2D, 0x12, 0xFF);
        static readonly Color Gold = new Color32(0xD9, 0x77, 0x06, 0xFF);

        [System.Serializable] class Layer { public string name; public float x, y, w, h; }
        [System.Serializable] class Layout { public Layer[] layers; }

        // Draw order back to front (TentHub.md §2).
        static readonly string[] DrawOrder =
        {
            "WeaponRack", "Merchant", "Captain", "Tactician", "DispatchBearer", "DispatchGlow", "TentFlaps", "Debts",
            "GoldChest", "WarTable", "Ledger", "Map",
        };
        // Keyboard / controller order, left to right (TentHub.md §3).
        static readonly string[] NavigationOrder =
        {
            "GoldChest", "WeaponRack", "Merchant", "Debts", "Tactician", "Captain", "DispatchBearer", "Ledger", "Map", "TentFlaps",
        };

        [MenuItem("Leveraged Wars/Pre-Battle/Build Tent Hub")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ConfigureArt();
            var layout = LoadLayout();
            if (layout == null) return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var log = new List<string>();

            // Mission data from the old controller (or a previous build).
            var data = new List<PreparationData>();
            foreach (var old in Object.FindObjectsByType<PreparationContoller>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var list = new SerializedObject(old).FindProperty("preparationData");
                for (int i = 0; i < list.arraySize; i++)
                    data.Add(list.GetArrayElementAtIndex(i).objectReferenceValue as PreparationData);
                if (data.Count > 0) break;
            }
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != HubName) continue;
                if (data.Count == 0 && root.TryGetComponent<TentHubController>(out var previous))
                {
                    var list = new SerializedObject(previous).FindProperty("_preparationData");
                    for (int i = 0; i < list.arraySize; i++)
                        data.Add(list.GetArrayElementAtIndex(i).objectReferenceValue as PreparationData);
                }
                Object.DestroyImmediate(root);
                log.Add("Replaced the previous Tent Hub.");
            }
            if (data.Count == 0) Debug.LogWarning("Tent: no PreparationData found; assign them on Tent Hub > TentHubController.");

            // Retire the old tabbed menu (TentHub.md §8).
            var retire = new HashSet<GameObject>();
            foreach (var c in Object.FindObjectsByType<PreparationContoller>(FindObjectsInactive.Include, FindObjectsSortMode.None)) retire.Add(TopCanvasObject(c.transform));
            foreach (var c in Object.FindObjectsByType<ToBattleButton>(FindObjectsInactive.Include, FindObjectsSortMode.None)) retire.Add(TopCanvasObject(c.transform));
            foreach (var c in Object.FindObjectsByType<TopLineView>(FindObjectsInactive.Include, FindObjectsSortMode.None)) retire.Add(TopCanvasObject(c.transform));
            foreach (var go in retire)
                if (go.activeSelf)
                {
                    go.SetActive(false);
                    log.Add($"Deactivated old menu object '{go.name}' (delete it once the tent is approved).");
                }

            EnsureEventSystem(log);
            var hub = BuildHub(layout, data);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = hub;
            log.Add($"Tent Hub built with {data.Count} mission(s) of PreparationData.");
            Debug.Log("Tent: " + string.Join("\n", log));
        }

        // --- Art ---

        static void ConfigureArt()
        {
            foreach (var file in Directory.GetFiles(ArtDir, "*.png"))
            {
                string path = file.Replace('\\', '/');
                AssetDatabase.ImportAsset(path);
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;
                bool dirty = ti.textureType != TextureImporterType.Sprite || !ti.isReadable || ti.mipmapEnabled ||
                             ti.textureCompression != TextureImporterCompression.Uncompressed || ti.maxTextureSize < 4096 ||
                             ti.spriteImportMode != SpriteImportMode.Single;
                if (!dirty) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.isReadable = true;            // alpha hit testing
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.maxTextureSize = 4096;        // art is rendered at 2x (background 2560 x 1440)
                ti.SaveAndReimport();
            }
        }

        static Layout LoadLayout()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
            if (json == null)
            {
                Debug.LogError($"Tent: {LayoutPath} is missing.");
                return null;
            }
            return JsonUtility.FromJson<Layout>(json.text);
        }

        static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}Tent_{name}.png");

        static Layer Find(Layout layout, string name)
        {
            foreach (var l in layout.layers) if (l.name == name) return l;
            Debug.LogError($"Tent: layer {name} missing from {LayoutPath}.");
            return new Layer { name = name, w = 10, h = 10 };
        }

        // --- Hub ---

        static GameObject BuildHub(Layout layout, List<PreparationData> data)
        {
            var hub = new GameObject(HubName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            hub.layer = LayerMask.NameToLayer("UI");
            var canvas = hub.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = hub.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var controller = hub.AddComponent<TentHubController>();

            Stretch(Img(hub.transform, "Backdrop", new Color(0.08f, 0.06f, 0.05f, 1f)).gameObject);

            // Stage: the 1280 x 720 tent, always fully visible.
            var stage = UI(hub.transform, "Stage");
            Anchored(stage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1280, 720));
            var background = Img(stage.transform, "Background", Color.white, Art("Background"));
            Place(background.gameObject, Find(layout, "Background"));
            background.raycastTarget = false;

            var hotspotLayer = UI(stage.transform, "Hotspots");
            Stretch(hotspotLayer);
            var group = hotspotLayer.AddComponent<CanvasGroup>();

            var hotspots = new Dictionary<string, TentHotspot>();
            GameObject glow = null;
            foreach (var name in DrawOrder)
            {
                var image = Img(hotspotLayer.transform, name, Color.white, Art(name));
                image.preserveAspect = false;
                Place(image.gameObject, Find(layout, name));
                switch (name)
                {
                    case "WeaponRack": hotspots[name] = Hotspot<TentHotspot>(image, "Weapon Rack", TentAction.Inventory); break;
                    case "Merchant": hotspots[name] = Hotspot<TentHotspot>(image, "Merchant", TentAction.Shop); break;
                    case "Tactician": hotspots[name] = Hotspot<CourtHotspot>(image, "Tactician", TentAction.Court, 0); break;
                    case "Captain": hotspots[name] = Hotspot<CourtHotspot>(image, "Captain", TentAction.Court, 1); break;
                    case "DispatchBearer": hotspots[name] = Hotspot<CourtHotspot>(image, "Dispatch-bearer", TentAction.Court, 2); break;
                    case "TentFlaps": hotspots[name] = Hotspot<TentFlapsHotspot>(image, "Tent Flaps", TentAction.Launch); break;
                    case "Debts": hotspots[name] = Hotspot<DebtsHotspot>(image, "War Debts", TentAction.Debts); break;
                    case "GoldChest": hotspots[name] = Hotspot<GoldChestHotspot>(image, "Treasury", TentAction.None); break;
                    case "Ledger": hotspots[name] = Hotspot<TentHotspot>(image, "Roster Ledger", TentAction.Ledger); break;
                    case "Map": hotspots[name] = Hotspot<TentHotspot>(image, "Tactical Map", TentAction.Map); break;
                    case "DispatchGlow":
                        image.raycastTarget = false;
                        image.gameObject.AddComponent<TentPulse>();
                        glow = image.gameObject;
                        break;
                    case "WarTable":
                        image.gameObject.AddComponent<TentAlphaHitTest>();  // blocks clicks to people behind the table
                        break;
                }
            }
            for (int i = 0; i < NavigationOrder.Length; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (i > 0) nav.selectOnLeft = hotspots[NavigationOrder[i - 1]];
                if (i < NavigationOrder.Length - 1) nav.selectOnRight = hotspots[NavigationOrder[i + 1]];
                hotspots[NavigationOrder[i]].navigation = nav;
            }

            var dimmer = Img(hub.transform, "Dimmer", new Color(0f, 0f, 0f, 0.55f));
            Stretch(dimmer.gameObject);

            var panels = UI(hub.transform, "Panels");
            Stretch(panels);
            var debts = BuildDebts(panels.transform);
            var ledger = BuildLedger(panels.transform);
            var map = BuildMap(panels.transform);
            var stub = BuildStub(panels.transform);

            var dialogue = BuildDialogue(hub.transform);
            BuildTooltip(hub.transform);

            var so = new SerializedObject(controller);
            var listProp = so.FindProperty("_preparationData");
            listProp.arraySize = data.Count;
            for (int i = 0; i < data.Count; i++) listProp.GetArrayElementAtIndex(i).objectReferenceValue = data[i];
            so.FindProperty("_hotspotLayer").objectReferenceValue = group;
            so.FindProperty("_firstHotspot").objectReferenceValue = hotspots[NavigationOrder[0]];
            so.FindProperty("_dispatchGlow").objectReferenceValue = glow;
            so.FindProperty("_dimmer").objectReferenceValue = dimmer.gameObject;
            so.FindProperty("_dialogue").objectReferenceValue = dialogue;
            so.FindProperty("_debts").objectReferenceValue = debts;
            so.FindProperty("_ledger").objectReferenceValue = ledger;
            so.FindProperty("_map").objectReferenceValue = map;
            so.FindProperty("_stub").objectReferenceValue = stub;
            so.FindProperty("_sample").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TentSampleData>(TentSampleDataCreator.SamplePath);
            so.ApplyModifiedPropertiesWithoutUndo();

            dimmer.gameObject.SetActive(false);
            foreach (var p in new TentPanel[] { debts, ledger, map, stub }) p.gameObject.SetActive(false);
            return hub;
        }

        static T Hotspot<T>(Image image, string displayName, TentAction action, int courtIndex = 0) where T : TentHotspot
        {
            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.82f, 0.3f, 0.95f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);
            outline.enabled = false;
            var hotspot = image.gameObject.AddComponent<T>();
            hotspot.targetGraphic = image;
            hotspot.transition = Selectable.Transition.None;
            hotspot.EditorSetup(displayName, action, courtIndex, outline);
            EditorUtility.SetDirty(hotspot);
            return hotspot;
        }

        // --- Panels ---

        static readonly Color Leather = new Color32(0x4A, 0x2C, 0x17, 0xFF);
        static readonly Color CardStock = new Color32(0xFB, 0xF6, 0xE9, 0xFF);
        static readonly Color Faded = new Color32(0x6B, 0x5A, 0x45, 0xFF);
        static readonly Color Green = new Color32(0x3F, 0x62, 0x12, 0xFF);

        static T Panel<T>(Transform parent, string name, string title, Vector2 size, out RectTransform content,
            out TextMeshProUGUI titleText) where T : TentPanel
        {
            var root = UI(parent, name);
            Stretch(root);
            var window = Img(root.transform, "Window", Parchment);
            Anchored(window.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var border = window.gameObject.AddComponent<Outline>();
            border.effectColor = Ink;
            border.effectDistance = new Vector2(3, -3);

            titleText = Label(window.transform, "Title", title, 30, Ink, TextAlignmentOptions.MidlineLeft, true);
            Stretch(titleText.gameObject, 24, 10, 80, size.y - 60);
            var close = Btn(window.transform, "Close", "X", 22);
            Anchored(close.gameObject, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -12), new Vector2(44, 44));

            var c = UI(window.transform, "Content");
            content = Stretch(c, 24, 70, 24, 24);

            var panel = root.AddComponent<T>();
            Set(panel, "_closeButton", close);
            return panel;
        }

        // An open book, 1180 x 640: leather cover, two 548 x 556 parchment pages and a spine. The strip under the pages
        // (y 604-634 on the cover) is for page controls.
        static T Book<T>(Transform parent, string name, string title, out RectTransform cover, out RectTransform left,
            out RectTransform right) where T : TentPanel
        {
            var root = UI(parent, name);
            Stretch(root);
            var coverImage = Img(root.transform, "Cover", Leather);
            cover = Anchored(coverImage.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 640));
            var edge = coverImage.gameObject.AddComponent<Outline>();
            edge.effectColor = Ink;
            edge.effectDistance = new Vector2(3, -3);

            var titleText = Label(cover, "Title", title, 22, Cream, TextAlignmentOptions.Center, true);
            Anchored(titleText.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(600, 34));
            var close = Btn(cover, "Close", "X", 20);
            Anchored(close.gameObject, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -6), new Vector2(36, 36));

            var leftImage = Img(cover, "Left Page", Parchment);
            left = TopLeft(leftImage.gameObject, 30, 46, 548, 556);
            var rightImage = Img(cover, "Right Page", Parchment);
            right = TopLeft(rightImage.gameObject, 602, 46, 548, 556);
            var spine = Img(cover, "Spine", new Color(0.17f, 0.1f, 0.05f, 0.45f));
            spine.raycastTarget = false;
            TopLeft(spine.gameObject, 576, 46, 28, 556);
            var ribbon = Img(cover, "Bookmark", Rust);
            ribbon.raycastTarget = false;
            TopLeft(ribbon.gameObject, 1060, 40, 18, 590);

            var panel = root.AddComponent<T>();
            Set(panel, "_closeButton", close);
            return panel;
        }

        static void PageHeader(RectTransform page, string title, string subtitle)
        {
            var header = Label(page, "Header", title, 26, Ink, TextAlignmentOptions.MidlineLeft, true);
            TopLeft(header.gameObject, 28, 16, 492, 36);
            var sub = Label(page, "Subtitle", subtitle, 13, Faded, TextAlignmentOptions.MidlineLeft);
            sub.fontStyle = FontStyles.Italic;
            TopLeft(sub.gameObject, 28, 52, 492, 20);
        }

        static DebtsPanel BuildDebts(Transform parent)
        {
            var panel = Book<DebtsPanel>(parent, "Debts Panel", "War Debts", out var cover, out var left, out var right);

            PageHeader(left, "Loan Offers", "Pinned up for this mission. Click a card to take the loan.");
            var loanArea = UI(left, "Loan Area");
            TopLeft(loanArea, 4, 78, 540, 470);
            var loanList = ScrollList(loanArea.transform, "Loans", 16, 18, transparent: true);
            var loanEmpty = Label(loanArea.transform, "Empty", "No lender has come forward for this mission.", 16, Faded, TextAlignmentOptions.Center);
            loanEmpty.fontStyle = FontStyles.Italic;
            Stretch(loanEmpty.gameObject);

            PageHeader(right, "Debts Owed", "Repay before they fall due. Click a card to repay it.");
            var repayArea = UI(right, "Repay Area");
            TopLeft(repayArea, 4, 78, 540, 470);
            var repayList = ScrollList(repayArea.transform, "Repayments", 16, 18, transparent: true);
            var repayEmpty = Label(repayArea.transform, "Empty", "No debts owed. The creditors are quiet, for now.", 16, Faded, TextAlignmentOptions.Center);
            repayEmpty.fontStyle = FontStyles.Italic;
            Stretch(repayEmpty.gameObject);

            var treasury = Label(cover, "Treasury", "", 16, Cream, TextAlignmentOptions.Center);
            TopLeft(treasury.gameObject, 30, 604, 1120, 30);

            var loanTemplate = PinnedCard<LoanCard>(loanList, "Loan Card Template", "TAKEN", Rust);
            var repayTemplate = PinnedCard<RepaymentCard>(repayList, "Repayment Card Template", "REPAID", Green);

            Set(panel, "_loanTemplate", loanTemplate);
            Set(panel, "_repaymentTemplate", repayTemplate);
            Set(panel, "_loanList", loanList);
            Set(panel, "_repayList", repayList);
            Set(panel, "_loanEmpty", loanEmpty);
            Set(panel, "_repayEmpty", repayEmpty);
            Set(panel, "_treasury", treasury);
            return panel;
        }

        // A piece of card stuck to the page with a strip of tape (loan offers and debts). 480 x 112.
        static T PinnedCard<T>(Transform parent, string name, string stampText, Color stampColour) where T : MonoBehaviour
        {
            var card = Img(parent, name, CardStock);
            ((RectTransform)card.transform).sizeDelta = new Vector2(480, 112);
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            shadow.effectDistance = new Vector2(2, -3);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            var group = card.gameObject.AddComponent<CanvasGroup>();

            var title = Label(card.transform, "Title", "Title", 18, Ink, TextAlignmentOptions.MidlineLeft, true);
            TopLeft(title.gameObject, 16, 10, 448, 26);
            var terms = Label(card.transform, "Terms", "", 14, Ink, TextAlignmentOptions.MidlineLeft);
            TopLeft(terms.gameObject, 16, 36, 448, 22);
            var status = Label(card.transform, "Status", "", 13, new Color(0.25f, 0.23f, 0.18f), TextAlignmentOptions.MidlineLeft);
            TopLeft(status.gameObject, 16, 58, 448, 20);
            var details = Label(card.transform, "Details", "", 12, Faded, TextAlignmentOptions.TopLeft);
            details.fontStyle = FontStyles.Italic;
            details.overflowMode = TextOverflowModes.Ellipsis;
            TopLeft(details.gameObject, 16, 80, 448, 28);

            var stamp = Label(card.transform, "Stamp", stampText, 34, new Color(stampColour.r, stampColour.g, stampColour.b, 0.8f), TextAlignmentOptions.Center, true);
            Anchored(stamp.gameObject, new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-90, -4), new Vector2(180, 50));
            stamp.transform.localRotation = Quaternion.Euler(0, 0, 14);
            stamp.gameObject.SetActive(false);

            var tape = Img(card.transform, "Tape", new Color(0.97f, 0.93f, 0.76f, 0.78f));
            tape.raycastTarget = false;
            Anchored(tape.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(96, 24));
            tape.transform.localRotation = Quaternion.Euler(0, 0, -4);

            var component = card.gameObject.AddComponent<T>();
            Set(component, "_button", button);
            Set(component, "_group", group);
            Set(component, "_title", title);
            Set(component, "_terms", terms);
            Set(component, "_status", status);
            Set(component, "_details", details);
            Set(component, "_stamp", stamp.gameObject);
            return component;
        }

        static LedgerPanel BuildLedger(Transform parent)
        {
            var panel = Book<LedgerPanel>(parent, "Ledger Panel", "Roster Ledger", out var cover, out var left, out var right);
            var leftPage = LedgerPageLayout(left);
            var rightPage = LedgerPageLayout(right);

            var previous = Btn(cover, "Previous", "< Previous", 15);
            TopLeft(previous.gameObject, 30, 604, 140, 30);
            var next = Btn(cover, "Next", "Next >", 15);
            TopLeft(next.gameObject, 1010, 604, 140, 30);
            var label = Label(cover, "Spread", "", 14, Cream, TextAlignmentOptions.Center);
            TopLeft(label.gameObject, 300, 604, 580, 30);

            Set(panel, "_leftPage", leftPage);
            Set(panel, "_rightPage", rightPage);
            Set(panel, "_previous", previous);
            Set(panel, "_next", next);
            Set(panel, "_spreadLabel", label);
            return panel;
        }

        // One character per page (548 x 556).
        static LedgerPage LedgerPageLayout(RectTransform page)
        {
            var content = UI(page, "Content");
            Stretch(content);
            var c = content.transform;

            var frame = Img(c, "Portrait Frame", ParchmentDark);
            frame.raycastTarget = false;
            TopLeft(frame.gameObject, 24, 24, 158, 158);
            var portrait = Img(c, "Portrait", Color.white);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            TopLeft(portrait.gameObject, 28, 28, 150, 150);

            var name = Label(c, "Name", "Name", 28, Ink, TextAlignmentOptions.MidlineLeft, true);
            TopLeft(name.gameObject, 196, 30, 324, 40);
            var cls = Label(c, "Class", "Class", 18, Faded, TextAlignmentOptions.MidlineLeft);
            cls.fontStyle = FontStyles.Italic;
            TopLeft(cls.gameObject, 196, 70, 324, 28);
            var level = Label(c, "Level", "", 15, Ink, TextAlignmentOptions.MidlineLeft);
            TopLeft(level.gameObject, 196, 104, 324, 24);
            var xpBack = Img(c, "XP Bar", new Color(Ink.r, Ink.g, Ink.b, 0.15f));
            xpBack.raycastTarget = false;
            TopLeft(xpBack.gameObject, 196, 134, 300, 12);
            var xpFill = Img(xpBack.transform, "Fill", Gold);
            xpFill.raycastTarget = false;
            Stretch(xpFill.gameObject);

            Divider(c, 196);
            var statsHeader = Label(c, "Stats Header", "Soldier stats   <size=80%><color=#8a7a62>(value / class cap)</color></size>", 15, Ink, TextAlignmentOptions.MidlineLeft, true);
            TopLeft(statsHeader.gameObject, 28, 204, 492, 24);
            var statsLeft = Label(c, "Stats Left", "", 16, Ink, TextAlignmentOptions.TopLeft);
            TopLeft(statsLeft.gameObject, 28, 232, 230, 132);
            var statsRight = Label(c, "Stats Right", "", 16, Ink, TextAlignmentOptions.TopLeft);
            TopLeft(statsRight.gameObject, 290, 232, 230, 132);
            Divider(c, 370);
            var skills = Label(c, "Skills", "", 15, Ink, TextAlignmentOptions.MidlineLeft);
            TopLeft(skills.gameObject, 28, 378, 492, 26);
            var equipment = Label(c, "Equipment", "", 14, Ink, TextAlignmentOptions.TopLeft);
            TopLeft(equipment.gameObject, 28, 410, 492, 80);
            var footer = Label(c, "Footer", "", 15, Ink, TextAlignmentOptions.MidlineLeft);
            TopLeft(footer.gameObject, 28, 496, 492, 26);

            var empty = Label(page, "Empty", "This page is blank.", 16, Faded, TextAlignmentOptions.Center);
            empty.fontStyle = FontStyles.Italic;
            Stretch(empty.gameObject);
            var number = Label(page, "Page Number", "1", 13, Faded, TextAlignmentOptions.Center);
            number.fontStyle = FontStyles.Italic;
            Anchored(number.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(60, 22));

            var component = page.gameObject.AddComponent<LedgerPage>();
            Set(component, "_content", content);
            Set(component, "_empty", empty);
            Set(component, "_pageNumber", number);
            Set(component, "_portrait", portrait);
            Set(component, "_name", name);
            Set(component, "_class", cls);
            Set(component, "_level", level);
            Set(component, "_xpFill", (RectTransform)xpFill.transform);
            Set(component, "_statsLeft", statsLeft);
            Set(component, "_statsRight", statsRight);
            Set(component, "_skills", skills);
            Set(component, "_equipment", equipment);
            Set(component, "_footer", footer);
            return component;
        }

        static void Divider(Transform parent, float y)
        {
            var line = Img(parent, "Divider", new Color(Ink.r, Ink.g, Ink.b, 0.3f));
            line.raycastTarget = false;
            TopLeft(line.gameObject, 28, y, 492, 2);
        }

        static MapPanel BuildMap(Transform parent)
        {
            var panel = Panel<MapPanel>(parent, "Map Panel", "Tactical Map", new Vector2(1180, 640), out var content, out _);
            var missionTab = Btn(content, "Mission Tab", "Mission", 18);
            TopLeft(missionTab.gameObject, 0, 0, 200, 40);
            var deploymentTab = Btn(content, "Deployment Tab", "Deployment", 18);
            TopLeft(deploymentTab.gameObject, 210, 0, 200, 40);

            // Mission page (1132 x 496)
            var mission = UI(content, "Mission Page");
            Stretch(mission, 0, 50, 0, 0);
            var title = Label(mission.transform, "Mission Title", "Mission", 26, Ink, TextAlignmentOptions.MidlineLeft, true);
            TopLeft(title.gameObject, 0, 0, 600, 40);
            var image = Img(mission.transform, "Mission Image", Color.white);
            image.preserveAspect = true;
            image.raycastTarget = false;
            TopLeft(image.gameObject, 0, 50, 440, 330);
            var briefing = Label(mission.transform, "Briefing", "", 18, Ink, TextAlignmentOptions.TopLeft);
            briefing.enableAutoSizing = true;
            briefing.fontSizeMin = 12;
            briefing.fontSizeMax = 20;
            TopLeft(briefing.gameObject, 460, 50, 672, 400);
            var replay = Btn(mission.transform, "Replay Briefing", "Replay Briefing", 18);
            TopLeft(replay.gameObject, 0, 440, 220, 44);

            // Deployment page (1132 x 496)
            var deployment = UI(content, "Deployment Page");
            Stretch(deployment, 0, 50, 0, 0);
            var d = deployment.transform;
            var rosterLabel = Label(d, "Roster Label", "Roster: click to deploy (costs gold, 2 to 5 troops)", 17, Ink, TextAlignmentOptions.MidlineLeft, true);
            TopLeft(rosterLabel.gameObject, 0, 0, 620, 28);
            var rosterArea = UI(d, "Roster Area");
            TopLeft(rosterArea, 0, 32, 620, 464);
            var rosterList = ScrollList(rosterArea.transform, "Roster", 10, 6);
            var rosterTemplate = RosterCardTemplate(rosterList);

            var deployedHeader = Label(d, "Deployed Header", "Deployed troops (0/5)", 17, Ink, TextAlignmentOptions.MidlineLeft, true);
            TopLeft(deployedHeader.gameObject, 640, 0, 492, 28);
            var deployedArea = UI(d, "Deployed Area");
            TopLeft(deployedArea, 640, 32, 492, 324);
            var deployedList = ScrollList(deployedArea.transform, "Deployed", 10, 6);
            var deployedTemplate = DeployedCardTemplate(deployedList);

            var goldBox = Img(d, "Gold Box", ParchmentDark);
            TopLeft(goldBox.gameObject, 640, 364, 492, 40);
            var goldBorder = goldBox.gameObject.AddComponent<Outline>();
            goldBorder.effectColor = Gold;
            goldBorder.effectDistance = new Vector2(1.5f, -1.5f);
            var gold = Label(goldBox.transform, "Gold", "", 16, Ink, TextAlignmentOptions.MidlineLeft);
            Stretch(gold.gameObject, 14, 0, 14, 0);

            var readiness = Label(d, "Readiness", "", 15, Ink, TextAlignmentOptions.MidlineLeft, true);
            TopLeft(readiness.gameObject, 640, 452, 262, 44);
            var march = Btn(d, "March Out", "March Out", 22);
            TopLeft(march.gameObject, 912, 452, 220, 44);
            march.targetGraphic.color = Rust;

            Set(panel, "_missionTab", missionTab);
            Set(panel, "_deploymentTab", deploymentTab);
            Set(panel, "_missionPage", mission);
            Set(panel, "_deploymentPage", deployment);
            Set(panel, "_missionTitle", title);
            Set(panel, "_briefingText", briefing);
            Set(panel, "_missionImage", image);
            Set(panel, "_replayBriefing", replay);
            Set(panel, "_rosterCardTemplate", rosterTemplate);
            Set(panel, "_rosterList", rosterList);
            Set(panel, "_deployedList", deployedList);
            Set(panel, "_deployedCardTemplate", deployedTemplate);
            Set(panel, "_deployedHeader", deployedHeader);
            Set(panel, "_gold", gold);
            Set(panel, "_readiness", readiness);
            Set(panel, "_marchOut", march);
            return panel;
        }

        // 600 x 68: portrait, name and class, level / soldiers / speed / morale, cost, deploy state.
        static RosterCard RosterCardTemplate(Transform parent)
        {
            var card = Img(parent, "Roster Card Template", ParchmentDark);
            ((RectTransform)card.transform).sizeDelta = new Vector2(600, 68);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            var portrait = Img(card.transform, "Portrait", Color.white);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            TopLeft(portrait.gameObject, 8, 6, 56, 56);
            var name = Label(card.transform, "Name", "Name", 18, Ink, TextAlignmentOptions.MidlineLeft, true);
            TopLeft(name.gameObject, 74, 6, 400, 28);
            var detail = Label(card.transform, "Detail", "", 13, Faded, TextAlignmentOptions.MidlineLeft);
            TopLeft(detail.gameObject, 74, 36, 380, 24);
            var cost = Label(card.transform, "Cost", "0 g", 18, Rust, TextAlignmentOptions.MidlineRight, true);
            TopLeft(cost.gameObject, 480, 6, 110, 28);
            var state = Label(card.transform, "State", "", 12, Faded, TextAlignmentOptions.MidlineRight, true);
            TopLeft(state.gameObject, 420, 36, 170, 24);

            var component = card.gameObject.AddComponent<RosterCard>();
            Set(component, "_button", button);
            Set(component, "_background", card);
            Set(component, "_portrait", portrait);
            Set(component, "_name", name);
            Set(component, "_detail", detail);
            Set(component, "_cost", cost);
            Set(component, "_state", state);
            return component;
        }

        // 470 x 56: portrait, name and class, gold spent, empty pairing slot.
        static DeployedTroopCard DeployedCardTemplate(Transform parent)
        {
            var card = Img(parent, "Deployed Card Template", Parchment);
            ((RectTransform)card.transform).sizeDelta = new Vector2(470, 56);
            var portrait = Img(card.transform, "Portrait", Color.white);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            TopLeft(portrait.gameObject, 6, 4, 48, 48);
            var name = Label(card.transform, "Name", "Character", 15, Ink, TextAlignmentOptions.MidlineLeft, true);
            TopLeft(name.gameObject, 62, 2, 230, 52);
            var cost = Label(card.transform, "Cost", "-0 g", 16, Rust, TextAlignmentOptions.MidlineRight, true);
            TopLeft(cost.gameObject, 296, 2, 80, 52);
            var pair = Btn(card.transform, "Pair Slot", "+ Pair\n(soon)", 11);
            TopLeft(pair.gameObject, 388, 8, 74, 40);
            pair.interactable = false;
            var component = card.gameObject.AddComponent<DeployedTroopCard>();
            Set(component, "_portrait", portrait);
            Set(component, "_name", name);
            Set(component, "_cost", cost);
            Set(component, "_pairSlot", pair);
            return component;
        }

        static StubPanel BuildStub(Transform parent)
        {
            var panel = Panel<StubPanel>(parent, "Stub Panel", "Coming Soon", new Vector2(640, 360), out var content, out var title);
            var body = Label(content, "Body", "", 20, Ink, TextAlignmentOptions.Center);
            Stretch(body.gameObject);
            Set(panel, "_title", title);
            Set(panel, "_body", body);
            return panel;
        }

        // --- Dialogue & tooltip ---

        static DialogueBox BuildDialogue(Transform parent)
        {
            var holder = UI(parent, "Dialogue");
            Stretch(holder);
            var root = Img(holder.transform, "Root", new Color(0f, 0f, 0f, 0.35f));
            Stretch(root.gameObject);

            var left = Img(root.transform, "Left Portrait", Color.white);
            left.preserveAspect = true;
            left.raycastTarget = false;
            Anchored(left.gameObject, new Vector2(0, 0), new Vector2(0, 0), new Vector2(80, 200), new Vector2(300, 300));
            var right = Img(root.transform, "Right Portrait", Color.white);
            right.preserveAspect = true;
            right.raycastTarget = false;
            Anchored(right.gameObject, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-80, 200), new Vector2(300, 300));

            var box = Img(root.transform, "Box", new Color(0.12f, 0.09f, 0.07f, 0.94f));
            Anchored(box.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(1160, 170));
            var boxBorder = box.gameObject.AddComponent<Outline>();
            boxBorder.effectColor = Gold;
            boxBorder.effectDistance = new Vector2(2, -2);

            var plate = Img(box.transform, "Name Plate", Rust);
            Anchored(plate.gameObject, new Vector2(0, 1), new Vector2(0, 0), new Vector2(24, -8), new Vector2(280, 44));
            var speaker = Label(plate.transform, "Speaker", "", 22, Cream, TextAlignmentOptions.Center, true);
            Stretch(speaker.gameObject);

            var text = Label(box.transform, "Text", "", 24, Cream, TextAlignmentOptions.TopLeft);
            Stretch(text.gameObject, 28, 26, 28, 30);
            var hint = Label(box.transform, "Hint", "Click to continue  ·  Esc to skip", 14, new Color(Cream.r, Cream.g, Cream.b, 0.6f), TextAlignmentOptions.MidlineRight);
            Anchored(hint.gameObject, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 6), new Vector2(420, 22));

            var dialogue = holder.AddComponent<DialogueBox>();
            Set(dialogue, "_root", root.gameObject);
            Set(dialogue, "_speakerName", speaker);
            Set(dialogue, "_namePlate", plate.gameObject);
            Set(dialogue, "_text", text);
            Set(dialogue, "_leftPortrait", left);
            Set(dialogue, "_rightPortrait", right);
            root.gameObject.SetActive(false);
            return dialogue;
        }

        static void BuildTooltip(Transform parent)
        {
            var holder = UI(parent, "Tooltip");
            Stretch(holder);
            var window = Img(holder.transform, "Window", new Color(0.1f, 0.08f, 0.06f, 0.92f));
            window.raycastTarget = false;
            Anchored(window.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0, 1), Vector2.zero, new Vector2(160, 40));
            var layout = window.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = window.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var border = window.gameObject.AddComponent<Outline>();
            border.effectColor = Gold;
            border.effectDistance = new Vector2(1.5f, -1.5f);
            var text = Label(window.transform, "Text", "", 18, Cream, TextAlignmentOptions.TopLeft);
            var tooltip = holder.AddComponent<TentTooltip>();
            Set(tooltip, "_window", (RectTransform)window.transform);
            Set(tooltip, "_text", text);
        }

        // --- Helpers ---

        static void EnsureEventSystem(List<string> log)
        {
            if (Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length > 0) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            log.Add("Added an EventSystem (the old one was inside the retired menu).");
        }

        // The top-most Canvas object above t, or t's own object when it is not under a canvas.
        static GameObject TopCanvasObject(Transform t)
        {
            GameObject top = t.gameObject;
            for (var p = t; p != null; p = p.parent)
                if (p.GetComponent<Canvas>() != null) top = p.gameObject;
            return top;
        }

        static GameObject Prefab(string file)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrepDir + file);
            if (prefab == null) Debug.LogError($"Tent: prefab {PrepDir + file} not found.");
            return prefab;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"Tent: {target.GetType().Name} has no field {field}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject UI(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        static Image Img(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var image = UI(parent, name).AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            return image;
        }

        static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions alignment, bool bold = false)
        {
            var t = UI(parent, name).AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = alignment;
            if (bold) t.fontStyle = FontStyles.Bold;
            t.raycastTarget = false;
            return t;
        }

        static Button Btn(Transform parent, string name, string label, float size)
        {
            var image = Img(parent, name, Wood);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Label(image.transform, "Label", label, size, Cream, TextAlignmentOptions.Center, true);
            Stretch(text.gameObject, 4, 2, 4, 2);
            return button;
        }

        // A vertical scrolling list filling parent; returns the content transform cards are added to.
        static Transform ScrollList(Transform parent, string name, int padding = 10, float spacing = 8, bool transparent = false)
        {
            var area = Img(parent, name, new Color(Ink.r, Ink.g, Ink.b, transparent ? 0f : 0.06f));
            Stretch(area.gameObject);
            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var viewport = UI(area.transform, "Viewport");
            Stretch(viewport);
            viewport.AddComponent<RectMask2D>();

            var content = UI(viewport.transform, "Content");
            var rt = (RectTransform)content.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = rt;
            return content.transform;
        }

        static RectTransform Stretch(GameObject go, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        static RectTransform Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0) =>
            Stretch(rt.gameObject, left, top, right, bottom);

        static RectTransform Anchored(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        // x, y from the parent's top-left corner, y down.
        static RectTransform TopLeft(GameObject go, float x, float y, float w, float h) =>
            Anchored(go, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h));

        // A layer from TentLayout.json (1280 x 720, top-left origin), pivot at its centre so the hover scale grows in place.
        static void Place(GameObject go, Layer l) =>
            Anchored(go, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(l.x + l.w / 2f, -(l.y + l.h / 2f)), new Vector2(l.w, l.h));
    }
}
