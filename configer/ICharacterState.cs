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
    /// <summary>
    /// 状态自报：返回想要切换到的目标状态（null = 留在本状态）。
    /// 切换裁决（动作屏蔽、优先级）统一由 StateController 执行。
    /// </summary>
    CharacterStateEnum? OnUpdate();
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
    public virtual CharacterStateEnum? OnUpdate() { return null; }
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
        AudioManager.Instance?.PlayJumpSFX();
    }

    public override CharacterStateEnum? OnUpdate()
    {
        // 检查是否到达最高点
        if (character.Rigidbody.velocity.y <= 0 && !_hasReachedApex)
        {
            _hasReachedApex = true;
            return CharacterStateEnum.Fall;
        }

        // 空中水平控制（减弱）
        HandleHorizontalMovement(0.8f);
        return null;
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

    public override CharacterStateEnum? OnUpdate()
    {
        // 空中水平控制
        HandleHorizontalMovement(0.7f);

        // 落地检测
        if (character.IsGrounded && !_hasLanded)
        {
            _hasLanded = true;
            character.Animator.SetBool("Is Falling", false);
            AudioManager.Instance?.PlayLandSFX();

            // 根据是否有输入决定回到Run还是Idle
            return stateController.GetRunOrIdle();
        }
        return null;
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

    public override CharacterStateEnum? OnUpdate()
    {
        // 检查是否应该离开Idle状态
        if (!character.IsGrounded)
        {
            return CharacterStateEnum.Fall;
        }

        if (!userInput.stop && Mathf.Abs(userInput.HorizontalInput) > 0.01f)
        {
            return CharacterStateEnum.Run;
        }
        return null;
    }
}
public class RunState : CharacterStateBase
{
    private float _stepTimer;
    private const float StepInterval = 0.3f;
    public override CharacterStateEnum StateType => CharacterStateEnum.Run;

    public RunState(CharacterMovement character, UserInput userInput, StateController stateController)
        : base(character, userInput, stateController) { }

    public override void OnEnter()
    {
        character.Animator.SetBool("Is Running", true);
        character.Animator.Play("startrun");
    }

    public override CharacterStateEnum? OnUpdate()
    {
        if (!character.IsGrounded)
        {
            return CharacterStateEnum.Fall;
        }

        // 移动处理
        HandleHorizontalMovement();
        if (!userInput.stop && character.IsGrounded && Mathf.Abs(userInput.HorizontalInput) > 0.01f)
        {
            _stepTimer -= TimeManager.GameplayDT;
            if (_stepTimer <= 0f)
            {
                AudioManager.Instance?.PlayMoveSFX();
                _stepTimer = StepInterval;
            }
        }
        else
        {
            _stepTimer = 0f;
            AudioManager.Instance?.StopMoveSFX();
        }
        // 停止检查
        if (Mathf.Abs(userInput.HorizontalInput) <= 0.01f)
        {
            AudioManager.Instance?.StopMoveSFX();
            character.Animator.SetBool("Is Running", false);
            character.Animator.Play("stoprun");
            return CharacterStateEnum.Idle;
        }

        return null;
    }

    public override void OnExit()
    {
        AudioManager.Instance?.StopMoveSFX();
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
        character.Animator.SetBool("Is Jumping", true);  // 与 JumpState 一致，避免动画机回退到 Fall
        character.Animator.Play("RunJump");
        AudioManager.Instance?.PlayJumpSFX();
        // 关键：进入RunJump时短暂屏蔽外部请求（0.2秒），防止跳跃瞬间被打断。
        // 注意：不再屏蔽 Damage——受伤应该能正常打断任何状态（无敌帧由 DamageState 自己提供）
        character.AddActionIgnore(0.2f,
            ActionIgnoreTag.Jump,
            ActionIgnoreTag.Attack,
            ActionIgnoreTag.crouch,
            ActionIgnoreTag.Move
        );
        // 根据输入方向施加跳跃力
        float horizontalVelocity = userInput.HorizontalInput * character.moveSpeed;
        character.Rigidbody.velocity = new Vector2(horizontalVelocity, character.jumpForce);

        UpdateFacingDirection();

        Debug.Log("[RunJump] 进入状态，添加动作屏蔽");
    }

    public override CharacterStateEnum? OnUpdate()
    {
        // 空中水平加速
        if (Mathf.Abs(userInput.HorizontalInput) > 0.01f)
        {
            float newHorizontalSpeed = character.Rigidbody.velocity.x +
                userInput.HorizontalInput * _airControlBoost * TimeManager.GameplayDT;
            character.Rigidbody.velocity = new Vector2(newHorizontalSpeed, character.Rigidbody.velocity.y);
            UpdateFacingDirection();
        }

        // 到达最高点检测
        if (character.Rigidbody.velocity.y <= 0 && !_hasReachedApex)
        {
            _hasReachedApex = true;
        }

        // 落地检测：过了最高点（开始下落）后才允许落地。
        // 起跳头几帧地面 raycast 还没脱离判定范围（半径+0.15 余量），
        // 直接判 IsGrounded 会把跳跃立刻打断（表现为只播 Fall 动画）
        if (_hasReachedApex && character.IsGrounded)
        {
            Debug.Log("[RunJump] 落地");
            AudioManager.Instance?.PlayLandSFX();

            return stateController.GetRunOrIdle();
        }
        return null;
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
        AudioManager.Instance?.PlayDodgeSFX();
        // 1. 重置标记
        _isDodgeFinished = false;

        // 2.设置碰撞体（用注入的 character，而不是全局单例，避免多实例/场景重建时操作错对象）
        character.Idle_collider.GetComponent<Collider2D>().enabled = false;
        character.Dodge_collider.GetComponent<Collider2D>().enabled = true;

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

        // 7. 设置结束计时器
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
    // 闪避中的跳跃已收归 HandleJumpRequest（事件统一入口），这里只负责自然结束
    public override CharacterStateEnum? OnUpdate()
    {
        // 结束检测：如果闪避时间到了
        if (_isDodgeFinished)
        {
            // 根据是否在地面决定回 Fall 还是 Run/Idle
            return !character.IsGrounded
                ? CharacterStateEnum.Fall
                : stateController.GetRunOrIdle();
        }

        // 防止在闪避期间被外部代码误改方向（可选）
        // character.Rigidbody.velocity = new Vector2(Mathf.Sign(character.transform.localScale.x) * character.moveSpeed * DODGE_SPEED_MULTIPLIER, character.Rigidbody.velocity.y);
        return null;
    }

    // ---------- 10. 退出状态 ----------
    public override void OnExit()
    {
        // 1. 重置动画参数
        character.Animator.ResetTrigger("Is Dodge");

        character.Idle_collider.GetComponent<Collider2D>().enabled = true;
        character.Dodge_collider.GetComponent<Collider2D>().enabled = false;
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
    private float segmentTimer;     // 当前攻击段剩余时间
    private float hitTimer;         // 距离出伤点的时间
    private float windowTimer;      // 连击窗口剩余时间
    private bool hasExitedComboWindow = false;
    private bool hasAppliedDamage;
    private bool attackBuffered;    // 攻击期间缓冲的按击（解决"提前按被吞"）
    private bool attackHeldLast;    // 上一帧是否按住攻击键（缓冲只认上升沿）
    private readonly PlayerAttackDealer attackDealer;

    public override CharacterStateEnum StateType => CharacterStateEnum.Attack;

    public AttackState(CharacterMovement character, UserInput userInput, StateController stateController)
        : base(character, userInput, stateController)
    {
        attackDealer = character.GetComponent<PlayerAttackDealer>();
    }

    public override void OnEnter()
    {
        // 直接从第1段开始
        currentCombo = 1;
        hasExitedComboWindow = false;
        hasAppliedDamage = false;
        attackBuffered = false;
        // 把"进入攻击的那一下按键"视为已消费，避免一次按键自动打出两段连击
        attackHeldLast = true;
        segmentTimer = attackDuration;
        hitTimer = GetHitTime(currentCombo);
        windowTimer = comboWindow;   // 必须初始化：否则攻击段一结束窗口就过期，连击永远打不出

        // 锁定动作
        character.AddActionIgnore(attackDuration,
            ActionIgnoreTag.Move,
            ActionIgnoreTag.Jump,
            ActionIgnoreTag.Attack);

        // 播放动画
        character.Animator.SetBool("Is Attacking", true);
        character.Animator.SetInteger("BasicAttackIndex", currentCombo);
        character.Animator.Play($"attack{currentCombo}");
        AudioManager.Instance?.PlayAttackSwing();

        Debug.Log($"[Attack] 第 {currentCombo} 段攻击开始");
    }

    public override CharacterStateEnum? OnUpdate()
    {
        float dt = TimeManager.GameplayDT;

        // 只缓冲"新"的按击（上升沿），避免"提前按被吞"；
        // 进入攻击的那一下按键视为已消费，否则一次按键会在攻击段结束后自动触发下一段
        if (userInput.AttackPressed && !attackHeldLast)
            attackBuffered = true;
        attackHeldLast = userInput.AttackPressed;

        if (!hasAppliedDamage)
        {
            hitTimer -= dt;
            if (hitTimer <= 0f)
            {
                attackDealer?.DealDamage(currentCombo);
                hasAppliedDamage = true;
            }
        }

        segmentTimer -= dt;
        if (segmentTimer > 0f)
        {
            return null;
        }

        // 当前攻击段已结束，进入连击窗口
        if (hasExitedComboWindow) return null;

        windowTimer -= dt;

        if (currentCombo < maxCombo && attackBuffered)
        {
            // 触发下一段连击
            attackBuffered = false;
            currentCombo++;
            segmentTimer = attackDuration;
            hitTimer = GetHitTime(currentCombo);
            hasAppliedDamage = false;
            windowTimer = comboWindow;

            // 重新锁定动作
            character.AddActionIgnore(attackDuration,
                ActionIgnoreTag.Move,
                ActionIgnoreTag.Jump,
                ActionIgnoreTag.Attack);

            // 播放下一段动画
            character.Animator.SetInteger("BasicAttackIndex", currentCombo);
            character.Animator.Play($"attack{currentCombo}");
            if (currentCombo == maxCombo)
            {
                AudioManager.Instance?.PlayAttackFinisher();  // 最大连击终结音效
            }
            else
            {
                AudioManager.Instance?.PlayAttackSwing();
            }

            Debug.Log($"[Attack] 连击! 第 {currentCombo} 段");
            return null;
        }

        if (windowTimer <= 0f)
        {
            // 窗口期结束，自报退出攻击（动画参数由 OnExit 统一清理）
            hasExitedComboWindow = true;
            Debug.Log("[Attack] 攻击结束");
            return character.IsGrounded ? stateController.GetRunOrIdle() : CharacterStateEnum.Fall;
        }

        return null;
    }

    private float GetHitTime(int comboIndex)
    {
        switch (comboIndex)
        {
            case 1:
                return 0.12f;
            case 2:
                return 0.14f;
            case 3:
                return 0.16f;
            default:
                return 0.12f;
        }
    }

    public override void OnExit()
    {
        character.Animator.SetBool("Is Attacking", false);
        character.Animator.SetInteger("BasicAttackIndex", 0);
        hasAppliedDamage = false;
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
        AudioManager.Instance?.PlayCrouchSFX();
        character.Animator.SetBool("Is Crouching", true);

        character.AddActionIgnore(CrouchDuration,
            ActionIgnoreTag.Move,
            ActionIgnoreTag.Jump,
            ActionIgnoreTag.Attack);
        if (character.Crouch_collider != null && character.Idle_collider != null)
        {
            character.Crouch_collider.enabled = true;
            character.Idle_collider.enabled = false;
        }



    }

    // 每帧更新调用
    public override CharacterStateEnum? OnUpdate()
    {
        if (userInput.IsCrouchPressed) {
            character.AddActionIgnore(0.1f, ActionIgnoreTag.Move,
            ActionIgnoreTag.Jump,
            ActionIgnoreTag.Attack);
        }
        if (!userInput.IsCrouchPressed)
        {
            return character.IsGrounded
                ? stateController.GetRunOrIdle()
                : CharacterStateEnum.Fall;
        }

        return null;
    }

    // 退出状态时调用
    public override void OnExit()
    {
        // 1. 重置动画参数
        character.Animator.SetBool("Is Crouching", false);


        // 2. 恢复原始碰撞体
        if (character.Crouch_collider != null && character.Idle_collider != null)
        {
            character.Crouch_collider.enabled = false;
            character.Idle_collider.enabled = true;
        }


    }
}

public class DamageState : CharacterStateBase
{
    private const float HurtDuration = 1.1f;
    private const float HurtInvincibilityTime = 1f;
    private const float HurtForce =40f;
    private const float MaxKnockbackSpeed = 3f;
    public override CharacterStateEnum StateType => CharacterStateEnum.Damage;

    private float hurtTimer = 3.5f;
    private Vector3 hurtDirection;

    public DamageState(CharacterMovement character, UserInput userInput, 
                       StateController stateController, Vector3 hurtSourcePosition)
        : base(character, userInput, stateController) 
    {
        Vector3 direction = (character.transform.position - hurtSourcePosition);
        direction.y = 0f;
        hurtDirection = direction.normalized;
        // 没有攻击者位置时（如直接调 ChangeState(Damage)），默认按朝向反向击退，
        // 避免零向量导致 normalized 全零、角色缩放被压平
        if (hurtDirection.sqrMagnitude < 0.001f)
            hurtDirection = character.IsFacingRight ? Vector3.left : Vector3.right;
    }

    public override void OnEnter()
    {
        
        hurtTimer = HurtDuration;
        // 受击无敌帧：HurtInvincibilityTime 内 Damage 被屏蔽，EventManager 端不再扣血
        character.AddActionIgnore(HurtInvincibilityTime, ActionIgnoreTag.Damage);
        Vector3 scale = character.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(-hurtDirection.x);
        character.transform.localScale = scale;
        character.Animator.Play("hurt");
        AudioManager.Instance?.PlayHurtSFX();
        character.Animator.SetBool("Is Hurt", true);
        character.AddActionIgnore(HurtDuration,
            ActionIgnoreTag.Move, ActionIgnoreTag.Jump,
            ActionIgnoreTag.Attack, ActionIgnoreTag.Interact);

    }

    /// <summary>
    /// 连续受击时刷新硬直：更新击退方向、重置硬直计时、续上动作锁定与无敌帧。
    /// 不重播受伤动画和音效——动画只在首次进入 Damage 时播放一次。
    /// </summary>
    public void RefreshHit(Vector3 hurtSourcePosition)
    {
        Vector3 direction = character.transform.position - hurtSourcePosition;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            hurtDirection = direction.normalized;

        // 按新的击退方向翻转朝向
        Vector3 scale = character.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(-hurtDirection.x);
        character.transform.localScale = scale;

        hurtTimer = HurtDuration;
        character.AddActionIgnore(HurtDuration,
            ActionIgnoreTag.Move, ActionIgnoreTag.Jump,
            ActionIgnoreTag.Attack, ActionIgnoreTag.Interact);
        character.AddActionIgnore(HurtInvincibilityTime, ActionIgnoreTag.Damage);
    }

    public override CharacterStateEnum? OnUpdate()
    {
        hurtTimer -= TimeManager.GameplayDT;

        if (hurtTimer > 0f)
        {
            // 击退：地面清零竖直速度，空中保留下落速度（空中受击不再悬浮）
            float vy = character.IsGrounded ? 0f : character.Rigidbody.velocity.y;
            float vx = Mathf.Clamp(hurtDirection.x * HurtForce, -MaxKnockbackSpeed, MaxKnockbackSpeed);
            character.Rigidbody.velocity = new Vector2(vx, vy);
        }

        if (hurtTimer <= 0f)
        {
            return character.IsGrounded
                ? stateController.GetRunOrIdle()
                : CharacterStateEnum.Fall;
        }
        return null;
    }

    public override void OnExit()
    {
        character.Animator.SetBool("Is Hurt", false);
        hurtTimer = 0f;
    }
}

public class DeathState : CharacterStateBase
{
    private const float DeathDuration = 1f;
    private const float RespawnTime = 2.0f;

    public override CharacterStateEnum StateType => CharacterStateEnum.Die;

    private float deathTimer = DeathDuration;
    private float responeTimer = RespawnTime;
    private bool isDying;
    private bool _respawnRequested;

    public DeathState(CharacterMovement character, UserInput userInput,
                       StateController stateController)
        : base(character, userInput, stateController)
    {
        isDying = false;
    }

    public override void OnEnter()
    {
        EventManager.Instance.TriggerDie();
        deathTimer = DeathDuration;
        responeTimer = RespawnTime;
        isDying = true;
        _respawnRequested = false;
        character.Rigidbody.velocity = Vector3.zero;     // 清除速度
        character.Animator.Play("Death");
        AudioManager.Instance?.PlayDeathSFX();
        character.Animator.SetBool("Is active", true);
        character.AddActionIgnore(DeathDuration,
          ActionIgnoreTag.All);


    }

    // 复活走事件（TriggerRespawn → HandleRespawnRequest），OnUpdate 不自报
    public override CharacterStateEnum? OnUpdate()
    {
        deathTimer -= TimeManager.GameplayDT;
        responeTimer -= TimeManager.GameplayDT;


        if (deathTimer <= 0f && isDying)
        {
            isDying = false;
        }

        // 复活：倒计时结束后发复活请求，由 StateController 切回 Idle
        if (responeTimer <= 0f && !_respawnRequested)
        {
            _respawnRequested = true;
            EventManager.Instance?.TriggerRespawn();
        }
        return null;
    }

    public override void OnExit()
    {
        character.Animator.SetBool("Is Dead", false);
      
    }
}
