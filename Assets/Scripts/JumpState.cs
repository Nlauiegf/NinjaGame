using UnityEngine;

public class JumpState : PlayerBaseState
{
    private float jumpForce = 7.5f;
    private float wallJumpHorizontalForce = 5f;
    private float enterTime;
    private bool hasJumped = false;
    private bool isWallJump = false;

    public JumpState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        enterTime = Time.time;
        hasJumped = false;
        isWallJump = false;

        stateMachine.GetType().GetField("jumpGroundedGraceTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(stateMachine, 0.10f);

        if (stateMachine.Animator != null)
            stateMachine.Animator.Play("Jump");

        bool isWallJumpNow = stateMachine.IsTouchingWall() && !stateMachine.IsGrounded();

        if (isWallJumpNow)
        {
            if (stateMachine.JumpsRemaining > 0)
            {
                Vector2 wallJumpDir = GetWallJumpDirection();
                if (stateMachine.RB != null)
                {
                    stateMachine.RB.linearVelocity = Vector2.zero;
                    stateMachine.RB.AddForce(new Vector2(wallJumpDir.x * wallJumpHorizontalForce, stateMachine.WallJumpForce), ForceMode2D.Impulse);
                    hasJumped = true;
                    isWallJump = true;
                    stateMachine.JumpsRemaining = Mathf.Max(0, stateMachine.JumpsRemaining - 1);
                }
            }
        }
        else if (stateMachine.IsGrounded())
        {
            if (stateMachine.RB != null)
            {
                stateMachine.RB.linearVelocity = new Vector2(stateMachine.RB.linearVelocity.x, 0f);
                stateMachine.RB.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                hasJumped = true;
            }
        }
        else if (stateMachine.JumpsRemaining > 0)
        {
            stateMachine.JumpsRemaining = Mathf.Max(0, stateMachine.JumpsRemaining - 1);
            if (stateMachine.RB != null)
            {
                stateMachine.RB.linearVelocity = new Vector2(stateMachine.RB.linearVelocity.x, 0f);
                stateMachine.RB.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                hasJumped = true;
            }
        }
    }

    public override void Tick(float deltaTime)
    {
        if (stateMachine.InputReader.IsShootPressed())
        {
            stateMachine.SwitchState(stateMachine.ShootState);
            return;
        }

        if (stateMachine.IsDashing)
        {
            return;
        }

        Vector2 moveInputAir = stateMachine.InputReader.GetMovementInput();
        float targetVelocityX = moveInputAir.x * stateMachine.MoveSpeed;
        stateMachine.RB.linearVelocity = new Vector2(targetVelocityX, stateMachine.RB.linearVelocity.y);

        if (moveInputAir.x > 0.01f)
            stateMachine.transform.localScale = new Vector3(1, stateMachine.transform.localScale.y, stateMachine.transform.localScale.z);
        else if (moveInputAir.x < -0.01f)
            stateMachine.transform.localScale = new Vector3(-1, stateMachine.transform.localScale.y, stateMachine.transform.localScale.z);

        if (stateMachine.IsGrounded())
        {
            stateMachine.JumpsRemaining = stateMachine.MaxJumps;
            Vector2 moveInput = stateMachine.InputReader.GetMovementInput();
            if (moveInput == Vector2.zero)
                stateMachine.SwitchState(stateMachine.IdleState);
            else if (stateMachine.InputReader.IsRunPressed())
                stateMachine.SwitchState(stateMachine.RunState);
            else
                stateMachine.SwitchState(stateMachine.WalkState);
            return;
        }
    
        if (stateMachine.IsTouchingWall() && stateMachine.RB.linearVelocity.y <= 0)
        {
            stateMachine.SwitchState(stateMachine.WallClingState);
            return;
        }

        if (!stateMachine.IsGrounded() && !stateMachine.IsTouchingWall())
        {
            stateMachine.SwitchState(stateMachine.FallState);
            return;
        }

        if (stateMachine.Animator != null)
        {
            float vy = stateMachine.RB.linearVelocity.y;
            Animator anim = stateMachine.Animator;
            if (vy > 0.01f)
                anim.Play("Jump");
            else if (vy < -0.01f)
                anim.Play("Fall");
        }
    }

    public override void Exit()
    {
    }

    private Vector2 GetWallJumpDirection()
    {
        float facing = stateMachine.transform.localScale.x;
        return facing > 0 ? Vector2.left : Vector2.right;
    }
}