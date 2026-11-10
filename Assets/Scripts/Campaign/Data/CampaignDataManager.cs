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
        // Saving and loading are switched off while the game is prototyped and tuned (SU-132): the campaign lives in
        // memory on this persistent manager for the play session, so the roster's levels and stat gains carry from one
        // mission to the next, but nothing is read from or written to campaign.json. Set to true to bring saves back.
        public static bool SavingEnabled = false;

        [SerializeField]
        bool LoadData, SaveData;
        [SerializeField]
        CampaignData data;
        public static CampaignData Data
        {
            get
            {
                if ((SavingEnabled && CampaignDataManager.instance.LoadData) || CampaignDataManager.instance.data == null)
                {
                    // No save (or saving switched off): keep the starting data set on the DataManager prefab (or an
                    // empty campaign).
                    CampaignData loaded = SavingEnabled ? CampaignDataManager.instance.SaverLoader.Load() : null;
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
            // Saving off: continue with the campaign already in memory (or the DataManager's starting data).
            if (!SavingEnabled)
            {
                SceneManager.LoadScene(1);
                return;
            }
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
            if (SavingEnabled && SaveData)
                SaverLoader.Save(data);
        }
        private void OnApplicationQuit()
        {
            SaveGame();
        }

    }
}
