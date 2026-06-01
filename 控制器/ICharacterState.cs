using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static UnityEngine.RuleTile.TilingRuleOutput;



public enum CharacterStateEnum
{
    Idle,
    Run,
    Jump,
    Fall,
    Attack,
    Skill,
    RunJump,
    Dodge,
    Crouch,
    Damage,
    Die
}


public interface ICharacterState
{
    CharacterStateEnum StateType { get; }
    void OnEnter();
    void OnUpdate();
    void OnFixedUpdate();  // 新增：分离物理更新
    void OnExit();
}


public abstract class CharacterStateBase : ICharacterState
{
    protected CharacterMovement character;  // 改名：charactermove -> CharacterMovement
    protected UserInput userInput;
    protected StateController stateController;
    protected CameraFollowObject followObject;
    public abstract CharacterStateEnum StateType { get; }

    // 构造函数：强制要求依赖注入
    public CharacterStateBase(CharacterMovement character, UserInput userInput, StateController stateController)
    {
        this.character = character;
        this.userInput = userInput;
        this.stateController = stateController;
    }

    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnFixedUpdate() { }
    public virtual void OnExit() { }

    // 辅助方法：处理水平移动
    protected void HandleHorizontalMovement(float multiplier = 1f)
    {
        if (Mathf.Abs(userInput.HorizontalInput) > 0.01f && !character.IsActionIgnored(ActionIgnoreTag.Move))
        {
            float targetSpeed = userInput.HorizontalInput * character.moveSpeed * multiplier;
            character.Rigidbody.velocity = new Vector2(targetSpeed, character.Rigidbody.velocity.y);
            UpdateFacingDirection();
        }
    }

    // 辅助方法：更新朝向
    protected void UpdateFacingDirection()
    {
        if (userInput.HorizontalInput > 0)
        {
            character.IsFacingRight = true;
            character.transform.localScale = new Vector3(3, 3, 3);
        }
        else if (userInput.HorizontalInput < 0)
        {
            character.IsFacingRight = false;
            character.transform.localScale = new Vector3(-3, 3, 3);
        }
        if (followObject != null)
        {
            followObject.StartFlipRotation(character.IsFacingRight);
        }
    }
}



public class JumpState : CharacterStateBase
{
    private bool _hasReachedApex;

    public override CharacterStateEnum StateType => CharacterStateEnum.Jump;

    public JumpState(CharacterMovement character, UserInput userInput, StateController stateController)
        : base(character, userInput, stateController) { }

    public override void OnEnter()
    {
        _hasReachedApex = false;

        // 施加跳跃力
        character.Rigidbody.velocity = new Vector2(
            character.Rigidbody.velocity.x,
            character.jumpForce
        );

        // 添加动作屏蔽
        character.AddActionIgnore(0.1f, ActionIgnoreTag.Move);

        character.Animator.SetBool("Is Jumping", true);
        character.Animator.Play("Jump");
    }

    public override void OnUpdate()
    {
        // 检查是否到达最高点
        if (character.Rigidbody.velocity.y <= 0 && !_hasReachedApex)
        {
            _hasReachedApex = true;
            stateController.ChangeState(CharacterStateEnum.Fall);
            return;
        }

        // 空中水平控制（减弱）
        HandleHorizontalMovement(0.8f);
    }

    public override void OnExit()
    {
        character.Animator.SetBool("Is Jumping", false);
    }
}
public class FallState : CharacterStateBase
{
    private bool _hasLanded;

    public override CharacterStateEnum StateType => CharacterStateEnum.Fall;

    public FallState(CharacterMovement character, UserInput userInput, StateController stateController)
        : base(character, userInput, stateController) { }

    public override void OnEnter()
    {
        _hasLanded = false;
        character.Animator.SetBool("Is Falling", true);
        character.Animator.Play("Fall");
    }

    public override void OnUpdate()
    {
        // 空中水平控制
        HandleHorizontalMovement(0.7f);

        // 落地检测
        if (character.IsGrounded && !_hasLanded)
        {
            _hasLanded = true;
            character.Animator.SetBool("Is Falling", false);

            // 根据是否有输入决定切换到Run还是Idle
            stateController.ChangeState(
                Mathf.Abs(userInput.HorizontalInput) > 0.01f ?
                CharacterStateEnum.Run : CharacterStateEnum.Idle
            );
        }
    }

    public override void OnExit()
    {
        character.Animator.SetBool("Is Falling", false);
    }
}
public class IdleState : CharacterStateBase
{
    public override CharacterStateEnum StateType => CharacterStateEnum.Idle;

    public IdleState(CharacterMovement character, UserInput userInput, StateController stateController)
        : base(character, userInput, stateController) { }

    public override void OnEnter()
    {
      
        character.Animator.SetBool("Is Idle", true);
        character.Animator.SetBool("Is Running", false);
        character.Animator.SetBool("Is Jumping", false);
        character.Animator.SetBool("Is Falling", false);
        
    }

    public override void OnUpdate()
    {
        // 检查是否应该离开Idle状态
        if (!character.IsGrounded)
        {
            stateController.ChangeState(CharacterStateEnum.Fall);
            return;
        }

        if (Mathf.Abs(userInput.HorizontalInput) > 0.01f)
        {
            stateController.ChangeState(CharacterStateEnum.Run);
        }
    }
}
public class RunState : CharacterStateBase
{
    public override CharacterStateEnum StateType => CharacterStateEnum.Run;

    public RunState(CharacterMovement character, UserInput userInput, StateController stateController)
        : base(character, userInput, stateController) { }

    public override void OnEnter()
    {
        character.Animator.SetBool("Is Running", true);
        character.Animator.Play("startrun");
    }

    public override void OnUpdate()
    {
        // 跳跃检查 - 先检查是否被屏蔽
        if (userInput.IsJumpPressed && !character.IsActionIgnored(ActionIgnoreTag.Jump))
        {
            stateController.ChangeState(CharacterStateEnum.RunJump);
            return;
        }

        
        // 下落通常不应该被屏蔽，但如果是硬直状态可能需要
        if (!character.IsGrounded && !character.IsActionIgnored(ActionIgnoreTag.Move))
        {
            stateController.ChangeState(CharacterStateEnum.Fall);
            return;
        }

        // 移动处理
        HandleHorizontalMovement();

        // 停止检查
        if (Mathf.Abs(userInput.HorizontalInput) <= 0.01f && !character.IsActionIgnored(ActionIgnoreTag.Move))
        {
            character.Animator.SetBool("Is Running", false);
            character.Animator.Play("stoprun");
            stateController.ChangeState(CharacterStateEnum.Idle);
        }
    }

    public override void OnExit()
    {
      
        if (character.IsGrounded)
        {
           
        }
       
    }
}
public class RunJumpState : CharacterStateBase
{
    private bool _hasReachedApex;
    private float _airControlBoost = 1.1f;

    public override CharacterStateEnum StateType => CharacterStateEnum.RunJump;

    public RunJumpState(CharacterMovement character, UserInput userInput, StateController stateController)
        : base(character, userInput, stateController) { }

    public override void OnEnter()
    {
        _hasReachedApex = false;
        character.Animator.SetBool("Is Run Jumping", true);
        character.Animator.Play("RunJump");
        // 关键：进入RunJump时屏蔽状态切换
        // 屏蔽所有可能干扰的状态（0.2秒内不允许切换到其他状态）
        character.AddActionIgnore(0.1f, ActionIgnoreTag.All  // 防止攻击打断
                                                             // 可以根据需要添加更多
        );

        // 根据输入方向施加跳跃力
        float horizontalVelocity = userInput.HorizontalInput * character.moveSpeed;
        character.Rigidbody.velocity = new Vector2(horizontalVelocity, character.jumpForce);

        UpdateFacingDirection();



        Debug.Log("[RunJump] 进入状态，添加动作屏蔽0.2秒");
    }

    public override void OnUpdate()
    {
        // 空中水平加速
        if (Mathf.Abs(userInput.HorizontalInput) > 0.01f)
        {
            float newHorizontalSpeed = character.Rigidbody.velocity.x +
                userInput.HorizontalInput * _airControlBoost * Time.deltaTime;
            character.Rigidbody.velocity = new Vector2(newHorizontalSpeed, character.Rigidbody.velocity.y);
            UpdateFacingDirection();
        }

        // 到达最高点检测
        if (character.Rigidbody.velocity.y <= 0 && !_hasReachedApex)
        {
            _hasReachedApex = true;
        }

        // 落地检测 - 检查是否被屏蔽
        if (character.IsGrounded && !character.IsActionIgnored(ActionIgnoreTag.Move))
        {
            Debug.Log("[RunJump] 落地，屏蔽已解除");

            CharacterStateEnum nextState = Mathf.Abs(userInput.HorizontalInput) > 0.01f ?
                CharacterStateEnum.Run : CharacterStateEnum.Idle;

            stateController.ChangeState(nextState);
        }
    }

    public override void OnExit()
    {
        character.Animator.SetBool("Is Run Jumping", false);
        character.Animator.SetBool("Is Jumping", false);
        if (character.IsGrounded)
        {

            character.Animator.Play("characterIdle");


        }
    }

   
}
public class DodgeState : CharacterStateBase
{
    // ---------- 1. 基础属性 ----------
    // 闪避的持续时间（秒）
    private const float DODGE_DURATION = 0.72f;
    // 闪避过程中的水平速度倍率（相对于角色原始速度）
    private const float DODGE_SPEED_MULTIPLIER = 2.5f;
    // 是否已经结束了闪避动作
    private bool _isDodgeFinished = false;

    // ---------- 2. 构造函数 ----------
    public DodgeState(CharacterMovement character, UserInput userInput, StateController stateController)
        : base(character, userInput, stateController) { }

    // ---------- 3. 状态标识 ----------
    public override CharacterStateEnum StateType => CharacterStateEnum.Dodge;

    // ---------- 4. 进入状态 ----------
    public override void OnEnter()
    {
        character.Animator.Play("dodge");
        // 1. 重置标记
        _isDodgeFinished = false;

        // 2.设置碰撞体
        CharacterMovement.Instance.Idle_collider.GetComponent<Collider2D>().enabled = false;
        CharacterMovement.Instance.Dodge_collider.GetComponent<Collider2D>().enabled = true;

        // 3. 设置动作屏蔽（0.4秒内不允许切换到攻击或移动）
        character.AddActionIgnore(0.5f,
            ActionIgnoreTag.Attack,   // 防止被攻击打断
            ActionIgnoreTag.Move,     // 防止在闪避途中被迫停止
            ActionIgnoreTag.Dodge,
            ActionIgnoreTag.crouch// 防止连续闪避叠加
        );

        // 4. 计算闪避方向（根据玩家当前输入或角色朝向）
        float dodgeDirection = userInput.HorizontalInput != 0
            ? Mathf.Sign(userInput.HorizontalInput) // 按住方向键闪避
            : Mathf.Sign(character.transform.localScale.x); // 没有输入则向角色当前朝向闪避

        // 5. 施加水平冲量（瞬间位移）
        Vector2 dodgeVelocity = new Vector2(dodgeDirection * character.moveSpeed * DODGE_SPEED_MULTIPLIER, character.Rigidbody.velocity.y);
        character.Rigidbody.velocity = dodgeVelocity;

        // 6. 调整角色朝向
        if (dodgeDirection > 0) character.transform.localScale = new Vector3(3, 3, 3);
        else if (dodgeDirection < 0) character.transform.localScale = new Vector3(-3, 3, 3);

        // 7. 设置结束计时器（0.35秒后自动结束）
        character.StartCoroutine(EndDodgeAfterDelay(DODGE_DURATION - 0.18f));


        Debug.Log("[Dodge] 进入闪避状态，方向：" + dodgeDirection);
    }

    // ---------- 8. 闪避结束计时 ----------
    private System.Collections.IEnumerator EndDodgeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _isDodgeFinished = true;
    }

    // ---------- 9. 更新逻辑 ----------
    public override void OnUpdate()
    {
        if (userInput.IsJumpPressed && character.IsGrounded && !character.IsActionIgnored(ActionIgnoreTag.Jump))
        {
            // 跳跃逻辑：立即切换到 Jump 状态
            stateController.ChangeState(CharacterStateEnum.RunJump);
            return; // 跳跃后退出当前的 Update，防止后面的逻辑干扰
        }
        // 1. 结束检测：如果闪避时间到了
        if (_isDodgeFinished && !character.IsActionIgnored(ActionIgnoreTag.Dodge))
        {
            // 2. 根据当前输入决定落地后的状态
            CharacterStateEnum nextState;
            if (!character.IsGrounded)
            {
                // 空中结束：回到跳跃状态
                nextState = CharacterStateEnum.Fall;
            }
            else
            {
                // 地面结束：根据是否有水平输入决定是跑动还是待机
                nextState = Mathf.Abs(userInput.HorizontalInput) > 0.01f
                    ? CharacterStateEnum.Run
                    : CharacterStateEnum.Idle;
            }

            stateController.ChangeState(nextState);
        }

        // 2. 防止在闪避期间被外部代码误改方向（可选）
        // character.Rigidbody.velocity = new Vector2(Mathf.Sign(character.transform.localScale.x) * character.moveSpeed * DODGE_SPEED_MULTIPLIER, character.Rigidbody.velocity.y);
    }

    // ---------- 10. 退出状态 ----------
    public override void OnExit()
    {
        // 1. 重置动画参数
        character.Animator.ResetTrigger("Is Dodge");

        CharacterMovement.Instance.Idle_collider.GetComponent<Collider2D>().enabled = true;
        CharacterMovement.Instance.Dodge_collider.GetComponent<Collider2D>().enabled = false;
        // 2. 确保水平速度恢复正常（防止残留冲量）
        if (character.IsGrounded)
        {
            character.Rigidbody.velocity = new Vector2(0, character.Rigidbody.velocity.y);
        }


    }
}


public class AttackState : CharacterStateBase
{
    private const int maxCombo = 3;
    private const float attackDuration = 0.35f;
    private const float comboWindow = 0.2f;

    private int currentCombo = 0;
    private float attackStartTime = 0f;
    private bool hasExitedComboWindow = false;  // 是否已退出连击窗口

    public override CharacterStateEnum StateType => CharacterStateEnum.Attack;

    public AttackState(CharacterMovement character, UserInput userInput, StateController stateController)
        : base(character, userInput, stateController) { }

    public override void OnEnter()
    {
        // 直接从第1段开始
        currentCombo = 1;
        attackStartTime = Time.time;
        hasExitedComboWindow = false;

        // 锁定动作
        character.AddActionIgnore(attackDuration,
            ActionIgnoreTag.Move,
            ActionIgnoreTag.Jump,
            ActionIgnoreTag.Attack);

        // 播放动画
        character.Animator.SetBool("Is Attacking", true);
        character.Animator.SetInteger("BasicAttackIndex", currentCombo);
        character.Animator.Play($"attack{currentCombo}");

        Debug.Log($"[Attack] 第 {currentCombo} 段攻击开始");
    }

    public override void OnUpdate()
    {
        // 当前攻击段是否结束？
        if (Time.time - attackStartTime >= attackDuration)
        {
            // 已经退出连击窗口？
            if (hasExitedComboWindow) return;

            float timeSinceAttackEnd = Time.time - (attackStartTime + attackDuration);

            // 如果在连击窗口内
            if (timeSinceAttackEnd <= comboWindow)
            {
               
                if (currentCombo < maxCombo && userInput.AttackPressed)
                {
                    // 触发下一段连击
                    currentCombo++;
                    attackStartTime = Time.time;

                    // 重新锁定动作
                    character.AddActionIgnore(attackDuration,
                        ActionIgnoreTag.Move,
                        ActionIgnoreTag.Jump,
                        ActionIgnoreTag.Attack);

                    // 播放下一段动画
                    character.Animator.SetInteger("BasicAttackIndex", currentCombo);
                    character.Animator.Play($"attack{currentCombo}");

                    Debug.Log($"[Attack] 连击! 第 {currentCombo} 段");
                }
            }
            else if (!hasExitedComboWindow)
            {
                // 超过窗口期，退出攻击
                hasExitedComboWindow = true;
                ExitAttack();
            }
        }
    }

    private void ExitAttack()
    {
        // 关闭攻击动画
        character.Animator.SetBool("Is Attacking", false);

        // 根据地面状态切换
        if (character.IsGrounded)
        {
            if (Mathf.Abs(userInput.HorizontalInput) > 0.01f)
                stateController.ChangeState(CharacterStateEnum.Run);
            else
                stateController.ChangeState(CharacterStateEnum.Idle);
        }
        else
        {
            stateController.ChangeState(CharacterStateEnum.Fall);
        }

        Debug.Log("[Attack] 攻击结束");
    }

    public override void OnExit()
    {
        Debug.Log("1111");
        character.Animator.SetBool("Is Attacking", false);
        character.Animator.SetInteger("BasicAttackIndex", 0);
         
    }
}
public class CrouchState : CharacterStateBase
{
    // 标识当前状态为 Crouch
    private const float CrouchDuration = 0.1f;
    public override CharacterStateEnum StateType => CharacterStateEnum.Crouch;

    // 构造函数，保持依赖注入
    public CrouchState(CharacterMovement character, UserInput userInput, StateController stateController)
        : base(character, userInput, stateController) { }

    // 进入状态时调用
    public override void OnEnter()
    {
   
        character.Animator.Play("Crouch");
        character.Animator.SetBool("Is Crouching", true);

        character.AddActionIgnore(CrouchDuration,
            ActionIgnoreTag.Move,
            ActionIgnoreTag.Jump,
            ActionIgnoreTag.Attack);
        if (character.Crouch_collider != null && character.Crouch_collider != null)
        {

            character.Crouch_collider.enabled = true;
            character.Idle_collider.enabled = false;
           
           
        }

        
        
    }

    // 每帧更新调用
    public override void OnUpdate()
    {
        if (userInput.IsCrouchPressed) {
            character.AddActionIgnore(0.1f, ActionIgnoreTag.All);
        }
        if (!userInput.IsCrouchPressed)
        {
            
            
            CharacterStateEnum nextState = character.IsGrounded ?
                (Mathf.Abs(userInput.HorizontalInput) > 0.01f ? CharacterStateEnum.Run : CharacterStateEnum.Idle) :
                CharacterStateEnum.Fall;

            stateController.ChangeState(nextState);
            return;
        }

        

    }

    // 退出状态时调用
    public override void OnExit()
    {
        Debug.Log("222");
        // 1. 重置动画参数
        character.Animator.SetBool("Is Crouching", false);
 
       
        // 2. 恢复原始碰撞体
        if (character.Crouch_collider != null && character.Crouch_collider != null)
        {
            character.Crouch_collider.enabled = false;
            character.Idle_collider.enabled = true;
        }

        
    }
}

public class DamageState : CharacterStateBase
{
    private const float HurtDuration = 1.1f;
    private const float HurtInvincibilityTime = 0.3f;
    private const float HurtForce = 2.5f;
    
    public override CharacterStateEnum StateType => CharacterStateEnum.Damage;
    
    private float hurtTimer = 3.5f;
    private float invincibilityTimer = 2f;
    private Vector3 hurtDirection;

    public DamageState(CharacterMovement character, UserInput userInput, 
                       StateController stateController, Vector3 hurtSourcePosition)
        : base(character, userInput, stateController) 
    {
        Vector3 direction = (character.transform.position - hurtSourcePosition);
        direction.y = 0f;
        hurtDirection = direction.normalized;
    }

    public override void OnEnter()
    {
        
        hurtTimer = HurtDuration;
        invincibilityTimer = HurtInvincibilityTime;
        Vector3 scale = character.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(-hurtDirection.x);
        character.transform.localScale = scale;
        character.Animator.Play("hurt");
        character.Animator.SetBool("Is Hurt", true);
        character.AddActionIgnore(HurtDuration,
            ActionIgnoreTag.Move, ActionIgnoreTag.Jump, 
            ActionIgnoreTag.Attack, ActionIgnoreTag.Interact);

    }

    public override void OnUpdate()
    {
        hurtTimer -= Time.deltaTime;
        invincibilityTimer -= Time.deltaTime;
        
        if (invincibilityTimer <= 0f)
            //character.Animator.SetBool("Is Invincible", false);

        if (hurtTimer > 0f)
        {
          
            character.Rigidbody.velocity = new Vector3(hurtDirection.x, 
                0, hurtDirection.z)*HurtForce;
         
        }

        if (hurtTimer <= 0f)
        {
            CharacterStateEnum nextState = character.IsGrounded ?
                (Mathf.Abs(userInput.HorizontalInput) > 0.01f ? 
                    CharacterStateEnum.Run : CharacterStateEnum.Idle) :
                CharacterStateEnum.Fall;
            stateController.ChangeState(nextState);
            return;
        }
    }

    public override void OnExit()
    {
        character.Animator.SetBool("Is Hurt", false);
        hurtTimer = 0f;
        invincibilityTimer = 0f;
    }
}

public class DeathState : CharacterStateBase
{
    private const float DeathDuration = 1.5f;
    private const float RespawnTime = 2.0f;

    public override CharacterStateEnum StateType => CharacterStateEnum.Die;

    private float deathTimer = DeathDuration;
    private float responeTimer = RespawnTime;
    private bool isDying;

    public DeathState(CharacterMovement character, UserInput userInput,
                       StateController stateController)
        : base(character, userInput, stateController)
    {
        isDying = false;
    }

    public override void OnEnter()
    {
        deathTimer = DeathDuration;
        isDying = true;
        character.Rigidbody.velocity = Vector3.zero;     // 清除速度
           
        character.Animator.Play("Death");
        character.Animator.SetBool("Is active", true);
        character.AddActionIgnore(DeathDuration,
          ActionIgnoreTag.All);

        
    }

    public override void OnUpdate()
    {
        deathTimer -= Time.deltaTime;
        responeTimer -= Time.deltaTime;

      
        if (deathTimer <= 0f && isDying)
        {
            isDying = false;
        }

       
        if (responeTimer <= 0f)
        {
            stateController.ChangeState(CharacterStateEnum.Idle);
            return;
        }
    }

    public override void OnExit()
    {
        character.Animator.SetBool("Is Dead", false);
    }
}
