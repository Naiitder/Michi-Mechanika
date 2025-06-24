using System;
using System.Collections;
using UnityEngine;

public class Lever : MonoBehaviour
{
    public Tile tileWhereLeverIs;
    public bool activated = false;
    
    public GameObject[] tilesGameObjects;
    
    [SerializeField] private Transform pivotGameObject; 
    private Quaternion originalRotation;
    
    [SerializeField] private Transform pivotA;
    [SerializeField] private Transform pivotB;
    [SerializeField] private float durationTransition = 1f;
    
    public void Initialize()
    {
        if(TileController.instance != null) tileWhereLeverIs = TileController.instance.GetClosestTile(transform.position);
        originalRotation = pivotGameObject.localRotation;
    }

    public void PullLever(Tile tileWherePlayerIs)
    {
        if (tileWhereLeverIs != tileWherePlayerIs) return;
        
        activated = !activated;
        if( GameController.instance != null) GameController.instance.canInteract = false;
        StopAllCoroutines();
        
        Quaternion targetRotation = activated
            ? Quaternion.Euler(0, 0, -45)
            : originalRotation;
        
        StartCoroutine(RotatePivot(pivotGameObject, targetRotation, 0.5f));

        Transform pivotTarget = activated ? pivotB : pivotA;

        foreach (GameObject go in tilesGameObjects)
        {
            StartCoroutine(MoveToPivot(go, pivotTarget, durationTransition));
        }
        
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
        if(GameController.instance != null)  GameController.instance.canInteract = true;
        
    }
    private IEnumerator RotatePivot(Transform target, Quaternion targetRotation, float duration)
    {
        Quaternion startRotation = target.localRotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            target.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        target.localRotation = targetRotation;
    }

}
