using System;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform player;
    private Vector3 offset;
    [SerializeField] private float smoothSpeed = 5f;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        offset = transform.position;
    }

    void LateUpdate()
    {
        if(player == null || GameFlow.instance.levelEnded) return;
        
        Vector3 desiredPosition = player.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
        
    }
}
