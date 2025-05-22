using UnityEngine;

public class WallClingState : PlayerBaseState
{
    private float slideSpeed = 1.5f; // Adjust this value for desired slide speed
    private float enterTime;
    private bool jumpHeldOnEnter;

    public WallClingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        enterTime = Time.time;
        if (stateMachine.Animator != null)
            stateMachine.Animator.Play("WallCling");
    }

    public override void Tick(float deltaTime)
    {
        if (!stateMachine.IsTouchingWall() || stateMachine.IsGrounded())
        {
            stateMachine.SwitchState(stateMachine.FallState);
            return;
        }

        if (stateMachine.InputReader.IsJumpPressed() && stateMachine.JumpsRemaining > 0)
        {
            stateMachine.SwitchState(stateMachine.JumpState);
            return;
        }

        if (stateMachine.InputReader.IsShootPressed())
        {
            stateMachine.SwitchState(stateMachine.ShootState);
            return;
        }

        Vector2 moveInput = stateMachine.InputReader.GetMovementInput();
        if (moveInput.x == 0)
        {
            stateMachine.SwitchState(stateMachine.FallState);
            return;
        }

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