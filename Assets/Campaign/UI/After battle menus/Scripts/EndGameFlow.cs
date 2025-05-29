using Campaign;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EndGameUI
{
    public class EndGameFlow : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI time, kills, deaths, money, prestige;
        [SerializeField] List<AudioClip> moneyGained, prestigeGained;
        [SerializeField] AudioSource moneyAudio, prestigeAudio;
        // Start is called before the first frame update
        void Start()
        {
            foreach (var pc in BattleReport.DeployedCharacters)
            {
                if (!Campaign.CampaignDataManager.Data.Characters.Contains(pc))
                    Campaign.CampaignDataManager.Data.Characters.Add(pc);
            }
            StartCoroutine(TrackFloat(BattleReport.timeTaken, (float t)=>time.text=GetTime(t) ));
            StartCoroutine(TrackInt(BattleReport.kills, (int k) => kills.text =$"Kills: {k}" ));
            StartCoroutine(TrackInt(BattleReport.deaths, (int d) => deaths.text = $"Deaths: {d}"));
            Cursor.visible = true;
        }
        IEnumerator TrackFloat(float endValue, System.Action<float> setString)
        {
            float lazyValue = 0;
            float updateSpeed = 0.1f;
            float floatIncrement = updateSpeed / _maxTrackerCompletionTime * endValue * 5;
            yield return new WaitForSecondsRealtime(Random.Range(0.05f, 0.2f));
            while (lazyValue < endValue)
            {
                lazyValue+= floatIncrement;
                setString(lazyValue);
                yield return new WaitForSeconds(updateSpeed);
            }
        }
        IEnumerator TrackInt(int endValue, System.Action<int> setString)
        {
            int lazyValue = 0;
            int delta = endValue - lazyValue;
            float waitTime = Mathf.Min(_minTrackerUpdateSpeed * delta, _maxTrackerCompletionTime) / delta;
            yield return new WaitForSecondsRealtime(Random.Range(0.05f, 0.2f));
            while (lazyValue < endValue)
            {
                lazyValue ++;
                setString(lazyValue);
                yield return new WaitForSecondsRealtime(waitTime);
            }
        }
        private void OnEnable()
        {
            Campaign.CampaignData.MoneyGained += UpdateMoney;
            Campaign.CampaignData.CurrentPrestigeGained += UpdatePrestige;
            StartCoroutine(TrackMoney());
            StartCoroutine(TrackPrestige());
        }
        private void OnDisable()
        {
            Campaign.CampaignData.MoneyGained -= UpdateMoney;
            Campaign.CampaignData.CurrentPrestigeGained -= UpdatePrestige;
        }
        [SerializeField, Range(0.2f, 1)] float _minTrackerUpdateSpeed = 0.3f;
        [SerializeField, Range(1, 10)] float _maxTrackerCompletionTime = 5;
        IEnumerator TrackMoney()
        {
            money.text = $"Gold: {CampaignDataManager.Data.Money}G";
            yield return new WaitForSecondsRealtime(Random.Range(0.05f, 0.2f));
            while (enabled)
            {
                if (_moneyDelta > 0)
                {
                    _moneyDelta--;
                    money.text = $"Gold: {CampaignDataManager.Data.Money-_moneyDelta}G";
                    PlayAudioClip(moneyAudio, moneyGained, Mathf.Min(_minTrackerUpdateSpeed * _moneyDelta, _maxTrackerCompletionTime) / _moneyDelta);
                }
                float waitTime = Mathf.Min(_minTrackerUpdateSpeed * _moneyDelta, _maxTrackerCompletionTime) / _moneyDelta;
                yield return new WaitForSecondsRealtime(waitTime);
            }
        }
        IEnumerator TrackPrestige()
        {
            prestige.text = $"Prestige: {0}";
            yield return new WaitForSecondsRealtime(Random.Range(0.05f, 0.2f));
            while (enabled)
            {
                if (_prestigeDelta > 0)
                {
                    _prestigeDelta--;
                    prestige.text = $"Prestige: {_prestigeGained - _prestigeDelta}";
                    PlayAudioClip(prestigeAudio, prestigeGained, Mathf.Min(_minTrackerUpdateSpeed * _prestigeDelta, _maxTrackerCompletionTime) / _prestigeDelta);
                }
                float waitTime = Mathf.Min(_minTrackerUpdateSpeed * _prestigeDelta, _maxTrackerCompletionTime)/_prestigeDelta;
                yield return new WaitForSecondsRealtime(waitTime);
            }
        }
        int _moneyDelta, _prestigeGained, _prestigeDelta;
        void UpdateMoney(int change) => _moneyDelta += change;
        void UpdatePrestige(int change) {
            _prestigeGained += change;
            _prestigeDelta += change;
        } 
        string GetTime(float timer)
        {
            int minutes = Mathf.FloorToInt(timer / 60F);
            int seconds = Mathf.FloorToInt(timer - minutes * 60);
            return "Time taken: "+ string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        [SerializeField, Range(0.3f, 1)] float _minPitch;
        [SerializeField, Range(1, 3)] float _maxPitch;
        void PlayAudioClip(AudioSource source, List<AudioClip> clips, float speed)
        {
            var clip = clips[Random.Range(0, clips.Count)];
            source.pitch = Mathf.Lerp(_minPitch, _maxPitch, speed);
            source.clip = clip;
            source.Play();
        }
    }
}