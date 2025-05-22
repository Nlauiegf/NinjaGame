using UnityEngine;

public class CrouchState : PlayerBaseState
{
    private float enterTime;
    private float crouchMoveSpeed;

    public CrouchState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        // Calculate actual crouch speed based on multipliers
        // Walk speed is 0.5 * MoveSpeed, Crouch is half of Walk speed (0.25 * MoveSpeed)
        crouchMoveSpeed = stateMachine.MoveSpeed * stateMachine.CrouchSpeedMultiplier;
    }

    public override void Enter()
    {
        enterTime = Time.time;
        if (stateMachine.Animator != null)
            stateMachine.Animator.Play("Crouch");
    }

    public override void Tick(float deltaTime)
    {
        if (!stateMachine.IsGrounded())
        {
            stateMachine.SwitchState(stateMachine.FallState);
            return;
        }

        if (stateMachine.InputReader.IsShootPressed())
        {
            stateMachine.SwitchState(stateMachine.ShootState);
            return;
        }

        Vector2 moveInput = stateMachine.InputReader.GetMovementInput();

        if (!stateMachine.InputReader.IsCrouchHeld())
        {
            if (moveInput.x != 0)
            {
                stateMachine.SwitchState(stateMachine.WalkState);
            }
            else
            {
                stateMachine.SwitchState(stateMachine.IdleState);
            }
            return;
        }

        float targetVelocityX = moveInput.x * stateMachine.CrouchSpeed;
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