using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
namespace Campaign
{
    [System.Serializable]
    public class CampaignData
    {
        public static event System.Action<int> MoneyGained, MaxPrestigeGained, CurrentPrestigeGained;
        [SerializeField] int _money;
        public int Money
        {
            get => _money;
            set
            {
                value = math.max(0, value);
                MoneyGained?.Invoke(value-_money);
                _money = value;
            }
        }
        [SerializeField] int _maxPrestige, _currentPrestige;
        public int MaxPrestige
        {
            get=> _maxPrestige;
            set{

                value = math.max(0, value);
                MaxPrestigeGained?.Invoke(value - _maxPrestige);
                _maxPrestige = value;
            }
        } 
        public int CurrentPrestige
        {
            get => _currentPrestige;
            set
            {

                value = math.max(0, value);
                CurrentPrestigeGained?.Invoke(value - _currentPrestige);
                _currentPrestige = value;
            }
        }
        [Range(1, 10)]
        public int CurrentLevel;
        public List<BattlePrep.BuyOrders> BuyOrders;
        // The player's characters (CampaignCharacter assets plus level, XP and cost to field).
        public List<RosterCharacter> Roster = new();

        // Saves with another version are discarded (FileDataHandler). Bump when the save format changes.
        public const int CurrentSaveVersion = 2;
        public int SaveVersion = CurrentSaveVersion;

        // Restores asset references after loading from JSON.
        public void ResolveReferences()
        {
            Roster ??= new List<RosterCharacter>();
            foreach (var character in Roster) character?.ResolveCharacter();
            Roster.RemoveAll(r => r == null || r.character == null);
        }
    }
}
