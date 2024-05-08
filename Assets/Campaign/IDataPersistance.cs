using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Campaign
{
    public interface IDataPersistance
    {
        void LoadData(CampaignData data);
        void SaveData(CampaignData data);

    }
}