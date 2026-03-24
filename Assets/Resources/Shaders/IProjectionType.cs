using UnityEngine;
namespace ComputeShaderTest
{
    [System.Serializable]
    public abstract class IProjectionType
    {
        public static IProjectionType Create(Rigidbody2D body)
        {
            if (body.GetComponent<CircleCollider2D>() != null) return new CircleProjection(body.GetComponent<CircleCollider2D>());
            else if (body.GetComponent<BoxCollider2D>() != null) return new RectangleProjection(body.GetComponent<BoxCollider2D>());
            else throw new System.Exception("Unsupported collider type");
        }
        public abstract ComputeShader ComputeShader { get; }
        public abstract void Update();
    }
    [System.Serializable]
    public class CircleProjection : IProjectionType
    {
        [SerializeField] ComputeShader circleProjection;
        [SerializeField] CircleCollider2D circleCollider;
        public CircleProjection(CircleCollider2D collider)
        {
            circleProjection = (ComputeShader)Resources.Load("Shaders/Projection");
            circleCollider = collider;
        }
        public override ComputeShader ComputeShader => circleProjection;
        public override void Update() => ComputeShader.SetFloat("_Radius", circleCollider.radius);
    }
    [System.Serializable]
    public class RectangleProjection : IProjectionType
    {
        [SerializeField] ComputeShader boxProjection;
        [SerializeField] BoxCollider2D boxCollider;
        public RectangleProjection(BoxCollider2D collider)
        {
            boxProjection = (ComputeShader)Resources.Load("Shaders/BoxProjection");
            boxCollider = collider;
        }
        public override ComputeShader ComputeShader => boxProjection;
        public override void Update() {
            ComputeShader.SetVector("_BoxSize", Vector3.Scale(boxCollider.size, boxCollider.transform.localScale)/2);
            ComputeShader.SetFloat("_Radius", Vector3.Scale(boxCollider.size, boxCollider.transform.localScale).magnitude/2);
            
        } 
    }
}