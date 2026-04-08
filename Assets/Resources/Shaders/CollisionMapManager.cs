using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
[RequireComponent(typeof(Grid))]
public class CollisionMapManager : MonoBehaviour
{
    public static CollisionMapManager instance;
    [StructLayout(LayoutKind.Sequential)]
    struct ShaderGrid
    {
        public Vector2 gridDimension, cellSize;
    };
    Grid grid => GetComponent<Grid>();
    [SerializeField]
    public Vector2Int mapSize = Vector2Int.one;
    ComputeShader CollisionMap;
    public RenderTexture heatMap;
    public ComputeBuffer occupancyMap;
    public int resolution=8;
    void Awake()
    {
        
        instance = this;
        CollisionMap = Resources.Load<ComputeShader>("Shaders/CollisionMap");
        heatMap = new RenderTexture(mapSize.x*resolution, mapSize.y*resolution, 0, RenderTextureFormat.ARGB32);
        heatMap.enableRandomWrite = true;
        heatMap.Create();
        occupancyMap = new ComputeBuffer(mapSize.x*mapSize.y, sizeof(uint));//this is a 32 bit uint which forms a boolean map
        
        //pass the data to the compute shader
        CollisionMap.SetTexture(0, "_Pixels", heatMap);
        CollisionMap.SetBuffer(0, "_Mask", occupancyMap);
        CollisionMap.SetVector("_cellSize", grid.cellSize);
        CollisionMap.SetVector("_pixelsPerUnit", new Vector2(resolution, resolution));
        //CollisionMap.SetInts("_pixelsPerUnit", mapSize.x / resolution, mapSize.y / resolution);
        
        GetComponent<SpriteRenderer>().sprite = GetSprite();
    }
    public Action UpdateProjections;
    public void Update()
    {
        CollisionMap.Dispatch(0, Mathf.CeilToInt(mapSize.x), Mathf.CeilToInt(mapSize.y), 1);
        UpdateProjections?.Invoke();
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
