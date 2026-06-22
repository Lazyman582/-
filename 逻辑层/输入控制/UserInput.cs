using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class UserInput : MonoBehaviour
{
    public static UserInput Instance { get; private set; }

    [Header("输入参数")]
    [SerializeField] private float _pressThreshold = 0.2f;
    [SerializeField] private float _releaseBuffer = 0.2f;

    // 公共属性
    public float HorizontalInput { get; private set; }
    public float VerticalInput { get; private set; }
    public bool IsJumpPressed { get; private set; }
    public bool IsJumpHeld { get; private set; }

    public float gameTime { get; private set; }
    public bool IsCrouchPressed { get; private set; }
    public bool AttackPressed { get; private set; }

    private float _pressStartTime;
    private float _lastReleaseTime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        HandleMovementInput();
        HandleJumpInput();
        HandleActionInput();
        HandleCrouchInput();
      
    }

    

    private void HandleMovementInput()
    {
        float rawInput = Input.GetAxisRaw("Horizontal");
        float previousInput = HorizontalInput;

        if (Mathf.Abs(rawInput) > 0.01f)
        {
            if (_pressStartTime == 0)
                _pressStartTime = Time.time;

            float heldDuration = Time.time - _pressStartTime;

            HorizontalInput = heldDuration <= _pressThreshold ?
                0.5f * Mathf.Sign(rawInput) :
                Mathf.Sign(rawInput);

            _lastReleaseTime = -1f;

            // 只有当输入值真正变化时才触发事件
            if (Mathf.Abs(HorizontalInput - previousInput) > 0.01f)
            {
                EventManager.Instance?.TriggerMove(HorizontalInput);
            }
        }
        else
        {
            if (_lastReleaseTime < 0)
                _lastReleaseTime = Time.time;

            if (Time.time - _lastReleaseTime >= _releaseBuffer)
            {
                if (Mathf.Abs(HorizontalInput) > 0.01f)  // 如果之前有输入，现在停止了
                {
                    HorizontalInput = 0f;
                    EventManager.Instance?.TriggerMove(0f);  // 触发停止移动
                }
                _pressStartTime = 0f;
            }
        }
    }

    private void HandleJumpInput()
    {
        // 按下瞬间 - 触发跳跃请求
        if (Input.GetKeyDown(KeyCode.Space))
        {
            IsJumpPressed = true;
            IsJumpHeld = true;
            EventManager.Instance?.TriggerJump ();  // 触发跳跃事件
          
        }

        // 释放
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            IsJumpPressed = false;
            IsJumpHeld = false;
            // 跳跃结束不需要触发事件，状态机自己会处理
        }
        else
        {
            IsJumpPressed = false;
        }
    }


    private void HandleCrouchInput() {

        if (Input.GetKey(KeyCode.S))
        {

            IsCrouchPressed = true;
            EventManager.Instance?.TriggerCrouch();

        }
        else if (Input.GetKeyUp(KeyCode.S)) {

            IsCrouchPressed = false;


        }
    
    }

    private void HandleActionInput()
    {


        // 攻击键示例（如果需要）
        if (Input.GetKeyDown(KeyCode.J))
        {
            AttackPressed = true;
            EventManager.Instance?.TriggerAttack();
        }
        else {

            AttackPressed = false;
        }
        // 技能键示例
        if (Input.GetKeyDown(KeyCode.K))
        {
            EventManager.Instance?.TriggerSkill();
        }
        if (Input.GetKeyDown(KeyCode.LeftShift)) {

            EventManager.Instance?.TriggerDodge();
           
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}


