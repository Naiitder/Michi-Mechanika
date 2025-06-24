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
        if (!activated)
        {
            pivotGameObject.localRotation = originalRotation;
            
            foreach (GameObject go in tilesGameObjects)
            {
                StartCoroutine(MoveToPivot(go, pivotA, 0.5f)); 
            }

        }
        else
        {
            pivotGameObject.localRotation =  Quaternion.Euler(0, 0, -45);
            
            foreach (GameObject go in tilesGameObjects)
            {
                StartCoroutine(MoveToPivot(go, pivotB, 0.5f)); 
            }
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
            float tLerp = Mathf.Clamp01(elapsed / duration);
        
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

}
