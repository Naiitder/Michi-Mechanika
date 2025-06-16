using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    public Animator anim;
    
    [Header("Animation's Hash")]
    [HideInInspector] public int IdleHash;
    [HideInInspector] public int WalkHash;
    [HideInInspector] public int ClimbHash;
    [HideInInspector] public int ClimbUpHash;
    [HideInInspector] public int ClimbDownHash;
    [HideInInspector] public int ClimbLeftHash;
    [HideInInspector] public int ClimbRightHash;

    public void Initialize()
    {
        anim = GetComponent<Animator>();
        IdleHash = Animator.StringToHash("idle");
        WalkHash = Animator.StringToHash("walk");
        ClimbHash = Animator.StringToHash("climb");
        ClimbUpHash = Animator.StringToHash("climbUp");
        ClimbDownHash = Animator.StringToHash("climbDown");
        ClimbLeftHash = Animator.StringToHash("climbLeft");
        ClimbRightHash = Animator.StringToHash("climbRight");
    }
}
