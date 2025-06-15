using System;
using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [HideInInspector] private PlayerAnimatorController playerAnimatorController;
    
    [Header("Movement")]
    private Transform transform;
    [SerializeField] private float movementSpeed = 7.5f;
    private bool isMoving = false;
    
    [Header("Tiles")]
    [SerializeField] private LayerMask tileLayer;
    [SerializeField] private Tile currentTile;
    
    private void Awake()
    {
        transform = GetComponent<Transform>();
        playerAnimatorController = GetComponent<PlayerAnimatorController>();
        playerAnimatorController.Initialize();
    }


    private void Update()
    {
        GetNextPosition();
    }


    private void GetNextPosition()
    {
        if (Input.GetMouseButtonDown(0) && !isMoving)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayer)){
                
                Tile clickedTile = hit.collider.GetComponent<Tile>();
                
                if (clickedTile != null)
                {
                    MoveToNextPosition(clickedTile);
                }
            }
        }
        
        

    }
    
    private void MoveToNextPosition(Tile targetTile)
    {
        if (Array.Exists(currentTile.connectedTiles, t => t == targetTile))
        {

            currentTile = targetTile;
            StartCoroutine(MoveSmoothlyTo(targetTile.position));
        }
        
       
    }

    IEnumerator MoveSmoothlyTo(Vector3 targetPosition)
    {
        isMoving = true;
        playerAnimatorController.anim.SetBool(playerAnimatorController.WalkHash, true);
        
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.LookAt(targetPosition);
            
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                movementSpeed * Time.deltaTime
            );
            yield return null;
        }

        transform.position = targetPosition;

        isMoving = false;
        playerAnimatorController.anim.SetBool(playerAnimatorController.WalkHash, false);

    }
    
}
