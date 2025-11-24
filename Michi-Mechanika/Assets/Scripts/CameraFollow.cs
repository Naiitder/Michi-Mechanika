using System;
using Unity.Cinemachine;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private CinemachineCamera vcamPlayer;
    [SerializeField] private CinemachineCamera vcamFree;
    [SerializeField] private float dragSpeed = 1f;

    void Update()
    {
        bool isBuilding = GameController.instance.isLevelBuilding;

        if (vcamFree != null)
            vcamFree.gameObject.SetActive(isBuilding);
        if (vcamPlayer != null)
            vcamPlayer.gameObject.SetActive(!isBuilding);

        if (isBuilding)
            HandleFreeCamDrag();
    }
    
    private void HandleFreeCamDrag()
    {
        var input = InputController.instance;
        if (input == null) return;
        if (!input.IsCameraDragging) return;

        Vector2 camDelta = input.CameraDragDelta;

        Vector3 move = new Vector3(-camDelta.x, 0, -camDelta.y) * dragSpeed;
        vcamFree.transform.Translate(move, Space.World);
    }
}
