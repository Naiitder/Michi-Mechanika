using System;
using UnityEngine;

public class ResetAnimation : StateMachineBehaviour
{
    private int AttackHash;
    
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        AttackHash = Animator.StringToHash("attack");
        animator.SetBool(AttackHash,false);
    }
}
