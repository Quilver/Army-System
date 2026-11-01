using System.Collections;
using System.Collections.Generic;
//using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngineInternal;

namespace Campaign
{
    
    public class CampaignDataManager: MonoBehaviour 
    {
        [SerializeField]
        bool LoadData, SaveData;
        [SerializeField]
        CampaignData data;
        public static CampaignData Data
        {
            get
            {
                if (CampaignDataManager.instance.LoadData || CampaignDataManager.instance.data == null)
                {
                    // No save: keep the starting data set on the DataManager prefab (or an empty campaign).
                    CampaignData loaded = CampaignDataManager.instance.SaverLoader.Load();
                    if (loaded != null) CampaignDataManager.instance.data = loaded;
                    else CampaignDataManager.instance.data ??= new CampaignData();
                    CampaignDataManager.instance.LoadData = false;
                    Debug.Log("Loading game");
                }
                return CampaignDataManager.instance.data;
            }
            private set
            {
                CampaignDataManager.instance.data = value;
            }
        }
        CampaignDataManager _manager;
        
        public static CampaignDataManager instance { get; private set; }
        FileDataHandler SaverLoader;
        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            SaverLoader= new FileDataHandler();
            DontDestroyOnLoad(gameObject);
        }
        // Pre-battle tent sample (Docs/PreBattle/TentHub.md): use in-memory data, never load or save a file.
        public void UseSampleData(CampaignData sample)
        {
            data = sample;
            LoadData = false;
            SaveData = false;
        }
        public void NewGame()
        {
            Debug.Log("New game");
            SceneManager.LoadScene(1);
        }
        
        public void LoadGame()
        {
            Data = SaverLoader.Load();
            if (data == null)
            {
                Debug.Log("no data found. starting new game");
                NewGame();
            }
            else
            {
                SceneManager.LoadScene(1);
            }
        }
        public void SaveGame()
        {
            if(SaveData)
                SaverLoader.Save(data);
        }
        private void OnApplicationQuit()
        {
            SaveGame();
        }

    }
}
