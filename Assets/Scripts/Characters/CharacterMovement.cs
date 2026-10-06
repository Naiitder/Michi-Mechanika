using System.Collections;
using UnityEngine;

public abstract class CharacterMovement : MonoBehaviour
{
    [SerializeField] public Tile currentTile;
    
    [Header("Movement")]
    protected new Transform transform;
    [SerializeField] protected float movementSpeed = 5f;
    [SerializeField] private float rotationSpeed = 15f;
    protected bool isMoving = false;

    [Header("Animation")]
    protected Animator anim;
    [HideInInspector] public int WalkHash;
    [HideInInspector] public int DeadHash;
    [HideInInspector] public int AttackHash;
    
    protected CharacterAudio sfx;
    
    public virtual void Initialize()
    {
        anim = GetComponent<Animator>();
        WalkHash = Animator.StringToHash("walk");
        DeadHash = Animator.StringToHash("isDead");
        AttackHash = Animator.StringToHash("attack");
        sfx = GetComponent<CharacterAudio>();
        
        transform = GetComponent<Transform>();
    }
    
    protected enum MoveAnim
    {
        Walk,
        FloorToClimbUp,
        FloorToClimbDown,
        ClimbToFloorUp,
        ClimbToFloorDown,
        ClimbUp,
        ClimbDown,
        ClimbLeft,
        ClimbRight
    }
    
    protected virtual void SetWalking(bool walking)
    {
        if (anim != null) anim.SetBool(WalkHash, walking);
    }
    
    
    protected virtual void PlayMove(MoveAnim move)
    {
    }
    
    protected virtual void OnStep(Tile.Type from, Tile.Type to)
    {
        if (sfx != null) sfx.PlayStep(from, to);
    }

   
    protected virtual float EvaluateHopProgress(float t)
    {
        return t;
    }

    
    protected virtual float RoofHeightOffset => 0f;
    protected virtual float RoofDepthOffset => 0f;
    protected virtual float RoofEdgeInset => 0.3f;
    protected float nextHopDistance;
    
    protected Vector3 StandPosition(Tile tile, Vector3 facingWall)
    {
        if (tile.tileType != Tile.Type.Roof) return tile.position;

        facingWall.y = 0f;
        return tile.position
               + Vector3.up * RoofHeightOffset
               - facingWall.normalized * RoofDepthOffset;
    }

    public IEnumerator MoveSmoothlyTo(Tile targetTile)
    {
        OnStep(currentTile.tileType, targetTile.tileType);
        if(currentTile.tileType == Tile.Type.Floor && targetTile.tileType == Tile.Type.Floor) yield return StartCoroutine(MoveFromFloorToFloor(targetTile));
        else if(currentTile.tileType == Tile.Type.Floor && targetTile.tileType == Tile.Type.Roof) yield return StartCoroutine(MoveFromFloorToRoof(targetTile));
        else if(currentTile.tileType == Tile.Type.Roof && targetTile.tileType == Tile.Type.Floor) yield return StartCoroutine(MoveFromRoofToFloor(targetTile));
        else if(currentTile.tileType == Tile.Type.Roof && targetTile.tileType == Tile.Type.Roof) yield return StartCoroutine(MoveFromRoofToRoof(targetTile));
        yield return null;
    }

    IEnumerator MoveFromFloorToFloor(Tile targetTile)
    {
        isMoving = true;
        SetWalking(true);
        PlayMove(MoveAnim.Walk);
        
        Vector3 targetPosition = targetTile.position;
        
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // El salto dura lo mismo que antes (distancia / velocidad), pero el avance sigue
        // EvaluateHopProgress para poder acelerar y frenar en vez de ir a velocidad uniforme.
        Vector3 startPosition = transform.position;
        float duration = Vector3.Distance(startPosition, targetPosition) / Mathf.Max(movementSpeed, 0.0001f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed 
            );
            
            transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, EvaluateHopProgress(t));
            yield return null;
        }

        transform.position = targetPosition;

        isMoving = false;
        SetWalking(false);
        
        CheckTile(targetTile);
    }
    
    IEnumerator MoveFromFloorToRoof(Tile targetTile)
    {
        isMoving = true;
        SetWalking(true);
        
        bool goingUp = currentTile.position.y < targetTile.position.y;
        Vector3 toWall = targetTile.position - transform.position;
        toWall.y = 0f;
        Vector3 targetPosition = StandPosition(targetTile, goingUp ? toWall : -toWall);

        if (!goingUp)
        {
            yield return StartCoroutine(DescendFromFloorToRoof(targetTile, toWall.normalized, targetPosition));
            yield break;
        }

        PlayMove(MoveAnim.FloorToClimbUp);
        
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
        SetWalking(false);

        if (goingUp)
        {
           
            
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
            
        }
        else
        {

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
            transform.position = targetPosition;
            
        }
        
        isMoving = false;
        CheckTile(targetTile);
    }
    IEnumerator DescendFromFloorToRoof(Tile targetTile, Vector3 outDirection, Vector3 hangPosition)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 edge = new Vector3(targetTile.position.x, startPosition.y, targetTile.position.z);
        Vector3 edgePosition = edge - outDirection * RoofEdgeInset;
        Quaternion faceWall = Quaternion.LookRotation(-outDirection);

        float approachDuration = Mathf.Max(Vector3.Distance(startPosition, edgePosition) / Mathf.Max(movementSpeed, 0.0001f), 0.25f);
        nextHopDistance = approachDuration * movementSpeed;
        PlayMove(MoveAnim.Walk);

        float elapsed = 0f;
        while (elapsed < approachDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / approachDuration);

            transform.position = Vector3.LerpUnclamped(startPosition, edgePosition, EvaluateHopProgress(t));
            transform.rotation = Quaternion.Slerp(startRotation, faceWall, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        transform.position = edgePosition;
        transform.rotation = faceWall;
        SetWalking(false);

        PlayMove(MoveAnim.FloorToClimbDown);

        float lowerDuration = Mathf.Max(Vector3.Distance(edgePosition, hangPosition) / Mathf.Max(movementSpeed, 0.0001f), 0.01f);
        elapsed = 0f;
        while (elapsed < lowerDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lowerDuration);

            float horizontal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.45f, t));
            float vertical = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 1f, t));

            Vector3 position = Vector3.Lerp(edgePosition, new Vector3(hangPosition.x, edgePosition.y, hangPosition.z), horizontal);
            position.y = Mathf.Lerp(edgePosition.y, hangPosition.y, vertical);

            transform.position = position;
            transform.rotation = faceWall;
            yield return null;
        }

        transform.position = hangPosition;

        isMoving = false;
        CheckTile(targetTile);
    }

    IEnumerator MoveFromRoofToFloor(Tile targetTile)
    {
        isMoving = true;
        
        Vector3 targetPosition = StandPosition(targetTile, transform.forward);
        
        bool goingUp = currentTile.position.y < targetTile.position.y;
        PlayMove(goingUp ? MoveAnim.ClimbToFloorUp : MoveAnim.ClimbToFloorDown);
        
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
        
        
        SetWalking(true);

        if (goingUp)
        {
            Vector3 startPosition = transform.position;
            float duration = Vector3.Distance(startPosition, targetPosition) / Mathf.Max(movementSpeed, 0.0001f);

            nextHopDistance = duration * movementSpeed;
            PlayMove(MoveAnim.Walk);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotationSpeed
                );

                transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, EvaluateHopProgress(t));
                yield return null;
            }
        }

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
        SetWalking(false);
        
        CheckTile(targetTile);

    }
    
    IEnumerator MoveFromRoofToRoof(Tile targetTile)
    {
        Vector3 targetPosition = StandPosition(targetTile, transform.forward);
        isMoving = true;
        
        if (currentTile.position.y == targetTile.position.y)
        {
            // Izquierda o derecha vistas desde el propio personaje, que en la pared mira hacia ella.
            if (Vector3.Dot(targetTile.position - currentTile.position, transform.right) < 0f)
            {
                PlayMove(MoveAnim.ClimbLeft);
        
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
                
            }
            else
            {
                PlayMove(MoveAnim.ClimbRight);
        
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
                
            }
        }
        else if (currentTile.position.y < targetTile.position.y)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0f;
            
            PlayMove(MoveAnim.ClimbUp);
            
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
            
        }
        else
        {
            PlayMove(MoveAnim.ClimbDown);
            
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
            
        }

        isMoving = false;
        CheckTile(targetTile);
    }

    public IEnumerator RotateTowardsTarget(Vector3 targetPosition)
    {   
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed 
            );
        }
        yield return null;
    }
    protected abstract void CheckTile(Tile tile);
}
