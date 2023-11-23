using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUD : MonoBehaviour {
    public GUISkin tileSkin, unitSkin;
    public GUIStyle mouseDragSkin;
    public const int UNIT_BAR_WIDTH = 150, TILE_BAR_HEIGHT = 40;
    private PlayerInput player;
    // Use this for initialization
    void Start () {
        player = transform.root.GetComponent<PlayerInput>();
    }
	
	// Update is called once per frame
	void OnGUI() {
        DrawUnitBar();
        DrawTileBar();
        //DrawSelectBox();
    }
    private void DrawTileBar()
    {
        string description = getString(player.hoverObject);
        GUI.skin = tileSkin;
        GUI.BeginGroup(new Rect(0, 0, Screen.width, TILE_BAR_HEIGHT));
        GUI.Box(new Rect(0, 0, Screen.width, TILE_BAR_HEIGHT), description);
        GUI.EndGroup();
    }
    private void DrawUnitBar()
    {
        string description = "";
        foreach (Unit unit in player.selectedUnits)
        {
            description += unit.getDetails();
            description += "\n";
        }
        GUI.skin = unitSkin;
        GUI.BeginGroup(new Rect(Screen.width - UNIT_BAR_WIDTH, TILE_BAR_HEIGHT, UNIT_BAR_WIDTH, Screen.height - TILE_BAR_HEIGHT));
        GUI.Box(new Rect(0, 0, UNIT_BAR_WIDTH, Screen.height - TILE_BAR_HEIGHT), description);
        GUI.EndGroup();
    }
    void DrawQuad(Rect position, Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        GUI.skin.box.normal.background = texture;
        GUI.Box(position, GUIContent.none);
    }
    private static Texture2D _staticRectTexture;
    private static GUIStyle _staticRectStyle;
    /*void DrawSelectBox()
    {
        if (!player.selectBoxActive && player.selectBox.size.x >= 1 && player.selectBox.size.y >= 1) { return; }
        GUI.Box(player.selectBox, "", mouseDragSkin);
    }*/
    string getString(Section section)
    {
        if (section == null)
        {
            return "";
        }
        else
        {
            return section.getDetails();
        }
    }
}
