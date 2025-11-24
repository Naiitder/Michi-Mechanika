using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        if (GameFlow.instance == null || !GameFlow.instance.canInteract || isMoving) return;
        HandleBufferedInput();
    }

    public void Die()
    {
        anim.SetBool(DeadHash, true);
        LevelManager.instance.RestartScene();
    }

    private void HandleBufferedInput()
    {
        if (InputController.instance == null) return;

        if (!InputController.instance.TryDequeueAction(out var action))
            return; 

        switch (action.Type)
        {
            case BufferedActionType.Click:
                ProcessClick(action.ClickScreenPos);
                break;

            case BufferedActionType.DragMove:
                ProcessDrag(action.DragStart, action.DragEnd);
                break;
        }
    }

    private void ProcessClick(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactiveLayer))
        {
            Lever clickedLever = hit.collider.GetComponentInParent<Lever>();
            if (clickedLever != null)
            {
                GameFlow.instance.LockInteraction();
                clickedLever.PullLever(currentTile);
            }
        }
    }

    private void ProcessDrag(Vector2 dragStart, Vector2 dragEnd)
    {
        Vector2 dir = (dragEnd - dragStart).normalized;

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight   = Camera.main.transform.right;

        camForward.y = 0;
        camRight.y   = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 worldDir = (camRight * dir.x + camForward * dir.y).normalized;
        Tile.Direction? desiredDirection = GetDirectionFromWorld(worldDir);

        if (desiredDirection != null)
        {
            Tile targetTile = currentTile.GetConnectedTileInDirection(desiredDirection.Value);

            if (targetTile != null)
            {
                GameFlow.instance.LockInteraction();
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
                {
                    GameFlow.instance.LockInteraction();
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
            StartCoroutine(MoveSmoothlyTo(targetTile));
        }
        
    }

    protected override void CheckTile(Tile targetTile)
    {
        if(currentTile is TilePression)
        {
            TilePression tp = (TilePression)currentTile;
            tp.CheckForPression(false);
        }

        currentTile.characterOnTile = null;
        currentTile = targetTile;
        
        if(currentTile is TilePression)
        {
            TilePression tp = (TilePression)currentTile;
            tp.CheckForPression(true);
        }
        
        //Todo cambiarlo y activar animacion de matar
        if (currentTile.characterOnTile != null)
        {
            if (currentTile.characterOnTile is Enemy)
            {
                Enemy enemyOnTile = (Enemy)currentTile.characterOnTile;
                enemyOnTile.Die();
            }
            else if (currentTile.characterOnTile is Saw)
            {
                Die();
            }
        }
        if(currentTile.characterOnTile is not Saw) currentTile.characterOnTile = this;
        StartCoroutine(LevelFinish());
        if(GameController.instance != null) StartCoroutine(GameFlow.instance.UpdateGameFlow());
    }

    private IEnumerator LevelFinish()
    {
        if (currentTile.endingTile)
        {
            GameFlow.instance.levelEnded = true;
            
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                yield return AdvanceTillTheEnd();
                
                string nextScenePath = SceneUtility.GetScenePathByBuildIndex(nextSceneIndex);
                string nextSceneName = System.IO.Path.GetFileNameWithoutExtension(nextScenePath);

                if(LevelManager.instance != null)StartCoroutine(LevelManager.instance.LoadSceneFade(nextSceneName));
            }
        }
    }

    private IEnumerator AdvanceTillTheEnd()
    {
        anim.SetBool(WalkHash,true);
        Vector3 targetPosition = transform.position + transform.forward*8;
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                movementSpeed * Time.deltaTime
            );
            yield return null;
        }
        anim.SetBool(WalkHash,false);
    }
}
