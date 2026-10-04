using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ActionIgnoreMask;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class CharacterMovement : MonoBehaviour
{
    [Header("character")]
    [Tooltip("移速")]
    public float moveSpeed = 5f;

    [Header("数据配置")]
    public float jumpForce = 7f;
    public Transform groundCheckPoint;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer;

    public bool IsFacingRight;
    public float groundpostion;
    public Collider2D Idle_collider;
    public Collider2D Dodge_collider;
    public Collider2D Crouch_collider;
    public bool groundDetected;

    private CharacterData characterData;
    private UserInput _userInput;
    private List<ActionIgnore> _actionIgnores;
    private float _fallSpeedYDampingChangeThreshould;

    public Rigidbody2D Rigidbody { get; private set; }
    public Animator Animator { get; private set; }
    public bool IsGrounded { get; private set; }

    public static CharacterMovement Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Rigidbody = GetComponent<Rigidbody2D>();
        Animator = GetComponent<Animator>();

        if (Rigidbody == null)
        {
            Debug.Log("Rigidbody:null");
        }

        _actionIgnores = new List<ActionIgnore>();
        RefreshCameraDependencies();
    }

    private void Start()
    {
        characterData = GetComponent<CharacterData>();
        if (characterData == null)
        {
            characterData = FindObjectOfType<CharacterData>();
        }

        _userInput = UserInput.Instance;
        if (_userInput == null)
        {
            Debug.Log("userInput:null");
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            OnDamge();
        }

        if (CameraManager.instance != null &&
            Rigidbody.velocity.y < _fallSpeedYDampingChangeThreshould &&
            !CameraManager.instance.IsLeapingDamping &&
            !CameraManager.instance.LeppedFromPlayerFalling)
        {
            CameraManager.instance.LrepDumping(true);
        }

        if (CameraManager.instance != null &&
            Rigidbody.velocity.y >= 0f &&
            !CameraManager.instance.IsLeapingDamping &&
            CameraManager.instance.LeppedFromPlayerFalling)
        {
            CameraManager.instance.LeppedFromPlayerFalling = false;
            CameraManager.instance.LrepDumping(false);
        }

        GroundCheck();
    }

    private void FixedUpdate()
    {
        RefreshActionIgnores();
    }

    private void GroundCheck()
    {
        if (groundCheckPoint == null)
        {
            return;
        }

        
        Vector2 origin = groundCheckPoint.position;
        float halfWidth = groundCheckRadius;
        float distance = groundCheckRadius + 0.15f;

        RaycastHit2D leftHit = Physics2D.Raycast(origin + Vector2.left * halfWidth, Vector2.down, distance, groundLayer);
        RaycastHit2D rightHit = Physics2D.Raycast(origin + Vector2.right * halfWidth, Vector2.down, distance, groundLayer);

        IsGrounded = IsValidGround(leftHit) || IsValidGround(rightHit);

        Animator.SetBool("Is Ground", IsGrounded);
    }


    private static bool IsValidGround(RaycastHit2D hit)
    {
        return hit.collider != null && hit.normal.y >= 0.5f;
    }

    private void OnDamge()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerDamage(10f, transform.position - transform.right);
        }
    }

    public void UpdateFacingDirection()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        if (horizontalInput != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(horizontalInput);
            transform.localScale = scale;
        }
    }

    public void AddActionIgnore(float duration, params ActionIgnoreTag[] tags)
    {
        var mask = ActionIgnoreMask.GetMask(tags);

        for (int i = 0; i < _actionIgnores.Count; i++)
        {
            if (_actionIgnores[i].Mask.Equals(mask))
            {
                _actionIgnores[i].timer = duration;
                return;
            }
        }

        _actionIgnores.Add(new ActionIgnore(mask, duration));
    }

    
    public void RemoveActionIgnore(params ActionIgnoreTag[] tags)
    {
        var mask = ActionIgnoreMask.GetMask(tags);
        for (int i = _actionIgnores.Count - 1; i >= 0; i--)
        {
            if (_actionIgnores[i].Mask.Equals(mask))
            {
                _actionIgnores.RemoveAt(i);
                return;
            }
        }
    }

    public bool IsActionIgnored(ActionIgnoreTag tag)
    {
        foreach (var ignore in _actionIgnores)
        {
            if (ignore.Mask.ContainTag(tag))
            {
                return true;
            }
        }

        return false;
    }

    public void RefreshActionIgnores()
    {
        for (int i = _actionIgnores.Count - 1; i >= 0; i--)
        {
            _actionIgnores[i].timer -= Time.fixedDeltaTime;
            if (_actionIgnores[i].timer <= 0)
            {
                _actionIgnores.RemoveAt(i);
            }
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshCameraDependencies();

        // 补抓输入引用：开局时 UserInput（Mannerger）可能尚未加载完成
        if (_userInput == null)
        {
            _userInput = UserInput.Instance;
        }
    }

    private void RefreshCameraDependencies()
    {
        if (CameraManager.instance != null)
        {
            _fallSpeedYDampingChangeThreshould = CameraManager.instance._fallSpeedDampingChangeThreshold;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;

            Vector2 origin = groundCheckPoint.position;
            float halfWidth = groundCheckRadius;
            float distance = groundCheckRadius + 0.15f;

            Vector2 left = origin + Vector2.left * halfWidth;
            Vector2 right = origin + Vector2.right * halfWidth;
            Gizmos.DrawLine(left, left + Vector2.down * distance);
            Gizmos.DrawLine(right, right + Vector2.down * distance);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(0, -groundpostion));
    }
}
