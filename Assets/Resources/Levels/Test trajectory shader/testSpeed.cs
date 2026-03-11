using UnityEngine;
namespace Test
{
    public class testSpeed : MonoBehaviour
    {
        [SerializeField] Vector2 velocity;
        [SerializeField] float rotation;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            GetComponent<Rigidbody2D>().linearVelocity = velocity;
            GetComponent<Rigidbody2D>().angularVelocity = rotation;
        }

        // Update is called once per frame
        void Update()
        {
            GetComponent<Rigidbody2D>().linearVelocity = velocity;
            GetComponent<Rigidbody2D>().angularVelocity = rotation;
        }
    }

}
