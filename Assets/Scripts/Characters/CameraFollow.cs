using System;
using Unity.Cinemachine;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private CinemachineCamera vcamPlayer;
    [SerializeField] private CinemachineCamera vcamFree;
    [SerializeField] private float dragSpeed = 1f;
    
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minFOV = 25f;
    [SerializeField] private float maxFOV = 90f;
    
    [SerializeField] private float rotationSpeed = 0.2f;


    void Update()
    {
        bool isBuilding = GameController.instance.isLevelBuilding;

        if (vcamFree != null)
            vcamFree.gameObject.SetActive(isBuilding);
        if (vcamPlayer != null)
            vcamPlayer.gameObject.SetActive(!isBuilding);

        if (isBuilding)
        {
            HandleFreeCamDrag();
            HandleFreeCamZoom(); 
        }
    }
    
    private void HandleFreeCamZoom()
    {
        float scroll = InputController.instance.CameraScroll;

        if (Mathf.Abs(scroll) < 0.01f) return;

        var lens = vcamFree.Lens;

        lens.FieldOfView -= scroll * zoomSpeed;
        lens.FieldOfView = Mathf.Clamp(lens.FieldOfView, minFOV, maxFOV);

        vcamFree.Lens = lens; 
    }

    
    private void HandleFreeCamDrag()
    {
        var input = InputController.instance;
        if (input == null) return;
        if (!input.IsCameraDragging) return;

        Vector2 camDelta = input.CameraDragDelta;

        bool isAltHeld = InputController.instance.IsAltHeld;

        if (isAltHeld)
        {
            HandleCameraRotation(camDelta);
        }
        else
        {
            HandleCameraPan(camDelta);
        }
    }

    private void HandleCameraPan(Vector2 camDelta)
    {
        Transform cam = vcamFree.transform;

        Vector3 forward = cam.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = cam.right;
        
        Vector3 move = (-camDelta.x * right + -camDelta.y * forward) * dragSpeed;

        cam.Translate(move, Space.World);
    }

    
    private void HandleCameraRotation(Vector2 camDelta)
    {
        float yaw = camDelta.x * rotationSpeed;
        
        float pitch = -camDelta.y * rotationSpeed;
        
        var euler = vcamFree.transform.eulerAngles;

        euler.y += yaw;
        euler.x = Mathf.Clamp(euler.x + pitch, 10f, 80f);

        vcamFree.transform.eulerAngles = euler;
    }

}
