using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    public Animator anim;
    
    [Header("Animation's Hash")]
    [HideInInspector] public int IdleHash;
    [HideInInspector] public int WalkHash;
    [HideInInspector] public int ClimbHash;

    public void Initialize()
    {
        anim = GetComponent<Animator>();
        IdleHash = Animator.StringToHash("idle");
        WalkHash = Animator.StringToHash("walk");
        ClimbHash = Animator.StringToHash("climb");
    }
}
