using UnityEngine;

public class FallState : PlayerBaseState
{
    private float enterTime;

    public FallState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        enterTime = Time.time;
        if (stateMachine.Animator != null)
            stateMachine.Animator.Play("Fall");
    }

    public override void Tick(float deltaTime)
    {
        if (stateMachine.IsGrounded())
        {
            stateMachine.SwitchState(stateMachine.IdleState);
            return;
        }

        if (stateMachine.IsTouchingWall() && stateMachine.RB.linearVelocity.y <= 0)
        {
            stateMachine.SwitchState(stateMachine.WallClingState);
            return;
        }

        if (stateMachine.InputReader.IsShootPressed())
        {
            stateMachine.SwitchState(stateMachine.ShootState);
            return;
        }

        Vector2 moveInput = stateMachine.InputReader.GetMovementInput();
        float targetVelocityX = moveInput.x * stateMachine.AirControlSpeed;
        stateMachine.RB.linearVelocity = new Vector2(targetVelocityX, stateMachine.RB.linearVelocity.y);

        if (moveInput.x > 0.01f)
            stateMachine.transform.localScale = new Vector3(1, stateMachine.transform.localScale.y, stateMachine.transform.localScale.z);
        else if (moveInput.x < -0.01f)
            stateMachine.transform.localScale = new Vector3(-1, stateMachine.transform.localScale.y, stateMachine.transform.localScale.z);

        if (stateMachine.Animator != null)
        {
            stateMachine.Animator.SetFloat("Horizontal", moveInput.x);
            stateMachine.Animator.SetFloat("Vertical", moveInput.y);
        }
    }

    public override void Exit()
    {
    }
}