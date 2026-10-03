using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EndGameUI
{
    public class UnitCard : MonoBehaviour
    {
        [SerializeField]
        Image portrait;
        [SerializeField]
        RectTransform levelBar, XPBar;
        [SerializeField]
        TextMeshProUGUI unitName, stats, levelUps;
        [SerializeField] FloatText textFloater;

        Campaign.PCWrapper unit;
        [SerializeField]
        AudioSource LevelUp, GainStat, xpRising; 
        public void Set(Campaign.PCWrapper unit)
        {
            this.unit = unit;
            BattleReport.CharacterUpdate += CharacterUpdate;
            portrait.sprite = unit.statBase.Portrait;
            unitName.text = unit.statBase.UnitName;
            stats.text = unit.statBase.StatString();
            levelUps.text = unit.LevelBonuses();
            InitTrackers();
        }
        void InitTrackers()
        {
            StartCoroutine(TrackXP());
            totalOfStatsGained = new();
            _statsGained = new();
            unit.LevelledUp += GainedLevel;
            unit.GainedStatBonus += GainedStat;
            StartCoroutine(TrackLevelBonuses());
        }
        
        int _lazyXP;
        [SerializeField, Range(0.2f, 1)] float _minTrackerUpdateSpeed = 0.3f;
        [SerializeField, Range(1, 10)] float _maxTrackerCompletionTime = 5;
        
        IEnumerator TrackXP()
        {   
            _lazyXP = unit.CurrentXP;
            float width = unit.PercentToNextLevel(_lazyXP) * levelBar.sizeDelta.x;
            XPBar.sizeDelta = new Vector2(width, levelBar.sizeDelta.y);
            yield return new WaitForSecondsRealtime(Random.Range(0.05f, 0.2f));
            while (enabled)
            {
                if (unit.CurrentXP > _lazyXP)
                {
                    _lazyXP++;
                    
                    width = unit.PercentToNextLevel(_lazyXP) * levelBar.sizeDelta.x;
                    XPBar.sizeDelta = new Vector2(width, levelBar.sizeDelta.y);
                    if (unit.PercentToNextLevel(_lazyXP) == 0)
                        ApplyGainedLevel();
                    else
                        xpRising.Play();
                }
                float xpDelta = unit.CurrentXP - _lazyXP;
                float waitTime = Mathf.Min(_minTrackerUpdateSpeed * xpDelta, _maxTrackerCompletionTime) / xpDelta;
                yield return new WaitForSecondsRealtime(waitTime);
            }
        }
        Dictionary<StatSystem.Refactor.StatType, int> totalOfStatsGained;
        List<Dictionary<StatSystem.Refactor.StatType, int>> _statsGained;
        int _levelGainedIndex = 0;
        void GainedLevel() => _statsGained.Add(new Dictionary<StatSystem.Refactor.StatType, int>());
        void GainedStat(StatSystem.Refactor.StatType type, int amount)
        {
            var list = _statsGained[_statsGained.Count - 1];
            if(list.ContainsKey(type)) 
                list[type] += amount;
            else
                list.Add(type, amount);
        }
        void ApplyGainedLevel()
        {
            if (_statsGained[_levelGainedIndex].Count == 0) return;
            LevelUp.Play();
            foreach(var stat in _statsGained[_levelGainedIndex].Keys)
            {
                int bonus = _statsGained[_levelGainedIndex][stat];
                if (totalOfStatsGained.ContainsKey(stat))
                    totalOfStatsGained[stat] += bonus;
                else 
                    totalOfStatsGained.Add(stat,bonus);
            }
            _levelGainedIndex += 1;
        }
        void UpdateBonusStats(Dictionary<StatSystem.Refactor.StatType, int> statsGained)
        {
            string bonuses = "";
            foreach (var statBonus in statsGained)
            {
                bonuses += $"{statBonus.Key.ToString()}: {statBonus.Value}, ";
            }
            if(bonuses.Length > 0) 
                bonuses = bonuses.Remove(bonuses.Length - 1);
            levelUps.text = bonuses;
        }
        IEnumerator TrackLevelBonuses()
        {
            Dictionary<StatSystem.Refactor.StatType, int> lazyBonuses = new();
            yield return new WaitForSecondsRealtime(Random.Range(0.05f, 0.2f));
            while (enabled)
            {
                float bonuses = 0;
                foreach (var bonus in totalOfStatsGained.Values)
                    bonuses += bonus;
                if(bonuses > 0)
                {
                    GainStat.Play();
                    bonuses--;
                    var key = totalOfStatsGained.ToList()[Random.Range(0, totalOfStatsGained.Count - 1)];
                    totalOfStatsGained[key.Key]--;
                    FloatText.FireText(textFloater, transform, $"+{key.Value} to {key.Key.ToString()}", Random.Range(0.5f, 5));
                    if (lazyBonuses.ContainsKey(key.Key)) lazyBonuses[key.Key]++;
                    else lazyBonuses.Add(key.Key, 1);
                    UpdateBonusStats(lazyBonuses);
                    if (totalOfStatsGained[key.Key] == 0) totalOfStatsGained.Remove(key.Key);
                }
                
                float waitTime = Mathf.Min(_minTrackerUpdateSpeed * bonuses, _maxTrackerCompletionTime) / bonuses;
                yield return new WaitForSecondsRealtime(waitTime);
            }
        }
        
        void CharacterUpdate(Campaign.PCWrapper unit)
        {
            if (this.unit != unit) return;
            stats.text = unit.statBase.StatString();
            //levelUps.text = unit.LevelBonuses();
        }
    }
}