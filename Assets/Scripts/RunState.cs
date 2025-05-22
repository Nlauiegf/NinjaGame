using UnityEngine;

public class RunState : PlayerBaseState
{
    private float enterTime;

    public RunState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        enterTime = Time.time;
        if (stateMachine.Animator != null)
            stateMachine.Animator.Play("Dash");
    }

    public override void Tick(float deltaTime)
    {
        if (!stateMachine.IsGrounded())
        {
            if (stateMachine.IsTouchingWall() && stateMachine.RB.linearVelocity.y <= 0)
            {
                stateMachine.SwitchState(stateMachine.WallClingState);
            }
            else
            {
                stateMachine.SwitchState(stateMachine.FallState);
            }
            return;
        }

        if (stateMachine.InputReader.IsShootPressed())
        {
            stateMachine.SwitchState(stateMachine.ShootState);
            return;
        }

        Vector2 moveInput = stateMachine.InputReader.GetMovementInput();

        if (stateMachine.IsGrounded() && stateMachine.InputReader.IsCrouchHeld())
        {
            stateMachine.SwitchState(stateMachine.SlideState);
            return;
        }

        if (stateMachine.InputReader.IsJumpPressed() && stateMachine.JumpsRemaining > 0)
        {
            stateMachine.SwitchState(stateMachine.JumpState);
            return;
        }

        if (stateMachine.InputReader.IsRunPressed()) {
            stateMachine.SwitchState(stateMachine.RunState);
        }
        else
        {
            stateMachine.SwitchState(stateMachine.WalkState);
            return;
        }

        if (moveInput == Vector2.zero)
        {
            stateMachine.SwitchState(stateMachine.IdleState);
            return;
        }

        float targetVelocityX = moveInput.x * stateMachine.MoveSpeed;
        stateMachine.RB.linearVelocity = new Vector2(targetVelocityX, stateMachine.RB.linearVelocity.y);

        if (moveInput.x > 0.01f)
            stateMachine.transform.localScale = new Vector3(1, stateMachine.transform.localScale.y, stateMachine.transform.localScale.z);
        else if (moveInput.x < -0.01f)
            stateMachine.transform.localScale = new Vector3(-1, stateMachine.transform.localScale.y, stateMachine.transform.localScale.z);

        if (stateMachine.Animator != null && !stateMachine.Animator.GetCurrentAnimatorStateInfo(0).IsName("Dash"))
            stateMachine.Animator.Play("Dash");

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