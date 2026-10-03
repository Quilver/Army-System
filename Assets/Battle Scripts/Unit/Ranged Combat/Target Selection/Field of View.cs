using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FieldofView : MonoBehaviour
{
    [SerializeField, Range(0, 120)]
    float FOV = 90f;
    [SerializeField, Range(2, 20)]
    int rayCount = 2;
    float range
    {
        get { return rangedWeapon.Range; }
    }
    MeshFilter _mesh;
    Mesh mesh;
    MeshRenderer _renderer;
    IUnit unit;
    RangedWeapon rangedWeapon;
    public Dictionary<Transform, float> _targets;
    public LayerMask SensorLayerMask;
    public List<Transform> _targetsList;
    public Formation.IFormationData _formationData;
    
    void Start()
    {

        unit = GetComponentInParent<IUnit>();
        _formationData = unit.GetComponentInChildren<Formation.IFormationData>();    
        rangedWeapon= GetComponentInParent<RangedWeapon>();
        _mesh = GetComponent<MeshFilter>();
        _renderer = GetComponent<MeshRenderer>();
        _renderer.material.color = rangedWeapon.CurrentColour;
        transform.position = Vector3.zero;
        transform.parent.rotation = Quaternion.Euler(Vector3.zero);
    }
    void Update()
    {
        transform.position = Vector3.zero;
        transform.parent.rotation = Quaternion.Euler(Vector3.zero);
        if (unit.State == UnitState.Idle)
        {
            _targets = new();
            _renderer.enabled = true;
            UpdateMesh();
            transform.position = Vector3.zero;
            _renderer.material.color = rangedWeapon.CurrentColour;
            _targetsList = _targets.Keys.ToList();
        }
        else
        {
            _renderer.enabled = false;
            _targetsList = new();
        }
    }
    void UpdateMesh()
    {
        var meshes = new CombineInstance[3];
        meshes[0].mesh = UpdateMeshLeft(); meshes[0].transform = Matrix4x4.identity;
        meshes[1].mesh = UpdateMeshForward(); meshes[1].transform = Matrix4x4.identity;
        meshes[2].mesh = UpdateMeshRight(); meshes[2].transform = Matrix4x4.identity;
        mesh = new Mesh();
        mesh.CombineMeshes(meshes);
        _mesh.mesh = mesh;

    }
    Mesh UpdateMeshLeft()
    {
        Mesh mesh = new();

        float angle = -unit.transform.eulerAngles.z;
        float angleIncrease = FOV / rayCount;
        Vector3 origin = LPosition;
        Vector3[] vertices = new Vector3[rayCount + 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[rayCount * 3];
        vertices[0] = origin;
        int vertexIndex = 1;
        int triangleIndex = 0;
        for (int i = 0; i < rayCount; i++)
        {
            Vector3 vertex = Ray(origin, angle);
            vertices[vertexIndex] = vertex;
            if (i > 0)
            {
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = vertexIndex - 1;
                triangles[triangleIndex + 2] = vertexIndex;
            }

            angle -= angleIncrease;
            triangleIndex += 3;
            vertexIndex++;
        }


        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        return mesh;
        //_mesh.mesh = mesh;
    }
    Mesh UpdateMeshRight()
    {
        Mesh mesh = new();

        float angle = -unit.transform.eulerAngles.z + FOV - FOV/12;
        float angleIncrease = FOV / rayCount;
        Vector3 origin = RPosition;
        Vector3[] vertices = new Vector3[rayCount + 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[rayCount * 3];
        vertices[0] = origin;
        int vertexIndex = 1;
        int triangleIndex = 0;
        for (int i = 0; i < rayCount; i++)
        {
            Vector3 vertex = Ray(origin, angle);
            vertices[vertexIndex] = vertex;
            if (i > 0)
            {
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = vertexIndex - 1;
                triangles[triangleIndex + 2] = vertexIndex;
            }

            angle -= angleIncrease;
            triangleIndex += 3;
            vertexIndex++;
        }


        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        return mesh;
        //_mesh.mesh = mesh;
    }
    Mesh UpdateMeshForward()
    {
        Mesh mesh = new();
        int rays = Mathf.CeilToInt(2 * Vector3.Distance(LPosition, RPosition)) + 1;
        float angle = -unit.transform.eulerAngles.z;
        Vector3 origin = LPosition;
        Vector3 diff = (RPosition - LPosition)/(float)(rays-1);
        Vector3[] vertices = new Vector3[(rays + 1)* 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[rays * 6];
        int vertexIndex = 0;
        int triangleIndex = 0;
        for (int i = 0; i < rays; i++)
        {
            Vector3 vertex = Ray(origin, angle);
            vertices[vertexIndex] = vertex;
            vertices[vertexIndex + 1] = origin;
            if (i > 0)
            {
                //t1
                triangles[triangleIndex] = vertexIndex - 2;
                triangles[triangleIndex + 1] = vertexIndex - 1;
                triangles[triangleIndex + 2] = vertexIndex;
                //t2
                triangles[triangleIndex + 3] = vertexIndex - 1;
                triangles[triangleIndex + 4] = vertexIndex;
                triangles[triangleIndex + 5] = vertexIndex + 1;
            }
            triangleIndex += 6;
            vertexIndex+=2;
            origin += diff;
        }
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        return mesh;
        //_mesh.mesh = mesh;
    }
    Vector3 LPosition
    {
        get
        {
            Vector2 pos2  = unit.transform.position-_formationData.ModelSize*(_formationData.Width-1) *unit.transform.right/4;
            Vector3 pos = unit.transform.position - unit.transform.right;
            //pos += GetVectorFromAngle(unit.Movement.Rotation)/2;
            return pos2;
        }
    }
    Vector3 RPosition
    {
        get
        {
            Vector2 pos2 = unit.transform.position + _formationData.ModelSize * (_formationData.Width - 1) * unit.transform.right/4;
            Vector3 pos = unit.transform.position + unit.transform.right;
            //pos += GetVectorFromAngle(unit.Movement.Rotation)/2;
            return pos2;
        }
    }
    Vector3 Ray(Vector3 origin, float angle)
    {
        var raycast2D = Physics2D.Raycast(origin, GetVectorFromAngle(angle), range, SensorLayerMask);
        if (raycast2D.collider == null)
            return origin + GetVectorFromAngle(angle) * range;
        else
        {
            var target = raycast2D.transform.GetComponentInParent<IUnit>();
            HittingUnit(target, raycast2D.distance);
            return raycast2D.point;
        }
    }
    void HittingUnit(IUnit unit, float Distance)
    {
        if (unit == null) return;
        if (!_targets.ContainsKey(unit.transform))
        {
            _targets.Add(unit.transform, Distance);
        }
        else
        {
            if (_targets[unit.transform] > Distance)
                _targets[unit.transform] = Distance;
        }
    }
    static Vector3 GetVectorFromAngle(float angle)
    {
        float angleRad = angle * (Mathf.PI / 180f);
        return new Vector3(Mathf.Sin(angleRad), Mathf.Cos(angleRad));
    }
}
