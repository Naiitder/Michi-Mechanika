using System;
using System.Collections.Generic;
using UnityEngine;

public class GearsController : MonoBehaviour
{
    public GearDecoration[] gears;

    private void Awake()
    {
        gears = FindObjectsByType<GearDecoration>(FindObjectsSortMode.None);
    }

    void Update()
    {
        float delta = Time.deltaTime;

        foreach (GearDecoration gear in gears)
        {
            gear.transform.Rotate(gear.axis * gear.speed * delta);
        }
    }
}
