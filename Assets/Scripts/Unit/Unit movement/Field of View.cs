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
    //[SerializeField, Range(2, 100)]
    float range
    {
        get { return rangedWeapon.Range; }
    }
    MeshFilter _mesh;
    Mesh mesh;
    MeshRenderer _renderer;
    UnitR unit;
    RangedWeapon rangedWeapon;
    public Dictionary<UnitR, float> _targets;

    // Start is called before the first frame update
    void Start()
    {
        unit = GetComponentInParent<UnitR>();
        rangedWeapon= GetComponentInParent<RangedWeapon>();
        _mesh = GetComponent<MeshFilter>();
        _renderer = GetComponent<MeshRenderer>();
        _renderer.material.color = rangedWeapon.CurrentColour;
        transform.position = Vector3.zero;
        transform.parent.rotation = Quaternion.Euler(Vector3.zero);
    }
    void Update()
    {
        if(unit.State == UnitState.Idle)
        {
            _targets = new();
            _renderer.enabled = true;
            UpdateMesh();
            transform.position = Vector3.zero;
            _renderer.material.color = rangedWeapon.CurrentColour;
        }
        else
        {
            _renderer.enabled = false;
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

        float angle = -unit.Movement.position.Rotation;
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
        
        float angle = -unit.Movement.position.Rotation + FOV - FOV/12;
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
        float angle = -unit.Movement.position.Rotation;
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
            Vector3 pos = unit.LeftMostModelPosition;
            pos += GetVectorFromAngle(unit.Movement.position.Rotation)/2;
            return pos;
        }
    }
    Vector3 RPosition
    {
        get
        {
            Vector3 pos = unit.RightMostModelPosition;
            pos -= GetVectorFromAngle(unit.Movement.position.Rotation)/2;
            return pos;
        }
    }
    Vector3 Ray(Vector3 origin, float angle)
    {
        var raycast2D = Physics2D.Raycast(origin, GetVectorFromAngle(angle), range, 1 << 6);
        if (raycast2D.collider == null)
            return origin + GetVectorFromAngle(angle) * range;
        else
        {
            var target = raycast2D.transform.GetComponentInParent<UnitR>();
            HittingUnit(target, raycast2D.distance);
            return raycast2D.point;
        }
    }
    void HittingUnit(UnitR unit, float Distance)
    {
        if (unit == null) return;
        if (!_targets.ContainsKey(unit))
        {
            _targets.Add(unit, Distance);
        }
        else
        {
            if (_targets[unit] > Distance)
                _targets[unit] = Distance;
        }
    }
    static Vector3 GetVectorFromAngle(float angle)
    {
        float angleRad = angle * (Mathf.PI / 180f);
        return new Vector3(Mathf.Sin(angleRad), Mathf.Cos(angleRad));
    }
}
