using System.Collections.Generic;
using Campaign;
using UnityEngine;

namespace Deployment
{
    // Turns the characters deployed in battle preparation into the troops the battle deploys (Tween system), and hands
    // BattleReport.DeployedTroops to the battle's deployment zone.
    // A character's troop is the troop asset in the campaign characters folder made of that character alone (for
    // example "Franz's Troop"); a character without one gets a troop made at runtime with DefaultTroopRadius.
    public static class RosterTroops
    {
        public const float DefaultTroopRadius = 5f;

        static readonly Dictionary<CharacterProfile, TroopProfile> RuntimeTroops = new();
        static TroopProfile[] _assets;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            DeploymentZone.TroopSource = () => BattleReport.Exists ? BattleReport.DeployedTroops : null;
        }

        // Rebuilds BattleReport.DeployedTroops from BattleReport.DeployedCharacters.
        public static void Sync()
        {
            if (!BattleReport.Exists) return;
            var troops = new List<TroopProfile>();
            foreach (RosterCharacter character in BattleReport.DeployedCharacters)
            {
                TroopProfile troop = For(character);
                if (troop != null) troops.Add(troop);
            }
            BattleReport.DeployedTroops = troops;
        }

        // The troop a roster character fights as, or null without a character.
        public static TroopProfile For(RosterCharacter character)
        {
            CharacterProfile profile = character != null ? character.character : null;
            if (profile == null) return null;

            _assets ??= Resources.LoadAll<TroopProfile>(RosterCharacter.CharacterFolder);
            foreach (TroopProfile asset in _assets)
                if (asset != null && asset.Characters.Count == 1 && asset.Characters[0] == profile) return asset;

            if (!RuntimeTroops.TryGetValue(profile, out TroopProfile runtime) || runtime == null)
            {
                runtime = TroopProfile.CreateRuntimeProfile(new[] { profile }, DefaultTroopRadius);
                runtime.name = character.Name;
                runtime.hideFlags = HideFlags.DontUnloadUnusedAsset;
                RuntimeTroops[profile] = runtime;
            }
            return runtime;
        }
    }
}
