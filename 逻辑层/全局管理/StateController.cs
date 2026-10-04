using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class StateController : MonoBehaviour
{
    [Header("当前状态")]
    [SerializeField] private CharacterStateEnum _currentStateType;

    private ICharacterState _currentState;
    private Dictionary<CharacterStateEnum, ICharacterState> _states;

    private CharacterMovement _character;
    private UserInput _userInput;

    // 切换重入保护：OnExit/OnEnter 里再发起切换时排队，避免两个状态交错执行
    private bool _isTransitioning;
    private CharacterStateEnum? _pendingState;
    private Vector3? _pendingAttackerPos;
    private bool _pendingSkipIgnore;

    public static StateController Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private bool _initialized = false;

    void Start()
    {
        TryInitialize();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 依赖组件可能来自尚未加载完的场景（如 Mannerger 里的 UserInput），
        // 每有一次场景加载完成就重试一次，直到全部就绪
        TryInitialize();
    }

    private void TryInitialize()
    {
        if (_initialized)
            return;

        _character = CharacterMovement.Instance;
        _userInput = UserInput.Instance;

        if (_character == null || _userInput == null)
        {
            Debug.LogWarning("StateController: 依赖组件尚未就绪，等待场景加载后重试...");
            return;
        }

        InitializeStates();

        // 订阅事件 - 注意事件名和EventManager里定义的保持一致
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnJumpRequested.Subscribe(HandleJumpRequest);
            EventManager.Instance.OnDodgeRequested.Subscribe(HandleDodgeRequest);
            EventManager.Instance.OnAttackRequested.Subscribe(HandleAttackRequest);
            EventManager.Instance.OnCrouchRequested.Subscribe(HandCrouchRequest);
            EventManager.Instance.OnDamgeRequested.Subscribe(HandleDamageRequest);
            EventManager.Instance.OnDieRequested.Subscribe(HandleDieRequest);
            EventManager.Instance.OnRespawnRequested.Subscribe(HandleRespawnRequest);
        }

        ChangeState(CharacterStateEnum.Idle);
        _initialized = true;
    }


    private void InitializeStates()
    {
        _states = new Dictionary<CharacterStateEnum, ICharacterState>
        {
            [CharacterStateEnum.Idle] = new IdleState(_character, _userInput, this),
            [CharacterStateEnum.Run] = new RunState(_character, _userInput, this),
            [CharacterStateEnum.Jump] = new JumpState(_character, _userInput, this),
            [CharacterStateEnum.Fall] = new FallState(_character, _userInput, this),
            [CharacterStateEnum.RunJump] = new RunJumpState(_character, _userInput, this),
            [CharacterStateEnum.Dodge] = new DodgeState(_character, _userInput, this),
            [CharacterStateEnum.Attack] = new AttackState(_character, _userInput, this),
            [CharacterStateEnum.Crouch] = new CrouchState(_character, _userInput, this),
          
            [CharacterStateEnum.Die] = new DeathState(_character, _userInput, this),
        };
    }

    void Update()
    {
        // 状态自报：状态只报告想去的方向，切换裁决（动作屏蔽等）统一在 ChangeState
        CharacterStateEnum? next = _currentState?.OnUpdate();
        if (next.HasValue)
        {
            // 状态自报的退出切换不受动作屏蔽限制：屏蔽只挡外部新请求，不能挡住状态自己的收尾
            // （否则 Crouch 松手退出会被自己每帧续的 Move 屏蔽卡住）
            ChangeState(next.Value, skipIgnoreCheck: true);
        }
        _currentStateType = _currentState?.StateType ?? CharacterStateEnum.Idle;
    }

    void FixedUpdate()
    {
        _currentState?.OnFixedUpdate();
    }

    /// <summary>
    /// 根据当前水平输入决定该回到 Run 还是 Idle（落地、攻击结束等处的统一判断）
    /// </summary>
    public CharacterStateEnum GetRunOrIdle()
    {
        return _userInput != null && Mathf.Abs(_userInput.HorizontalInput) > 0.01f
            ? CharacterStateEnum.Run
            : CharacterStateEnum.Idle;
    }

    /// <summary>
    /// 输入锁定单一入口：对话、背包等系统统一调用，不再各自摸 UserInput / UIManager。
    /// 内部同时处理 UserInput.stop、UIManager.IsUIBlockingInput，锁定时把角色切回 Idle。
    /// </summary>
    public static void SetInputLocked(bool locked)
    {
        if (UserInput.Instance != null)
            UserInput.Instance.stop = locked;

        UIManager.IsUIBlockingInput = locked;

        if (locked && Instance != null)
        {
            Instance.ChangeState(CharacterStateEnum.Idle);
        }
    }

    /// <summary>
    /// 状态切换的唯一入口。
    /// 动作屏蔽（ActionIgnore）的裁决统一在这里做：目标状态对应的动作被屏蔽时拒绝切换。
    /// Damage 状态需要攻击者位置，通过 attackerPos 传入并在此动态创建。
    /// skipIgnoreCheck=true 用于状态自报的退出切换（收尾不受屏蔽限制）。
    /// </summary>
    public void ChangeState(CharacterStateEnum newState, Vector3? attackerPos = null, bool skipIgnoreCheck = false)
    {
        // 重入保护：切换过程中收到的新请求排队，本次切换完成后再处理
        if (_isTransitioning)
        {
            _pendingState = newState;
            _pendingAttackerPos = attackerPos;
            _pendingSkipIgnore = skipIgnoreCheck;
            return;
        }

        // 同状态早退；Damage 例外——连续受击只刷新击退方向与硬直计时，不重播受伤动画
        if (_currentState != null && _currentState.StateType == newState)
        {
            if (newState == CharacterStateEnum.Damage && _currentState is DamageState dmg)
            {
                Vector3 source = attackerPos ?? (_character != null ? _character.transform.position : Vector3.zero);
                dmg.RefreshHit(source);
            }
            return;
        }

        // 动作屏蔽裁决（状态自报的退出切换不受限制）
        if (!skipIgnoreCheck && _character != null && IsBlockedByIgnore(newState))
        {
            Debug.Log($"[StateController] {newState} 被动作屏蔽，拒绝切换");
            return;
        }

        Debug.Log($"[StateController] {_currentState?.StateType} -> {newState}");

        // 先解析目标状态，找不到则保持原状态（避免退出当前状态后无状态可用）
        ICharacterState nextState;
        if (newState == CharacterStateEnum.Damage)
        {
            if (_character == null)
            {
                Debug.LogWarning("[StateController] 角色尚未就绪，忽略 Damage 切换");
                return;
            }
            Vector3 source = attackerPos ?? _character.transform.position;
            nextState = new DamageState(_character, _userInput, this, source);
        }
        else if (!_states.TryGetValue(newState, out nextState))
        {
            Debug.LogError($"[StateController] 没有注册状态 {newState}，切换被忽略");
            return;
        }

        _isTransitioning = true;
        _currentState?.OnExit();
        _currentState = nextState;
        _currentState.OnEnter();
        _isTransitioning = false;

        // 处理切换期间排队的新请求
        if (_pendingState.HasValue)
        {
            var pendingState = _pendingState.Value;
            var pendingPos = _pendingAttackerPos;
            var pendingSkip = _pendingSkipIgnore;
            _pendingState = null;
            _pendingAttackerPos = null;
            _pendingSkipIgnore = false;
            ChangeState(pendingState, pendingPos, pendingSkip);
        }
    }

    /// <summary>目标状态是否被当前的 ActionIgnore 屏蔽（裁决集中于此，别处不再重复检查）</summary>
    private bool IsBlockedByIgnore(CharacterStateEnum newState)
    {
        switch (newState)
        {
            case CharacterStateEnum.Run:
                return _character.IsActionIgnored(ActionIgnoreTag.Move);
            case CharacterStateEnum.RunJump:
                return _character.IsActionIgnored(ActionIgnoreTag.Move)
                    || _character.IsActionIgnored(ActionIgnoreTag.Jump);
            case CharacterStateEnum.Jump:
                return _character.IsActionIgnored(ActionIgnoreTag.Jump);
            case CharacterStateEnum.Attack:
                return _character.IsActionIgnored(ActionIgnoreTag.Attack);
            case CharacterStateEnum.Dodge:
                return _character.IsActionIgnored(ActionIgnoreTag.Dodge);
            case CharacterStateEnum.Crouch:
                return _character.IsActionIgnored(ActionIgnoreTag.crouch);
            case CharacterStateEnum.Damage:
                return _character.IsActionIgnored(ActionIgnoreTag.Damage);
            case CharacterStateEnum.Die:
                return _character.IsActionIgnored(ActionIgnoreTag.die);
            default:
                return false;
        }
    }

    // ========== 事件处理 ==========
    private void HandleJumpRequest()
    {
        if (_currentState == null) return;

        if (!_character.IsGrounded)
            return; // 空中不处理跳跃

        Debug.Log($"StateController: 收到跳跃请求，当前状态 {_currentState.StateType}");

        switch (_currentState.StateType)
        {
            case CharacterStateEnum.Run:
                ChangeState(CharacterStateEnum.RunJump);
                break;
            case CharacterStateEnum.Idle:
                ChangeState(CharacterStateEnum.Jump);
                break;
            case CharacterStateEnum.Dodge:
                // 闪避中起跳（原 DodgeState 里的规则，收归事件统一入口）。
                // 跳过屏蔽裁决：闪避自带的 Move 屏蔽不应挡住起跳取消
                ChangeState(CharacterStateEnum.RunJump, skipIgnoreCheck: true);
                break;
                // 其他状态不处理跳跃
        }
    }

    private void HandleAttackRequest()
    {
        if (_currentState == null) return;

        if (_character.IsGrounded)
        {
            ChangeState(CharacterStateEnum.Attack);
        }
    }

    private void HandleDodgeRequest()
    {
        if (_currentState == null) return;

        if (_character.IsGrounded)
        {
            ChangeState(CharacterStateEnum.Dodge);
        }
    }

    private void HandleDamageRequest(Vector3 attackerPosition)
    {
        if (_currentState == null) return;

        // 不再要求在地面：空中受击同样进 Damage 态（结束时会按是否落地自动回落）
        Debug.Log($"[StateController] 收到伤害请求，攻击者位置: {attackerPosition}");
        ChangeState(CharacterStateEnum.Damage, attackerPosition);
    }

    private void HandCrouchRequest()
    {
        if (_currentState == null) return;

        if (_character.IsGrounded)
        {
            ChangeState(CharacterStateEnum.Crouch);
        }
    }

    private void HandleDieRequest()
    {
        if (_currentState == null) return;

        ChangeState(CharacterStateEnum.Die);
    }

    private void HandleRespawnRequest()
    {
        if (_currentState == null) return;

        // 复活只从死亡状态出发
        if (_currentState.StateType != CharacterStateEnum.Die)
            return;

        // 复活时回满血量：否则 0 血复活后 TriggerDamage 会因 Health<=0 直接丢弃所有伤害
        if (_character != null)
        {
            CharacterData data = _character.GetComponent<CharacterData>();
            if (data != null)
                data.Health = data.MaxHealth;
        }

        ChangeState(CharacterStateEnum.Idle);
    }

    void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnJumpRequested.Unsubscribe(HandleJumpRequest);
            EventManager.Instance.OnDodgeRequested.Unsubscribe(HandleDodgeRequest);
            EventManager.Instance.OnAttackRequested.Unsubscribe(HandleAttackRequest);
            EventManager.Instance.OnCrouchRequested.Unsubscribe(HandCrouchRequest);
            EventManager.Instance.OnDamgeRequested.Unsubscribe(HandleDamageRequest);
            EventManager.Instance.OnDieRequested.Unsubscribe(HandleDieRequest);
            EventManager.Instance.OnRespawnRequested.Unsubscribe(HandleRespawnRequest);
        }
    }
}
