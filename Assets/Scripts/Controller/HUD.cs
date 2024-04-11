using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class HUD : MonoBehaviour {
    [SerializeField] TextMeshProUGUI mousePos, unitDetail;
    [SerializeField]
    PlayerInput player;
   
    void OnGUI() {
        DrawUnitBar();
        DrawTileBar();
    }
    private void DrawTileBar()
    {
        string description = player.Cursor.ToString();
        if (player.hoverUnit != null)
            description += "\n"+player.hoverUnit.ToString();
        mousePos.text = description;
    }
    private void DrawUnitBar()
    {
        if(player.selectedUnit!=null)
            unitDetail.text = player.selectedUnit.ToString();
        else
            unitDetail.text= string.Empty;
    }
}
