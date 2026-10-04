using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : CharacterMovement
{
    private Vector2 swipeStart;
    private bool isSwiping;
    
    private static readonly int IdleState = Animator.StringToHash("Idle");
    private static readonly int JumpLState = Animator.StringToHash("Jump_L");
    private static readonly int JumpRState = Animator.StringToHash("Jump_R");
    private static readonly int FloorToClimbUpState = Animator.StringToHash("Floor_To_ClimbUp");
    private static readonly int FloorToClimbDownState = Animator.StringToHash("Floor_To_ClimbDown");
    private static readonly int ClimbToFloorUpState = Animator.StringToHash("Climb_To_Floor_Up");
    private static readonly int ClimbToFloorDownState = Animator.StringToHash("Climb_To_Floor_Down");
    private static readonly int ClimbUpState = Animator.StringToHash("Climb_Up");
    private static readonly int ClimbDownState = Animator.StringToHash("Climb_Down");
    private static readonly int ClimbLeftState = Animator.StringToHash("Climb_Left");
    private static readonly int ClimbRightState = Animator.StringToHash("Climb_Right");
    private static readonly int JumpLAttackState = Animator.StringToHash("Jump_L_Attack");
    private static readonly int JumpRAttackState = Animator.StringToHash("Jump_R_Attack");
    private static readonly int JumpLSheatheState = Animator.StringToHash("Jump_L_Sheathe");
    private static readonly int JumpRSheatheState = Animator.StringToHash("Jump_R_Sheathe");
    private bool attackNext;
    
    private static readonly int HopSpeedParam = Animator.StringToHash("hopSpeed");
    private const float FallbackHopClipLength = 1f;
    private const float DefaultHopSpeed = 0.9f;
    private readonly Dictionary<int, float> hopClipLengths = new Dictionary<int, float>();
    private Coroutine hopSpeedRoutine;
    private float nextHopDistance;

    private const float AnimBlendTime = 0.1f;  
    private const float HopInterval = 0.32f;    
    private bool leftFootNext = true;

    [Header ("Interaction")]
    [SerializeField] private LayerMask interactiveLayer;

    [Header ("Jump")]
    [SerializeField] private AnimationCurve hopCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
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


    protected override void SetWalking(bool walking)
    {
    }

    protected override float EvaluateHopProgress(float t)
    {
        return hopCurve != null && hopCurve.length > 1 ? hopCurve.Evaluate(t) : t;
    }
    
    protected override void PlayMove(MoveAnim move)
    {
        if (anim == null) return;

        int state;
        bool floorHop = false;
        float hopTravelTime = 0f;
        switch (move)
        {
            case MoveAnim.FloorToClimbUp:   state = FloorToClimbUpState; break;
            case MoveAnim.FloorToClimbDown: state = FloorToClimbDownState; break;
            case MoveAnim.ClimbToFloorUp:   state = ClimbToFloorUpState; break;
            case MoveAnim.ClimbToFloorDown: state = ClimbToFloorDownState; break;
            case MoveAnim.ClimbUp:          state = ClimbUpState; break;
            case MoveAnim.ClimbDown:        state = ClimbDownState; break;
            case MoveAnim.ClimbLeft:        state = ClimbLeftState; break;
            case MoveAnim.ClimbRight:       state = ClimbRightState; break;
            default:
                floorHop = true;
                if (nextHopDistance > 0.01f && movementSpeed > 0f)
                    hopTravelTime = nextHopDistance / movementSpeed;
                nextHopDistance = 0f;
                if (attackNext)
                {
                    attackNext = false;
                    anim.SetBool(AttackHash, true);
                }

                bool inTransition = anim.IsInTransition(0);
                int currentState = anim.GetCurrentAnimatorStateInfo(0).shortNameHash;

                if (!inTransition && currentState == IdleState)
                    leftFootNext = true;
                
                bool weaponDrawn = !inTransition &&
                                   (currentState == JumpLAttackState || currentState == JumpRAttackState);

                if (attackNext)
                    state = leftFootNext ? JumpLAttackState : JumpRAttackState;
                else if (weaponDrawn)
                    state = leftFootNext ? JumpLSheatheState : JumpRSheatheState;
                else
                    state = leftFootNext ? JumpLState : JumpRState;
                leftFootNext = !leftFootNext;
                break;
        }

    
        if (floorHop)
        {
            if (hopSpeedRoutine != null) StopCoroutine(hopSpeedRoutine);
            hopSpeedRoutine = null;

            if (hopTravelTime > 0f)
            {
                float clipLength = hopClipLengths.TryGetValue(state, out float known) ? known : FallbackHopClipLength;
                anim.SetFloat(HopSpeedParam, clipLength / hopTravelTime);
                hopSpeedRoutine = StartCoroutine(SyncHopSpeedToClip(state, hopTravelTime));
            }
            else
            {
                anim.SetFloat(HopSpeedParam, DefaultHopSpeed);
            }
        }

        if (floorHop && anim.IsInTransition(0))
            anim.Play(state, 0, 0f);
        else
            anim.CrossFadeInFixedTime(state, AnimBlendTime, 0, 0f);
    }

  
    private IEnumerator SyncHopSpeedToClip(int state, float travelTime)
    {
        for (int attempt = 0; attempt < 4; attempt++)
        {
            yield return null; 

            AnimatorClipInfo[] clips = null;
            if (anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).shortNameHash == state)
                clips = anim.GetNextAnimatorClipInfo(0);
            else if (anim.GetCurrentAnimatorStateInfo(0).shortNameHash == state)
                clips = anim.GetCurrentAnimatorClipInfo(0);

            if (clips != null && clips.Length > 0 && clips[0].clip != null)
            {
                float length = clips[0].clip.length;
                if (length > 0.01f)
                {
                    hopClipLengths[state] = length;
                    anim.SetFloat(HopSpeedParam, length / travelTime);
                }
                break;
            }
        }
        hopSpeedRoutine = null;
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
            attackNext = targetTile.characterOnTile is Enemy;
            nextHopDistance = targetTile.tileType == Tile.Type.Floor
                ? Vector3.Distance(transform.position, targetTile.position)
                : 0f;
            StartCoroutine(MoveSmoothlyTo(targetTile));
        }
        
    }

    protected override void CheckTile(Tile targetTile)
    {
        anim.SetBool(AttackHash, false);
        
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
        float nextHop = 0f;
        float elapsed = 0f;
        Vector3 targetPosition = transform.position + transform.forward*8;
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            if (elapsed >= nextHop)
            {
                PlayMove(MoveAnim.Walk);
                nextHop += HopInterval;
            }
            elapsed += Time.deltaTime;

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                movementSpeed * Time.deltaTime
            );
            yield return null;
        }
    }
}
