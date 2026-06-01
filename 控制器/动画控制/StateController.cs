using System.Collections;
using System.Collections.Generic;
using UnityEngine;



// 状态枚举（保持不变）



public class StateController : MonoBehaviour
{
    [Header("当前状态")]
    [SerializeField] private CharacterStateEnum _currentStateType;

    private ICharacterState _currentState;
    private Dictionary<CharacterStateEnum, ICharacterState> _states;

    private CharacterMovement _character;
    private UserInput _userInput;

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

    void Start()
    {
        _character = CharacterMovement.Instance;
        _userInput = UserInput.Instance;

        if (_character == null || _userInput == null)
        {
            Debug.LogError("StateController: 找不到必要的依赖组件！");
            return;
        }

        InitializeStates();

        // 订阅事件 - 注意事件名和EventManager里定义的保持一致
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnJumpRequested += HandleJumpRequest;
            EventManager.Instance.OnRunRequested += HandleRunRequest;
            EventManager.Instance.OnMoveRequested += HandleMoveRequest;  // 新增：处理移动事件
            EventManager.Instance.OnDodgeRequested += HandleDodgeRequest;
            EventManager.Instance.OnAttackRequested += HandleAttackRequest;
            EventManager.Instance.OnCrouchRequested += HandCrouchRequest;
            EventManager.Instance.OnDamgeRequested += HandleDamageRequest;
            EventManager.Instance.OnDieRequested += HandleDieRequest;

        }

        ChangeState(CharacterStateEnum.Idle);
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
            //[CharacterStateEnum.Damage] = new DamageState(_character, _userInput, this)
            [CharacterStateEnum.Die] = new DeathState(_character, _userInput, this),
        };
    }

    void Update()
    {
        _currentState?.OnUpdate();
        _currentStateType = _currentState?.StateType ?? CharacterStateEnum.Idle;
    }

    void FixedUpdate()
    {
        _currentState?.OnFixedUpdate();
    }

    public void ChangeStateWithDamage(CharacterStateEnum newState, Vector3 attackerPosition)
    {
        if (_currentState != null && _currentState.StateType == newState)
            return;

        Debug.Log($"[StateController] {_currentState?.StateType} -> {newState}");
        _currentState?.OnExit();

        // DamageState 需要动态创建
        if (newState == CharacterStateEnum.Damage)
        {
            _currentState = new DamageState(_character, _userInput, this, attackerPosition);
            _currentState.OnEnter();
        }
        // 其他状态从字典获取
        else if (_states.TryGetValue(newState, out var state) && state != null)
        {
            _currentState = state;
            _currentState.OnEnter();
        }
    }
    public void ChangeState(CharacterStateEnum newState)
    {
        if (_currentState != null && _currentState.StateType == newState)
            return;

       
        if (_character != null)
        {
            // 如果要切换到移动相关状态，检查Move是否被屏蔽
            if (newState == CharacterStateEnum.Run || newState == CharacterStateEnum.RunJump)
            {
                if (_character.IsActionIgnored(ActionIgnoreTag.Move))
                {
                    Debug.Log($"[StateController] Move被屏蔽，无法切换到 {newState}");
                    return;
                }
            }

            // 如果要切换到跳跃相关状态，检查Jump是否被屏蔽
            if (newState == CharacterStateEnum.Jump || newState == CharacterStateEnum.RunJump)
            {
                if (_character.IsActionIgnored(ActionIgnoreTag.Jump))
                {
                    Debug.Log($"[StateController] Jump被屏蔽，无法切换到 {newState}");
                    return;
                }
            }
        }

        Debug.Log($"[StateController] {_currentState?.StateType} -> {newState}");

        _currentState?.OnExit();

        if (_states.TryGetValue(newState, out var state))
        {
            _currentState = state;
            _currentState.OnEnter();
        }
    }

    // ========== 事件处理 ==========
    private void HandleJumpRequest()
    {
        if (_currentState == null) return;

        if (!_character.IsGrounded)
            return; // 空中不处理跳跃


        Debug.Log($"StateController: 收到跳跃请求，当前状态 {_currentState.StateType}");

        // 检查是否被屏蔽
        if (_character.IsActionIgnored(ActionIgnoreTag.Jump))
        {
            Debug.Log("跳跃被屏蔽");
            return;
        }



        switch (_currentState.StateType)
        {
            case CharacterStateEnum.Run:
                ChangeState(CharacterStateEnum.RunJump);
                break;
            case CharacterStateEnum.Idle:
            case CharacterStateEnum.Fall:
                ChangeState(CharacterStateEnum.Jump);
                break;
                // 其他状态不处理跳跃
        }
    }

    private void HandleRunRequest()
    {
        if (_currentState == null) return;

        // 检查是否被屏蔽
        if (_character.IsActionIgnored(ActionIgnoreTag.Move))
            return;

        // 只有在地面才能跑步
        if (_character.IsGrounded)
        {
            ChangeState(CharacterStateEnum.Run);
        }
    }

    private void HandleAttackRequest() {

        if (_currentState == null) return;

        if (_character.IsActionIgnored(ActionIgnoreTag.Attack))
            return;
        if (_character.IsGrounded)
        {
           
            ChangeState(CharacterStateEnum.Attack);
        }
    }

    private void HandleDodgeRequest() {


        if (_currentState == null) return;

        if (_character.IsActionIgnored(ActionIgnoreTag.Dodge))
            return;
        if (_character.IsGrounded)
        {
            ChangeState(CharacterStateEnum.Dodge);
        }

    }
    private void HandleDamageRequest(Vector3 attackerPosition)
    {

        if (_currentState == null) return;

        if (_character.IsActionIgnored(ActionIgnoreTag.da))
            return;
        if (_character.IsGrounded)
        {

          

            Debug.Log($"[StateController] 收到伤害请求，攻击者位置: {attackerPosition}");

            // 使用新方法传入攻击者位置
            ChangeStateWithDamage(CharacterStateEnum.Damage, attackerPosition);
        }
    }
    private void HandCrouchRequest() {
     
        if (_currentState == null) return;

        if (_character.IsActionIgnored(ActionIgnoreTag.crouch))
            return;
        if (_character.IsGrounded)
        {
            ChangeState(CharacterStateEnum.Crouch);
        }


    }

    private void HandleMoveRequest(float direction)
    {
        
    }

    private void HandleDieRequest() {

        Debug.LogError("555555");
        if (_currentState == null) return;

        if (_character.IsActionIgnored(ActionIgnoreTag.die))
            return;
      Debug.LogError("555555");
            ChangeState(CharacterStateEnum.Die);
        




    }
    void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnJumpRequested -= HandleJumpRequest;
            EventManager.Instance.OnRunRequested -= HandleRunRequest;
            EventManager.Instance.OnMoveRequested -= HandleMoveRequest;    // 
            EventManager.Instance.OnDodgeRequested -= HandleDodgeRequest;  // 
            EventManager.Instance.OnAttackRequested -= HandleAttackRequest;
            EventManager.Instance.OnCrouchRequested -= HandCrouchRequest;  // 
            EventManager.Instance.OnDamgeRequested -= HandleDamageRequest;// 

        }
    }
}