using UnityEngine;
using GabrielBigardi.SpriteAnimator;

public class PlayerAnimationController : MonoBehaviour
{
    public SpriteAnimator playerAnim;
    public SpriteAnimator playerEffect;

    public void IdleAnim()
    {
        playerAnim.PlayIfNotPlaying("Idle");
    }
    public void MovingAnim()
    {
        playerAnim.PlayIfNotPlaying("Move");
    }

    public void AttackAnim()
    {
        playerAnim.PlayIfNotPlaying("Attack");
    }
    public void DashAnim()
    {
        playerAnim.PlayIfNotPlaying("Dash");
    }
}

