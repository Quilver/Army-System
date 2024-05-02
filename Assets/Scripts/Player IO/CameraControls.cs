using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
        Player inputs;
        void Awake()
        {
            targetOrtho = Camera.main.orthographicSize;
            inputs = new Player();
        }
        private void OnEnable()
        {
            inputs.Enable();
            inputs.CameraControls.Zoom.performed += ZoomCamera;
            inputs.CameraControls.Zoom.canceled += ZoomCamera;
            inputs.CameraControls.MoveCamera.performed += MoveCamera;
            inputs.CameraControls.MoveCamera.canceled += MoveCamera;

        }
        private void OnDisable()
        {
            inputs.Disable();
            inputs.CameraControls.Zoom.performed -= ZoomCamera;
            inputs.CameraControls.Zoom.canceled -= ZoomCamera;
            inputs.CameraControls.MoveCamera.performed -= MoveCamera;
            inputs.CameraControls.MoveCamera.canceled -= MoveCamera;
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
        float scroll;
        void ZoomCamera(InputAction.CallbackContext context)
        {
            scroll = context.ReadValue<float>();
        }
        Vector2 direction= Vector2.zero;
        void MoveCamera(InputAction.CallbackContext context)
        {
            direction = context.ReadValue<Vector2>();
            direction = direction.normalized * cameraSpeed;
        }
        void Update()
        {
            UpdateCamera();
            //MoveCamera();
        }
        void UpdateCamera()
        {
            Camera.main.transform.position += (Vector3)direction * Time.unscaledDeltaTime;
            if(scroll != 0)
            {
                targetOrtho -= scroll * zoomSpeed;
                targetOrtho = Mathf.Clamp(targetOrtho, minOrtho, maxOrtho);
                Camera.main.orthographicSize = Mathf.MoveTowards(Camera.main.orthographicSize, targetOrtho, smoothSpeed * Time.unscaledDeltaTime);
            }
            ClampCamera();
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