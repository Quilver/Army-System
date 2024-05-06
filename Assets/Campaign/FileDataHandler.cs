using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace Campaign
{
    public class FileDataHandler
    {
        string dataDirPath = Application.persistentDataPath;
        [SerializeField]
        string dataFileName = "data.game";
        public CampaignData Load()
        {
            string fullPath = Path.Combine(dataDirPath, dataFileName);
            CampaignData loadedData = null;
            if(File.Exists(fullPath))
            {
                try
                {
                    string dataToLoad = "";
                    using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            dataToLoad = reader.ReadToEnd();
                        }
                    }
                    loadedData = JsonUtility.FromJson<CampaignData>(dataToLoad);    
                }
                catch (System.Exception e){
                    Debug.LogError("Error occured when trying to data: " + fullPath + "\n " + e);
                }
            }
            return loadedData;
        }
        public void Save(CampaignData data)
        {
            string fullPath = Path.Combine(dataDirPath, dataFileName);
            Debug.Log("Saving to: " + fullPath);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                string dataToStore = JsonUtility.ToJson(data, true);
                using(FileStream stream = new FileStream(fullPath, FileMode.Create))
                {
                    using(StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.Write(dataToStore);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error when trying to save to " + fullPath + "\n " + e);

                throw;
            }
        }
    }
}