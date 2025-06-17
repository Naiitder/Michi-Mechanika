using System;
using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [HideInInspector] private PlayerAnimatorController playerAnimatorController;
    
    [Header("Movement")]
    private Transform transform;
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float rotationSpeed = 15f;
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
        GetNextAction();
    }


    private void GetNextAction()
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
                
                Lever clickedLever = hit.collider.GetComponent<Lever>();

                if (clickedLever != null)
                {
                    
                }
            }
        }
        
        

    }
    
    private void MoveToNextPosition(Tile targetTile)
    {
        if (Array.Exists(currentTile.connectedTiles, t => t == targetTile))
        {
            MoveSmoothlyTo(targetTile);
        }
        
       
    }

    void MoveSmoothlyTo(Tile targetTile)
    {
        if(currentTile.tileType == Tile.Type.Floor && targetTile.tileType == Tile.Type.Floor) StartCoroutine(MoveFromFloorToFloor(targetTile));
        else if(currentTile.tileType == Tile.Type.Floor && targetTile.tileType == Tile.Type.Roof) StartCoroutine(MoveFromFloorToRoof(targetTile));
        else if(currentTile.tileType == Tile.Type.Roof && targetTile.tileType == Tile.Type.Floor) StartCoroutine(MoveFromRoofToFloor(targetTile));
        else if(currentTile.tileType == Tile.Type.Roof && targetTile.tileType == Tile.Type.Roof) StartCoroutine(MoveFromRoofToRoof(targetTile));

    }

    IEnumerator MoveFromFloorToFloor(Tile targetTile)
    {
        isMoving = true;
        playerAnimatorController.anim.SetBool(playerAnimatorController.WalkHash, true);
        
        Vector3 targetPosition = targetTile.position;
        
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed 
            );
            
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
        currentTile = targetTile;
    }
    
    IEnumerator MoveFromFloorToRoof(Tile targetTile)
    {
        isMoving = true;
        playerAnimatorController.anim.SetBool(playerAnimatorController.WalkHash, true);
        
        Vector3 targetPosition = targetTile.position;
        
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
            
        Vector3 targetPositionFlat = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        while (Vector3.Distance(transform.position, targetPositionFlat) > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed 
            );

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPositionFlat,
                movementSpeed * Time.deltaTime
            );
            yield return null;
        }
        playerAnimatorController.anim.SetBool(playerAnimatorController.WalkHash, false);

        if (currentTile.position.y < targetPosition.y)
        {
           
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbUpHash, true);
            
            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotationSpeed 
                );

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    movementSpeed * Time.deltaTime
                );
                yield return null;
            }
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbUpHash, false);
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbHash, true);
            transform.position = targetPosition;

            isMoving = false;

        }
        else
        {
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbDownHash, true);

            targetRotation = transform.rotation * Quaternion.Euler(0, 180f, 0);
            
            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotationSpeed 
                );

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    movementSpeed * Time.deltaTime
                );
                yield return null;
            }
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbDownHash, false);
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbHash, true);
            transform.position = targetPosition;

            isMoving = false;
        }
        
        currentTile = targetTile;
    }
    IEnumerator MoveFromRoofToFloor(Tile targetTile)
    {
        isMoving = true;
        
        Vector3 targetPosition = targetTile.position;
        
        if(currentTile.position.y < targetPosition.y )playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbUpHash, true);
       else playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbDownHash, true);
        
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
            
        Vector3 targetPositionVertical = new Vector3(transform.position.x, targetPosition.y, transform.position.z);
        while (Vector3.Distance(transform.position, targetPositionVertical) > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed 
            );

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPositionVertical,
                movementSpeed * Time.deltaTime
            );
            yield return null;
        }
        
        playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbUpHash, false); 
        playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbDownHash, false);
        
        playerAnimatorController.anim.SetBool(playerAnimatorController.WalkHash, true);
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed 
            );
            
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
       
        currentTile = targetTile;

    }
    
    IEnumerator MoveFromRoofToRoof(Tile targetTile)
    {
        Vector3 targetPosition = targetTile.position;
        
        if (currentTile.position.y == targetPosition.y)
        {
            if (currentTile.position.x > targetPosition.x)
            {
                isMoving = true;
                playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbLeftHash, true);
        
                Vector3 direction = (targetPosition - transform.position).normalized;
                direction.y = 0f;
                
                while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        targetPosition,
                        movementSpeed * Time.deltaTime
                    );
                    yield return null;
                }

                transform.position = targetPosition;

                isMoving = false;
                playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbLeftHash, false);
            }
            else
            {
                isMoving = true;
                playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbRightHash, true);
        
                Vector3 direction = (targetPosition - transform.position).normalized;
                direction.y = 0f;
                
                while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        targetPosition,
                        movementSpeed * Time.deltaTime
                    );
                    yield return null;
                }

                transform.position = targetPosition;

                isMoving = false;
                playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbRightHash, false);
            }
        }
        else if (currentTile.position.y < targetPosition.y)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0f;
            
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbUpHash, true);
            
            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    movementSpeed * Time.deltaTime
                );
                yield return null;
            }
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbUpHash, false);
            transform.position = targetPosition;

            isMoving = false;

        }
        else
        {
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbDownHash, true);
            
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0f;
            
            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    movementSpeed * Time.deltaTime
                );
                yield return null;
            }
          
            transform.position = targetPosition;
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbDownHash, false);

            isMoving = false;
        }
        
        currentTile = targetTile;

    }
}
