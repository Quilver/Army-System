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
        string description = getString(player.hoverObject);
        mousePos.text = description;
    }
    private void DrawUnitBar()
    {
        string description = "";
        foreach (Unit unit in player.selectedUnits)
        {
            description += unit.getDetails();
            description += "\n";
        }
        unitDetail.text = description;
    }
    string getString(SelectionData section)
    {
        if (section == null)
        {
            return "";
        }
        else
        {
            return section.ToString();
        }
    }
}
