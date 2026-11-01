using System.IO;
using UnityEngine;

namespace Campaign
{
    // Saves and loads the campaign as JSON in Application.persistentDataPath. A save from an older version (including
    // the pre-troop-system "data.game") is deleted and the player starts a new game.
    public class FileDataHandler
    {
        const string DataFileName = "campaign.json";
        static readonly string[] ObsoleteFileNames = { "data.game" };

        readonly string dataDirPath = Application.persistentDataPath;

        string FullPath => Path.Combine(dataDirPath, DataFileName);

        public FileDataHandler()
        {
            DeleteObsoleteSaves();
        }

        public CampaignData Load()
        {
            string fullPath = FullPath;
            if (!File.Exists(fullPath)) return null;
            try
            {
                Debug.Log("Loading at: " + fullPath);
                CampaignData loadedData = JsonUtility.FromJson<CampaignData>(File.ReadAllText(fullPath));
                if (loadedData == null || loadedData.SaveVersion != CampaignData.CurrentSaveVersion)
                {
                    Debug.LogWarning($"Save at {fullPath} is from an older version and was deleted. Starting a new game.");
                    File.Delete(fullPath);
                    return null;
                }
                loadedData.ResolveReferences();
                return loadedData;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error occured when trying to load data: " + fullPath + "\n " + e);
                return null;
            }
        }

        public void Save(CampaignData data)
        {
            string fullPath = FullPath;
            Debug.Log("Saving to: " + fullPath);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                data.SaveVersion = CampaignData.CurrentSaveVersion;
                File.WriteAllText(fullPath, JsonUtility.ToJson(data, true));
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error when trying to save to " + fullPath + "\n " + e);
                throw;
            }
        }

        void DeleteObsoleteSaves()
        {
            foreach (string name in ObsoleteFileNames)
            {
                string path = Path.Combine(dataDirPath, name);
                if (!File.Exists(path)) continue;
                try
                {
                    File.Delete(path);
                    Debug.Log("Deleted obsolete save: " + path);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Could not delete obsolete save " + path + "\n " + e);
                }
            }
        }
    }
}
