using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.U2D.Animation;
using UnityEngine;





[DefaultExecutionOrder(-100)]
public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }


    public event Action OnJumpRequested;      // 跳跃请求
    public event Action OnRunRequested;       // 跑步请求
    public event Action OnAttackRequested;    // 攻击请求
    public event Action OnSkillRequested;     // 技能请求
    public event Action OnDodgeRequested;     //滑铲请求
    public event Action OnCrouchRequested;    //下蹲请求
    public event Action<Vector3> OnDamgeRequested;
    public event Action OnDieRequested;

    public event Action<float> OnMoveRequested;  // 移动请求，参数为方向

    public CharacterData characterData;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        characterData = FindObjectOfType<CharacterData>();
        DontDestroyOnLoad(gameObject);
    }

    // 触发事件的方法
    public void TriggerJump()
    {
        Debug.Log("[Event] Jump Requested");
        OnJumpRequested?.Invoke();
    }

    public void TriggerRun()
    {
        Debug.Log("[Event] Run Requested");
        OnRunRequested?.Invoke();
    }

    public void TriggerMove(float direction)
    {
        OnMoveRequested?.Invoke(direction);
    }

    public void TriggerAttack()
    {
        OnAttackRequested?.Invoke();
    }

    public void TriggerSkill()
    {
        OnSkillRequested?.Invoke();
    }

    public void TriggerDodge() { 
    
    
    OnDodgeRequested?.Invoke();
    
    }

    public void TriggerCrouch() {
        
    OnCrouchRequested?.Invoke();
    
    
    }
    public void TriggerDamage(float amount, Vector3 attackerPosition)
    {
        if (characterData.Health <= 0)
        {
            Debug.LogError("44444");
            OnDieRequested?.Invoke();


        }
        characterData.TakeDamage(amount);
        
        OnDamgeRequested?.Invoke(attackerPosition);  // 传入攻击者位置
    }

    public void TriggerDie() {


       UserInput.Instance.enabled = false;


    }

    // 清理事件
    public void ClearAllEvents()
    {
        OnJumpRequested = null;
        OnRunRequested = null;
        OnMoveRequested = null;
        OnAttackRequested = null;
        OnSkillRequested = null;
    }

    void OnDestroy()
    {
        ClearAllEvents();
    }
}

