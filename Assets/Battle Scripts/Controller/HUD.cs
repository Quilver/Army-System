using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class HUD : MonoBehaviour {
    [SerializeField] TextMeshProUGUI mousePos, unitDetail;
    [SerializeField] Image mouseUnit, SelectedUnit;
    [SerializeField]
    PlayerControls.PlayerInput player;
   
    void OnGUI() {
        DrawUnitBar();
        DrawTileBar();
    }
    private void DrawTileBar()
    {
        if (player.hoverUnit != null)
        {
            mousePos.text = player.hoverUnit.ToString();
            mouseUnit.enabled = true;
            mouseUnit.sprite = player.hoverUnit.Stats.portrait;
        }
        else
        {
            mousePos.text = "";
            mouseUnit.enabled = false;
            mouseUnit.sprite = null;
        }
    }
    private void DrawUnitBar()
    {
        if (player.selectedUnit != null)
        {
            unitDetail.text = player.selectedUnit.ToString();
            SelectedUnit.enabled = true;
            SelectedUnit.sprite = player.selectedUnit.Stats.portrait;
        }
        else
        {
            unitDetail.text = "";
            SelectedUnit.enabled = false;
            SelectedUnit.sprite = null;
        }
    }
}
