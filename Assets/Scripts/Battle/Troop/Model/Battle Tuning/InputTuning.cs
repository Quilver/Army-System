using UnityEngine;

// Battle Tuning: player input thresholds in screen pixels (spec §6, §7.2).
// Asset: Assets/Resources/Battle Tuning/Input Tuning.asset. Read through TroopTuning.Active.
[CreateAssetMenu(menuName = "Battle Tuning/Input Tuning")]
public sealed class InputTuning : ScriptableObject
{
    public const string AssetName = "Input Tuning";

    [Header("Input (spec §6, screen pixels)")]
    [SerializeField, Min(1f)] float _boxSelectMinDragPixels = 8f;
    [SerializeField, Min(1f), Tooltip("Minimum drag in screen pixels for a drag order.")] float _orderDragMinPixels = 8f;
    [SerializeField, Min(1f), Tooltip("Click pick radius around a pivot or soldier.")] float _clickPickRadiusPixels = 24f;
    [SerializeField, Min(0.05f), Tooltip("Seconds between two left clicks on a troop for a double click (selects its chain, SU-129).")]
    float _doubleClickSeconds = 0.35f;

    public float BoxSelectMinDragPixels => _boxSelectMinDragPixels;
    public float OrderDragMinPixels => _orderDragMinPixels;
    public float ClickPickRadiusPixels => _clickPickRadiusPixels;
    public float DoubleClickSeconds => _doubleClickSeconds;
}
