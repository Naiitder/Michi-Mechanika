using System.Collections;
using UnityEngine;

public class TilePression : Tile
{
    bool active = false;
    bool somethingUp = false;
    [SerializeField] Transform baseTransform;

    [Header("PlatformMovement")] 
    [SerializeField] private GameObject[] tilesGameObjects;

    [SerializeField] private Transform pivotA;
    [SerializeField] private Transform pivotB;
    

    public void CheckForPression(bool isSomethingUp)
    {
        if (isSomethingUp && !active)
        {
            this.somethingUp = true;
        }
        else if (!isSomethingUp && active)
        {
            this.somethingUp = false;
        }
    }

    public IEnumerator ActivateOrDeactivate()
    {
        if (somethingUp && !active)
        {
            active = true;
            yield return StartCoroutine(Activate());
        }
        else if (!somethingUp && active)
        {
            active = false;
            yield return StartCoroutine(DeActivate());
        }
    }

    IEnumerator Activate()
    {
        StartCoroutine(MoveBaseTransform(Vector3.down / 2, 0.3f));
        foreach (GameObject go in tilesGameObjects)
        {
            yield return StartCoroutine(MoveToPivot(go, pivotB, 1.25f));
        }
    }
    
    IEnumerator DeActivate()
    {
        StartCoroutine(MoveBaseTransform(Vector3.up / 2, 0.3f));
        foreach (GameObject go in tilesGameObjects)
        {
            yield return StartCoroutine(MoveToPivot(go, pivotA, 1.25f));
        }
    }
    
    private IEnumerator MoveBaseTransform(Vector3 targetOffset, float duration)
    {
        Vector3 startPos = baseTransform.position;
        Vector3 endPos = startPos + targetOffset;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            baseTransform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        baseTransform.position = endPos;
    }
    
    private IEnumerator MoveToPivot(GameObject go, Transform targetPivot, float duration)
    {
        Transform t = go.transform;
    
        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;

        Vector3 endPos = targetPivot.position;
        Quaternion endRot = targetPivot.rotation;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float tRaw = Mathf.Clamp01(elapsed / duration);
            float tLerp = Mathf.SmoothStep(0f, 1f, tRaw);

        
            t.position = Vector3.Lerp(startPos, endPos, tLerp);
            t.rotation = Quaternion.Slerp(startRot, endRot, tLerp);
        
            yield return null;
        }
        
        t.position = endPos;
        t.rotation = endRot;

        foreach (GameObject tilesGOs in tilesGameObjects)
        {
            Tile[] tilesInThisGO = tilesGOs.GetComponentsInChildren<Tile>();
            foreach (Tile tile in tilesInThisGO)
            {
                tile.UpdatePosition();
            }
        }
        
        if(TileController.instance != null) TileController.instance.ConnectTiles();
    }
}
