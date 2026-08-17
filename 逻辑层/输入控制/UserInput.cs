using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class UserInput : MonoBehaviour
{
    public static UserInput Instance { get; private set; }

    [Header("速度")]
    [SerializeField] private float _pressThreshold = 0.2f;
    [SerializeField] private float _releaseBuffer = 0.2f;


    public float HorizontalInput { get; private set; }
    public float VerticalInput { get; private set; }
    public bool IsJumpPressed { get; private set; }
    public bool IsJumpHeld { get; private set; }

    public bool stop { get; set; }
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
        if (stop)
        {
            // stop 时清零，防止 IdleState 读到旧值切回 Run
            if (Mathf.Abs(HorizontalInput) > 0.01f)
            {
                HorizontalInput = 0f;
                EventManager.Instance?.TriggerMove(0f);
            }
            return;
        }
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
                if (Mathf.Abs(HorizontalInput) > 0.01f)  
                {
                    HorizontalInput = 0f;
                    EventManager.Instance?.TriggerMove(0f); 
                }
                _pressStartTime = 0f;
            }
        }
    }

    private void HandleJumpInput()
    {
        if (stop) return;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            IsJumpPressed = true;
            IsJumpHeld = true;
            EventManager.Instance?.TriggerJump ();  
          
        }

      
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            IsJumpPressed = false;
            IsJumpHeld = false;
           
        }
        else
        {
            IsJumpPressed = false;
        }
    }


    private void HandleCrouchInput() {
        if (stop) return;
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
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[UserInput] E 键按下 → TriggerInteract");
            EventManager.Instance?.TriggerInteract();
        }

        if (stop) return;
        if (Input.GetKeyDown(KeyCode.J))
        {
            AttackPressed = true;
            EventManager.Instance?.TriggerAttack();
        }
        else {

            AttackPressed = false;
        }
       
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


