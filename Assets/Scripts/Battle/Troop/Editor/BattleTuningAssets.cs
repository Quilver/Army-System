using System.IO;
using UnityEditor;
using UnityEngine;

// Creates any missing Battle Tuning section asset in Assets/Resources/Battle Tuning (TroopTuning). Existing assets are
// never overwritten, so designer edits survive. Used by the Phase 01 generator and the menu item below.
public static class BattleTuningAssets
{
    [MenuItem("Troops/Battle Tuning/Create Missing Assets")]
    public static void EnsureAssets()
    {
        EnsureFolder();
        Ensure<SoldierTuning>(SoldierTuning.AssetName);
        Ensure<FormationTuning>(FormationTuning.AssetName);
        Ensure<MoraleTuning>(MoraleTuning.AssetName);
        Ensure<MeleeTuning>(MeleeTuning.AssetName);
        Ensure<ChainTuning>(ChainTuning.AssetName);
        Ensure<RangedTuning>(RangedTuning.AssetName);
        Ensure<InputTuning>(InputTuning.AssetName);
        AssetDatabase.SaveAssets();
    }

    static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(TroopTuning.AssetFolder)) return;
        AssetDatabase.CreateFolder(Path.GetDirectoryName(TroopTuning.AssetFolder).Replace('\\', '/'),
            Path.GetFileName(TroopTuning.AssetFolder));
    }

    static void Ensure<T>(string assetName) where T : ScriptableObject
    {
        string path = $"{TroopTuning.AssetFolder}/{assetName}.asset";
        if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;
        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        EditorUtility.SetDirty(asset);
    }
}
