using System.Collections.Generic;
using Campaign;
using UnityEngine;

namespace Deployment
{
    // Spawns one troop for each character deployed in the pre-battle tent (BattleReport.DeployedRoster), in a grid
    // across this object's area (its lossy scale), facing this object's up. Every character fights as its own troop
    // until pairing is built (TentHub.md PB-Q4). Bridge between campaign and battle (Tween system).
    public class DeployUnitFromBattleReport : MonoBehaviour
    {
        [SerializeField] Army armyData;
        [SerializeField, Tooltip("Team id of the spawned troops; the player team in TroopPlayerOrders.")] int _teamId;
        [SerializeField, Range(3, 40), Tooltip("Distance between troop centres.")] float unitSpacing = 20f;
        [SerializeField, Min(0.1f), Tooltip("Troop radius of each spawned troop.")] float _troopRadius = 16.5f;

        const string VisualiserPrefabPath = "Troop and Character Profiles/Troop Visualiser";

        readonly List<TroopProfile> _runtimeProfiles = new();

        void Awake()
        {
            if (!BattleReport.Exists) return;
            EvenDeployment(BattleReport.DeployedRoster);
        }

        void OnDestroy()
        {
            foreach (var profile in _runtimeProfiles)
                if (profile != null) Destroy(profile);
        }

        void EvenDeployment(IReadOnlyList<RosterCharacter> characters)
        {
            if (characters == null || characters.Count == 0) return;
            Vector2 range = transform.lossyScale;
            int files = Mathf.Max(1, Mathf.Min(characters.Count, Mathf.FloorToInt(range.x / unitSpacing) + 1));
            int rows = Mathf.CeilToInt(characters.Count / (float)files);
            for (int i = 0; i < characters.Count; i++)
            {
                int file = i % files;
                int row = i / files;
                float x = (file - (files - 1) * 0.5f) * unitSpacing;
                float y = ((rows - 1) * 0.5f - row) * unitSpacing;
                Vector3 position = transform.position + transform.right * x + transform.up * y;
                DeployTroop(characters[i], position);
            }
        }

        void DeployTroop(RosterCharacter character, Vector3 position)
        {
            if (character == null || character.character == null) return;
            var profile = TroopProfile.CreateRuntimeProfile(new CharacterProfile[] { character.character }, _troopRadius);
            profile.name = character.Name;
            _runtimeProfiles.Add(profile);

            var root = new GameObject(character.Name);
            root.transform.SetParent(armyData != null ? armyData.transform : null, false);
            root.transform.SetPositionAndRotation(position, transform.rotation);
            var visualiser = Resources.Load<TroopVisualiser>(VisualiserPrefabPath);
            if (visualiser != null) Instantiate(visualiser, root.transform).name = visualiser.name;
            var troop = root.AddComponent<Troop>();
            troop.Configure(_teamId, profile);
        }
    }
}
