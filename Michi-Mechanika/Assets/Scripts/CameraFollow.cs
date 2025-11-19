using System;
using Unity.Cinemachine;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private CinemachineCamera vcamPlayer;
    [SerializeField] private CinemachineCamera vcamFree;
    [SerializeField] private float dragSpeed = 1f;

    private Vector3 lastMousePos;

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
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePos = Input.mousePosition;
        }
        if (Input.GetMouseButton(0))
        {
            Vector3 current = Input.mousePosition;
            Vector3 delta = current - lastMousePos;
            lastMousePos = current;

            Vector3 move = new Vector3(-delta.x, 0, -delta.y) * dragSpeed * Time.deltaTime;
            vcamFree.transform.Translate(move, Space.World);
        }
    }
}
