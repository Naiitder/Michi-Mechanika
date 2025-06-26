using System.Collections;
using UnityEngine;

public abstract class CharacterMovement : MonoBehaviour
{
    [SerializeField] public Tile currentTile;
    
    [Header("Movement")]
    protected new Transform transform;
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float rotationSpeed = 15f;
    protected bool isMoving = false;

    [Header("Animation")]
    protected Animator anim;
    [HideInInspector] public int IdleHash;
    [HideInInspector] public int WalkHash;
    [HideInInspector] public int ClimbHash;
    [HideInInspector] public int ClimbUpHash;
    [HideInInspector] public int ClimbDownHash;
    [HideInInspector] public int ClimbLeftHash;
    [HideInInspector] public int ClimbRightHash;
    [HideInInspector] public int DeadHash;
    [HideInInspector] public int AttackHash;
    
    public virtual void Initialize()
    {
        anim = GetComponent<Animator>();
        IdleHash = Animator.StringToHash("idle");
        WalkHash = Animator.StringToHash("walk");
        ClimbHash = Animator.StringToHash("climb");
        ClimbUpHash = Animator.StringToHash("climbUp");
        ClimbDownHash = Animator.StringToHash("climbDown");
        ClimbLeftHash = Animator.StringToHash("climbLeft");
        ClimbRightHash = Animator.StringToHash("climbRight");
        DeadHash = Animator.StringToHash("isDead");
        AttackHash = Animator.StringToHash("attack");
        
        transform = GetComponent<Transform>();
    }
    
    public void MoveSmoothlyTo(Tile targetTile)
    {
        
        if(currentTile is TilePression)
        {
            TilePression tp = (TilePression)currentTile;
            tp.CheckForPression(false);
        }
        
        if(currentTile.tileType == Tile.Type.Floor && targetTile.tileType == Tile.Type.Floor) StartCoroutine(MoveFromFloorToFloor(targetTile));
        else if(currentTile.tileType == Tile.Type.Floor && targetTile.tileType == Tile.Type.Roof) StartCoroutine(MoveFromFloorToRoof(targetTile));
        else if(currentTile.tileType == Tile.Type.Roof && targetTile.tileType == Tile.Type.Floor) StartCoroutine(MoveFromRoofToFloor(targetTile));
        else if(currentTile.tileType == Tile.Type.Roof && targetTile.tileType == Tile.Type.Roof) StartCoroutine(MoveFromRoofToRoof(targetTile));

    }

    IEnumerator MoveFromFloorToFloor(Tile targetTile)
    {
        isMoving = true;
        anim.SetBool(WalkHash, true);
        
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
        anim.SetBool(WalkHash, false);
        
        CheckTile(targetTile);
    }
    
    IEnumerator MoveFromFloorToRoof(Tile targetTile)
    {
        isMoving = true;
        anim.SetBool(WalkHash, true);
        
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
        anim.SetBool(WalkHash, false);

        if (currentTile.position.y < targetPosition.y)
        {
           
            anim.SetBool(ClimbUpHash, true);
            
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
            anim.SetBool(ClimbUpHash, false);
            anim.SetBool(ClimbHash, true);
            transform.position = targetPosition;

            isMoving = false;

        }
        else
        {
            anim.SetBool(ClimbDownHash, true);

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
            anim.SetBool(ClimbDownHash, false);
            anim.SetBool(ClimbHash, true);
            transform.position = targetPosition;

            isMoving = false;
        }
        
        CheckTile(targetTile);
    }
    IEnumerator MoveFromRoofToFloor(Tile targetTile)
    {
        isMoving = true;
        
        Vector3 targetPosition = targetTile.position;
        
        if(currentTile.position.y < targetPosition.y )anim.SetBool(ClimbUpHash, true);
       else anim.SetBool(ClimbDownHash, true);
        
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
        
        anim.SetBool(ClimbUpHash, false); 
        anim.SetBool(ClimbDownHash, false);
        
        anim.SetBool(WalkHash, true);
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
        anim.SetBool(WalkHash, false);
       
        CheckTile(targetTile);

    }
    
    IEnumerator MoveFromRoofToRoof(Tile targetTile)
    {
        Vector3 targetPosition = targetTile.position;
        
        if (currentTile.position.y == targetPosition.y)
        {
            if (currentTile.position.x > targetPosition.x)
            {
                isMoving = true;
                anim.SetBool(ClimbLeftHash, true);
        
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
                anim.SetBool(ClimbLeftHash, false);
            }
            else
            {
                isMoving = true;
                anim.SetBool(ClimbRightHash, true);
        
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
                anim.SetBool(ClimbRightHash, false);
            }
        }
        else if (currentTile.position.y < targetPosition.y)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0f;
            
            anim.SetBool(ClimbUpHash, true);
            
            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    movementSpeed * Time.deltaTime
                );
                yield return null;
            }
            anim.SetBool(ClimbUpHash, false);
            transform.position = targetPosition;

            isMoving = false;

        }
        else
        {
            anim.SetBool(ClimbDownHash, true);
            
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
            anim.SetBool(ClimbDownHash, false);

            isMoving = false;
        }
        
        CheckTile(targetTile);
    }
    
    protected abstract void CheckTile(Tile tile);
}
