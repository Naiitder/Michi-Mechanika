using System;
using UnityEngine;

public class ResetAnimation : StateMachineBehaviour
{
    private int AttackHash;

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        AttackHash = Animator.StringToHash("attack");
        animator.SetBool(AttackHash,false);
    }
}
