using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerMovement : CharacterMovement
{
    private Vector2 swipeStart;
    private bool isSwiping;

    [Header ("Interaction")]
    [SerializeField] private LayerMask interactiveLayer;
    
    public override void Initialize()
    {
        base.Initialize();
        if(TileController.instance != null) currentTile = TileController.instance.GetClosestTile(transform.position);
    }


    private void Update()
    {
        if (GameController.instance == null || !GameController.instance.canInteract || isMoving) return;
        HandleInput();

    }

    public void Die()
    {
        anim.SetBool(DeadHash, true);
        LevelManager.instance.RestartScene();
    }

    private void HandleInput()
    {
        if (InputController.instance == null) return;

        if (InputController.instance.HasClicked)
        {
            InputController.instance.HasClicked = false;
            Vector2 screenPos = InputController.instance.ClickPosition;
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactiveLayer))
            {
                Lever clickedLever = hit.collider.GetComponentInParent<Lever>();
                if (clickedLever != null)
                {
                    clickedLever.PullLever(currentTile);
                }
            }
        }

        if (InputController.instance.HasDragged && !isMoving)
        {
            Debug.Log("Moving");
            Vector2 dir = InputController.instance.DragDirection;
            InputController.instance.HasDragged = false;
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;

            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 worldDir = (camRight * dir.x + camForward * dir.y).normalized;
            Tile.Direction? desiredDirection = GetDirectionFromWorld(worldDir);

            if (desiredDirection != null)
            {
                Tile targetTile = currentTile.GetConnectedTileInDirection(desiredDirection.Value);

                if (targetTile != null)
                {
                    MoveToNextPosition(targetTile);
                }
                else
                {
                    Vector3 forward = transform.forward;
                    float dot = Vector3.Dot(worldDir, forward);

                    Tile verticalTile = null;

                    if (dot > 0.5f)
                        verticalTile = currentTile.GetConnectedTileAbove();
                    else if (dot < -0.5f) 
                        verticalTile = currentTile.GetConnectedTileBelow();

                    if (verticalTile != null)
                        MoveToNextPosition(verticalTile);
                }
            }
        }
    }

    Tile.Direction? GetDirectionFromWorld(Vector3 worldDir)
    {
        float absX = Mathf.Abs(worldDir.x);
        float absZ = Mathf.Abs(worldDir.z);

        if (absX > absZ)
        {
            return worldDir.x > 0 ? Tile.Direction.Forward : Tile.Direction.Back;
        }
        else
        {
            return worldDir.z > 0 ? Tile.Direction.Left : Tile.Direction.Right;
        }
    }
    
    private void MoveToNextPosition(Tile targetTile)
    {
        if (Array.Exists(currentTile.connectedTiles, t => t == targetTile))
        {
            MoveSmoothlyTo(targetTile);
        }
        
    }

    protected override void CheckTile(Tile targetTile)
    {
        if(currentTile is TilePression)
        {
            TilePression tp = (TilePression)currentTile;
            tp.CheckForPression(false);
        }
        
        currentTile = targetTile;
        
        if(currentTile is TilePression)
        {
            TilePression tp = (TilePression)currentTile;
            tp.CheckForPression(true);
        }
        
        //Todo cambiarlo y activar animacion de matar
        if(currentTile.enemyOnTile != null) currentTile.enemyOnTile.Die();
        if(currentTile.sawOnTile != null) Die();
        if(GameController.instance != null) GameController.instance.UpdateGameFlow();
    }
}
