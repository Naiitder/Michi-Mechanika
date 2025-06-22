using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{
    [HideInInspector] private PlayerAnimatorController playerAnimatorController;
    
    [Header("Movement")]
    private new Transform transform;
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float rotationSpeed = 15f;
    private bool isMoving = false;
    
    [Header("Tiles")]
    [SerializeField] private LayerMask interactiveLayer;
    [SerializeField] private Tile currentTile;
    
    private Vector2 swipeStart;
    private bool isSwiping;

    
    public void Initialize()
    {
        transform = GetComponent<Transform>();
        playerAnimatorController = GetComponent<PlayerAnimatorController>();
        playerAnimatorController.Initialize();
        if(TileController.instance != null) currentTile = TileController.instance.GetClosestTile(transform.position);
    }


    private void Update()
    {
        if (GameController.instance == null || !GameController.instance.canInteract || isMoving) return;
        HandleInput();

    }
    
    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            swipeStart = Input.mousePosition;
            isSwiping = true;
        }

        if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            Vector2 swipeEnd = Input.mousePosition;
            Vector2 swipeDelta = swipeEnd - swipeStart;
            isSwiping = false;
            
            if (swipeDelta.magnitude < 50f)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactiveLayer))
                {
                    Lever clickedLever = hit.collider.GetComponent<Lever>();
                    if (clickedLever != null)
                    {
                        clickedLever.PullLever(currentTile);
                    }
                }

                return;
            }
            
            Vector2 dir = swipeDelta.normalized;

            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                if (dir.x > 0)
                    TryMoveInDirection(Tile.Direction.Right);
                else
                    TryMoveInDirection(Tile.Direction.Left);
            }
            else
            {
                if (dir.y > 0)
                    TryMoveInDirection(Tile.Direction.Forward);
                else
                    TryMoveInDirection(Tile.Direction.Back);
            }
        }
    }
    
    private void TryMoveInDirection(Tile.Direction direction)
    {
        foreach (Tile neighbor in currentTile.connectedTiles)
        {
            Tile.Direction? dirToNeighbor = TileController.instance.GetCardinalDirection(currentTile.position, neighbor.position);
            if (dirToNeighbor.HasValue && dirToNeighbor.Value == direction)
            {
                MoveToNextPosition(neighbor);
                return;
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
