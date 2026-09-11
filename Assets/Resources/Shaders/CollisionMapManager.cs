using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
[RequireComponent(typeof(Grid))]
public class CollisionMapManager : MonoBehaviour
{
    public static CollisionMapManager instance;
    public Grid grid => GetComponent<Grid>();
    [SerializeField]
    public Vector2Int mapSize = Vector2Int.one;
    ComputeShader CollisionMap;
    public RenderTexture heatMap;
    public ComputeBuffer occupancyMap, collisionMap;
    public int resolution=8;
    void Awake()
    {
        
        instance = this;
        CollisionMap = Resources.Load<ComputeShader>("Shaders/CollisionMap");
        heatMap = new RenderTexture(mapSize.x*resolution, mapSize.y*resolution, 0, RenderTextureFormat.ARGB32);
        heatMap.enableRandomWrite = true;
        heatMap.Create();
        occupancyMap = new ComputeBuffer(mapSize.x*mapSize.y, sizeof(uint));//this is a 32 bit uint which forms a boolean map
        collisionMap = new ComputeBuffer(mapSize.x*mapSize.y, sizeof(uint));
        //pass the data to the compute shader
        CollisionMap.SetTexture(0, "_Pixels", heatMap);
        CollisionMap.SetBuffer(0, "_OccupancyMap", occupancyMap);
        CollisionMap.SetBuffer(0, "_CollisionMap", collisionMap);
        CollisionMap.SetBuffer(1, "_OccupancyMap", occupancyMap);
        CollisionMap.SetBuffer(1, "_CollisionMap", collisionMap);
        CollisionMap.SetVector("_cellSize", grid.cellSize);
        CollisionMap.SetInt("_pixelsPerUnitX", resolution);
        CollisionMap.SetInt("_pixelsPerUnitY", resolution);
        CollisionMap.SetInt("_GridWidth", mapSize.x);
        CollisionMap.SetInt("_GridHeight", mapSize.y);
        //CollisionMap.SetInts("_pixelsPerUnit", mapSize.x / resolution, mapSize.y / resolution);

        GetComponent<SpriteRenderer>().sprite = GetSprite();
    }
    public Action UpdateProjections;
    [SerializeField] bool flag = true;  
    public void Update()
    {
        CollisionMap.Dispatch(1, mapSize.x * mapSize.y / 64, 1, 1);
        if(flag) 
            UpdateProjections?.Invoke();
        CollisionMap.Dispatch(0, heatMap.width/8, heatMap.height/8, 1);
    }



    public Sprite GetSprite()
    {
        return Sprite.Create(

            // We use Texture2D.CreateExternalTexture to treat the RT as a texture
            Texture2D.CreateExternalTexture(
                heatMap.width,
                heatMap.height,
                TextureFormat.RGBA32,
                false, false,
                heatMap.GetNativeTexturePtr()),
            new Rect(0, 0, heatMap.width, heatMap.height),
            new Vector2(0.5f, 0.5f),
            resolution/grid.cellSize.x
        );
    }
    [SerializeField]
    bool _drawGizmos = true;
    void OnDrawGizmos()
    {
        if(!_drawGizmos) return;
        Gizmos.DrawWireCube(transform.position, Vector2.Scale(grid.cellSize, mapSize));
    }
}
