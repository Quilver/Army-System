using UnityEngine;

// Battle Tuning: player input thresholds in screen pixels (spec §6, §7.2).
// Asset: Assets/Resources/Battle Tuning/Input Tuning.asset. Read through TroopTuning.Active.
[CreateAssetMenu(menuName = "Battle Tuning/Input Tuning")]
public sealed class InputTuning : ScriptableObject
{
    public const string AssetName = "Input Tuning";

    [Header("Input (spec §6, screen pixels)")]
    [SerializeField, Min(1f)] float _boxSelectMinDragPixels = 8f;
    [SerializeField, Min(1f), Tooltip("Provisional (Q-47).")] float _orderDragMinPixels = 8f;
    [SerializeField, Min(1f), Tooltip("Click pick radius around a pivot or soldier.")] float _clickPickRadiusPixels = 24f;

    public float BoxSelectMinDragPixels => _boxSelectMinDragPixels;
    public float OrderDragMinPixels => _orderDragMinPixels;
    public float ClickPickRadiusPixels => _clickPickRadiusPixels;
}
