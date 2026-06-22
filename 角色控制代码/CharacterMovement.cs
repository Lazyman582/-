using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ActionIgnoreMask;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class CharacterMovement : MonoBehaviour
{
    [Header("移动参数")]
    [Tooltip("角色的移动速度（米/秒）")]
    public float moveSpeed = 5f;

    [Header("跳跃参数")]
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
            Debug.LogError("CharacterMovement: 缺少Rigidbody2D组件！");
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
            Debug.LogError("CharacterMovement: 找不到UserInput实例！");
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

        IsGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);

        if (IsGrounded)
        {
            RaycastHit2D hit = Physics2D.BoxCast(
                groundCheckPoint.position,
                new Vector2(groundCheckRadius * 2, 0.01f),
                0f,
                Vector2.down,
                0.05f,
                groundLayer
            );

            if (hit.collider == null)
            {
                IsGrounded = false;
            }
        }

        Animator.SetBool("Is Ground", IsGrounded);
    }

    private void OnDamge()
    {
        if (characterData != null)
        {
            characterData.TakeDamage(10);
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
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(0, -groundpostion));
    }
}
