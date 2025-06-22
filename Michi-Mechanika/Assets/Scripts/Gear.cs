using System;
using UnityEngine;

public class Gear : MonoBehaviour
{
    public float rotationSpeed = 50f;
    public Vector3 rotationAxis = Vector3.up;

    void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
    }
}
