using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PlayerControls
{
    public class CameraControls : MonoBehaviour
    {
        [SerializeField]
        Vector2 MinimumCameraBounds, MaxCameraBounds;
        float targetOrtho;
        [SerializeField, Range(4, 15)]
        float cameraSpeed = 5;
        [SerializeField, Range(2, 10)]
        float zoomSpeed = 3;
        [SerializeField, Range(5, 20)]
        float smoothSpeed = 10f;
        [SerializeField, Range(5, 20)]
        float minOrtho = 5.0f;
        [SerializeField, Range(10, 30)]
        float maxOrtho = 20.0f;
        void Start()
        {
            targetOrtho = Camera.main.orthographicSize;
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = new(255, 0, 0, 50);
            Vector3 center = (MinimumCameraBounds + MaxCameraBounds) / 2;
            Vector3 size = MaxCameraBounds - MinimumCameraBounds;
            size.x = Mathf.Abs(size.x);
            size.y = Mathf.Abs(size.y);
            Gizmos.DrawWireCube(center, size);
        }
        // Update is called once per frame
        void Update()
        {
            MoveCamera();
        }
        void MoveCamera()
        {
            //move camera
            float xAxisValue = Input.GetAxis("Horizontal");
            float yAxisValue = Input.GetAxis("Vertical");
            float xDir = (xAxisValue) * cameraSpeed * Time.deltaTime;
            float yDir = (yAxisValue) * cameraSpeed * Time.deltaTime;
            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(camera.transform.position.x + xDir, camera.transform.position.y + yDir, camera.transform.position.z);
            }
            //zoom camera
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0.0f)
            {
                targetOrtho -= scroll * zoomSpeed;
                targetOrtho = Mathf.Clamp(targetOrtho, minOrtho, maxOrtho);
            }
            ClampCamera();
            camera.orthographicSize = Mathf.MoveTowards(camera.orthographicSize, targetOrtho, smoothSpeed * Time.deltaTime);
        }
        void ClampCamera()
        {
            var camera = Camera.main;
            float screenHeightInUnits = camera.orthographicSize;// * 2;
            float screenWidthInUnits = screenHeightInUnits * Screen.width / Screen.height;
            Vector3 position = camera.transform.position;
            if (position.x < MinimumCameraBounds.x + screenWidthInUnits)
                position.x = MinimumCameraBounds.x + screenWidthInUnits;
            else if (position.x > MaxCameraBounds.x - screenWidthInUnits)
                position.x = MaxCameraBounds.x - screenWidthInUnits;
            if (position.y < MinimumCameraBounds.y + screenHeightInUnits)
                position.y = MinimumCameraBounds.y + screenHeightInUnits;
            else if (position.y > MaxCameraBounds.y - screenHeightInUnits)
                position.y = MaxCameraBounds.y - screenHeightInUnits;
            camera.transform.position = position;
        }
    }
}