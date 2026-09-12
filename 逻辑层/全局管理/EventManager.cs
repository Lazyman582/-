using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;




[DefaultExecutionOrder(-100)]
public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }


    public readonly PriorityEvent OnJumpRequested = new();      // 跳跃请求
    public readonly PriorityEvent OnRunRequested = new();       // 跑步请求
    public readonly PriorityEvent OnAttackRequested = new();    // 攻击请求
    public readonly PriorityEvent OnSkillRequested = new();     // 技能请求
    public readonly PriorityEvent OnDodgeRequested = new();     //滑铲请求
    public readonly PriorityEvent OnCrouchRequested = new();    //下蹲请求
    public readonly PriorityEvent OnInteractRequested = new();  //交互请求（对话等）
    public readonly PriorityEvent<Vector3> OnDamgeRequested = new();
    public readonly PriorityEvent OnDieRequested = new();

    public readonly PriorityEvent<float> OnMoveRequested = new();  // 移动请求，参数为方向

    public CharacterData characterData;

    private CharacterData CurrentCharacterData
    {
        get
        {
            if (characterData == null)
            {
                characterData = PersistentPlayer.Instance != null
                    ? PersistentPlayer.Instance.CharacterData
                    : FindObjectOfType<CharacterData>();
            }

            return characterData;
        }
    }


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        characterData = PersistentPlayer.Instance != null
            ? PersistentPlayer.Instance.CharacterData
            : FindObjectOfType<CharacterData>();
        DontDestroyOnLoad(gameObject);
        Debug.Log("[EventManager] 初始化完成，Instance 已就绪");
    }

    // 触发事件的方法
    public void TriggerJump()
    {
        Debug.Log("[Event] Jump Requested");
        OnJumpRequested.Invoke();
    }

    public void TriggerRun()
    {
        Debug.Log("[Event] Run Requested");
        OnRunRequested.Invoke();
    }

    public void TriggerMove(float direction)
    {
        OnMoveRequested.Invoke(direction);
    }

    public void TriggerAttack()
    {
        OnAttackRequested.Invoke();
    }

    public void TriggerSkill()
    {
        OnSkillRequested.Invoke();
    }

    public void TriggerDodge() {


    OnDodgeRequested.Invoke();

    }

    public void TriggerCrouch() {

    OnCrouchRequested.Invoke();


    }

    public void TriggerInteract()
    {
        Debug.Log("[EventManager] TriggerInteract → OnInteractRequested.Invoke()");
        OnInteractRequested.Invoke();
    }

    public void TriggerDamage(float amount, Vector3 attackerPosition)
    {
        CharacterData player = CurrentCharacterData;
        if (player == null)
        {
            Debug.LogError("EventManager: CharacterData not found, damage ignored.");
            return;
        }

        if (player.Health <= 0f)
        {
            return;
        }

        player.TakeDamage(amount);

        if (player.Health <= 0f)
        {
            OnDieRequested.Invoke();
            return;
        }

        OnDamgeRequested.Invoke(attackerPosition);
    }

    public void TriggerDie() {


       if (UserInput.Instance != null)
       {
           UserInput.Instance.enabled = false;
       }


    }

    // 清理事件
    public void ClearAllEvents()
    {
        OnJumpRequested.Clear();
        OnRunRequested.Clear();
        OnMoveRequested.Clear();
        OnAttackRequested.Clear();
        OnSkillRequested.Clear();
        OnDodgeRequested.Clear();
        OnCrouchRequested.Clear();
        OnInteractRequested.Clear();
        OnDamgeRequested.Clear();
        OnDieRequested.Clear();
    }

    void OnDestroy()
    {
        ClearAllEvents();
    }
}
