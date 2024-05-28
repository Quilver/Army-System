using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
                    CampaignDataManager.instance.data = CampaignDataManager.instance.SaverLoader.Load();
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
