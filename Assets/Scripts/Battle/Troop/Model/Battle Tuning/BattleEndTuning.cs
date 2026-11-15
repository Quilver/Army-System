using UnityEngine;

// Battle Tuning: what happens between the battle's end and leaving its scene (SU-168).
// Asset: Assets/Resources/Battle Tuning/Battle End Tuning.asset. Read through TroopTuning.Active.
//   When the battle is won or lost (Battle.EndBattle) the fighting goes on at End Slow Speed while a Victory or Defeat
//   banner shows for End Banner Seconds of real time; then the battle hands over to the campaign (Battle.Leaving).
[CreateAssetMenu(menuName = "Battle Tuning/Battle End Tuning")]
public sealed class BattleEndTuning : ScriptableObject
{
    public const string AssetName = "Battle End Tuning";

    [Header("Battle end (SU-168)")]
    [SerializeField, Range(GamePause.MinSpeed, 1f), Tooltip("Game speed while the end banner shows (1 = normal speed).")]
    float _endSlowSpeed = 0.25f;
    [SerializeField, Min(0f), Tooltip("Seconds of real time the Victory or Defeat banner shows before the next scene loads.")]
    float _endBannerSeconds = 10f;

    public float EndSlowSpeed => _endSlowSpeed;
    public float EndBannerSeconds => _endBannerSeconds;
}
