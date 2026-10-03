using UnityEngine;

public class ResetWalking : StateMachineBehaviour
{  
    
    private int WalkHash;
    
    
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
       WalkHash = Animator.StringToHash("willWalk");
       animator.SetBool(WalkHash, false);
    }
}
