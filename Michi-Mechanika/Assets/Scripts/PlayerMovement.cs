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
            MoveSmoothlyTo(targetTile);
        }
        
       
    }

    void MoveSmoothlyTo(Tile targetTile)
    {
        if(currentTile.tileType == Tile.Type.Floor && targetTile.tileType == Tile.Type.Floor) StartCoroutine(MoveFromFloorToFloor(targetTile.position));
        else if(currentTile.tileType == Tile.Type.Floor && targetTile.tileType == Tile.Type.Roof) StartCoroutine(MoveFromFloorToRoof(targetTile.position));
        else if(currentTile.tileType == Tile.Type.Roof && targetTile.tileType == Tile.Type.Floor) StartCoroutine(MoveFromRoofToFloor(targetTile.position));
        else if(currentTile.tileType == Tile.Type.Roof && targetTile.tileType == Tile.Type.Roof) StartCoroutine(MoveFromRoofToRoof(targetTile.position));
    }
    
    //Done
    IEnumerator MoveFromFloorToFloor(Vector3 targetPosition)
    {
        isMoving = true;
        playerAnimatorController.anim.SetBool(playerAnimatorController.WalkHash, true);
        
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
    }
    
    IEnumerator MoveFromFloorToRoof(Vector3 targetPosition)
    {
        isMoving = true;
        
        if (currentTile.position.y < targetPosition.y)
        {
            playerAnimatorController.anim.SetBool(playerAnimatorController.WalkHash, true);
        
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
            
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbHash, false);
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbDownHash, false);
            
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
            playerAnimatorController.anim.SetBool(playerAnimatorController.WalkHash, true);
          
            transform.position = targetPosition;

            isMoving = false;
        }
        
      
    }
    IEnumerator MoveFromRoofToFloor(Vector3 targetPosition)
    {
        if (currentTile.position.y == targetPosition.y)
        {
            if (currentTile.position.x > targetPosition.x)
            {
                isMoving = true;
                playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbLeftHash, true);
        
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
                playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbLeftHash, false);
            }
            else
            {
                isMoving = true;
                playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbRightHash, true);
        
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
                playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbRightHash, false);
            }
        }
        else if (currentTile.position.y < targetPosition.y)
        {
            playerAnimatorController.anim.SetBool(playerAnimatorController.WalkHash, true);
        
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
            
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbHash, false);
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbDownHash, false);
            
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
            playerAnimatorController.anim.SetBool(playerAnimatorController.WalkHash, true);
          
            transform.position = targetPosition;

            isMoving = false;
        }
    }
    
    //Done
    IEnumerator MoveFromRoofToRoof(Vector3 targetPosition)
    {
        if (currentTile.position.y == targetPosition.y)
        {
            if (currentTile.position.x > targetPosition.x)
            {
                isMoving = true;
                playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbLeftHash, true);
        
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
                playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbLeftHash, false);
            }
            else
            {
                isMoving = true;
                playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbRightHash, true);
        
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
                playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbRightHash, false);
            }
        }
        else if (currentTile.position.y < targetPosition.y)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0f;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
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
            transform.position = targetPosition;

            isMoving = false;

        }
        else
        {
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbDownHash, true);
            
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
            playerAnimatorController.anim.SetBool(playerAnimatorController.ClimbDownHash, false);

            isMoving = false;
        }
    }
}
