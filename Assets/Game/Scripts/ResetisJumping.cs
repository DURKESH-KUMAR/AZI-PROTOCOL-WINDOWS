using UnityEngine;

public class ResetisJumping : StateMachineBehaviour
{
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("isJumping", false);
       
    }
}
