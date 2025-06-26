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

        if (InputController.instance.HasDragged)
        {
            Vector2 dir = InputController.instance.DragDirection;
            InputController.instance.HasDragged = false;
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;

            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 worldDir = (camRight * dir.x + camForward * dir.y).normalized;

            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 targetPos = origin + worldDir * 2f;
            

            Tile targetTile = TileController.instance.GetClosestTile(targetPos, currentTile);

            if (targetTile != null && currentTile.connectedTiles.Contains(targetTile))
            {
                MoveToNextPosition(targetTile);
            }
            else
            {
                Vector3 forward = transform.forward;
                float dot = Vector3.Dot(worldDir, forward);
                
                if (dot > 0.5f)
                    targetPos = new Vector3(transform.position.x,targetPos.y+2,transform.position.z);
                else if (dot < -0.5f)
                    targetPos = new Vector3(transform.position.x,targetPos.y-2,transform.position.z);
                
                targetTile = TileController.instance.GetClosestTile(targetPos, currentTile);
                
                //Debug.DrawLine(origin, targetPos, Color.green, 1f);

                if (targetTile != null && currentTile.connectedTiles.Contains(targetTile))
                {
                    MoveToNextPosition(targetTile);
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

    protected override void CheckTile(Tile targetTile)
    {
        currentTile = targetTile;
        
        if(currentTile is TilePression)
        {
            TilePression tp = (TilePression)currentTile;
            tp.CheckForPression(true);
        }
        
        //Todo cambiarlo y activar animacion de matar
        if(currentTile.enemyOnTile != null) currentTile.enemyOnTile.Die();
        if(GameController.instance != null) GameController.instance.UpdateGameFlow();
    }
}
