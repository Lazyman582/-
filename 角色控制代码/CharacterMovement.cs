using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
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

    public  bool IsFacingRight;

    public float groundpostion;

    public Collider2D Idle_collider;
    public Collider2D Dodge_collider;
    public Collider2D Crouch_collider;

    public bool groundDetected;

    private CharacterData characterData;

    public Rigidbody2D Rigidbody { get; private set; }
    public Animator Animator { get; private set; }
    public bool IsGrounded { get; private set; }

    // 私有字段
    private UserInput _userInput;
    private List<ActionIgnore> _actionIgnores;

    private float _fallSpeedYDampingChangeThreshould;
    // 公共属性
    public static CharacterMovement Instance { get; private set; }

    void Awake()
    {
        // 单例设置
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 获取组件
        Rigidbody = GetComponent<Rigidbody2D>();
        Animator = GetComponent<Animator>();

        if (Rigidbody == null)
            Debug.LogError("CharacterMovement: 缺少Rigidbody2D组件！");

        _actionIgnores = new List<ActionIgnore>();


        _fallSpeedYDampingChangeThreshould = CameraManager.instance._fallSpeedDampingChangeThreshold;
    }

    void Start()
    {
        characterData = FindObjectOfType<CharacterData>();


        _userInput = UserInput.Instance;
        if (_userInput == null)
            Debug.LogError("CharacterMovement: 找不到UserInput实例！");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {

         OnDamge();

        }

#if UNITY_2022
        if (Input.GetKeyDown(KeyCode.C)) {

            Time.timeScale = 0.33f;
        
        }
#endif
        if (Rigidbody.velocity.y< _fallSpeedYDampingChangeThreshould &&!CameraManager.instance.IsLeapingDamping && !CameraManager.instance.LeppedFromPlayerFalling) {

            Debug.Log(CameraManager.instance.IsLeapingDamping);
            CameraManager.instance.LrepDumping(true);
        
        }

        if (Rigidbody.velocity.y >=0f&& !CameraManager.instance.IsLeapingDamping && CameraManager.instance.LeppedFromPlayerFalling)
        {
            Debug.Log(CameraManager.instance.IsLeapingDamping);
            CameraManager.instance.LeppedFromPlayerFalling = false;

            CameraManager.instance.LrepDumping(false);
        }

        GroundCheck();
    }

    void FixedUpdate()
    {
        RefreshActionIgnores();
    }

    private void GroundCheck()
    {
        if (groundCheckPoint == null) return;

        // 第一步：使用 OverlapCircle 进行初步检测（保留原有逻辑）
        IsGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
        //Animator.SetBool("IsGround", IsGrounded);

        // 第二步：如果 OverlapCircle 检测到了地面，我们进行进一步确认
        if (IsGrounded)
        {
            // 发射一个极短的 BoxCast，只向下探测 0.05 单位距离
            // 这个盒子的宽度是检测圆的直径，高度非常薄（0.01），方向向下
            RaycastHit2D hit = Physics2D.BoxCast(
                groundCheckPoint.position,           // 起点（脚底中心）
                new Vector2(groundCheckRadius * 2, 0.01f), // 盒子的尺寸（宽度 = 检测圆的直径，厚度极薄）
                0f,                                 // 角度
                Vector2.down,                       // 方向向下
                0.05f,                              // 探测距离（非常短，只检测到地面）
                groundLayer                         // 只检测地面层
            );

            // 如果 BoxCast 没有检测到任何东西，说明角色实际上是站在墙壁侧面，而不是地面上
            if (hit.collider == null)
            {
                IsGrounded = false;
            }
        }

        // 更新动画状态
        Animator.SetBool("Is Ground", IsGrounded);
    }

    private void HandcollisionDetection()
    {


        groundDetected = Physics.Raycast(transform.position, Vector2.down, groundpostion, groundLayer);

    }

    private void OnDamge() {

        characterData.TakeDamage(10);





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

        // 查找是否已有相同mask
        for (int i = 0; i < _actionIgnores.Count; i++)
        {
            if (_actionIgnores[i].Mask.Equals(mask))
            {
                _actionIgnores[i].timer = duration;
                return;
            }
        }

        // 添加新屏蔽
        _actionIgnores.Add(new ActionIgnore(mask, duration));
    }

    public bool IsActionIgnored(ActionIgnoreTag tag)
    {
        foreach (var ignore in _actionIgnores)
        {
            if (ignore.Mask.ContainTag(tag))
                return true;
        }
        return false;
    }


    public void  RefreshActionIgnores()
    {
        for (int i = _actionIgnores.Count - 1; i >= 0; i--)
        {
            _actionIgnores[i].timer -= Time.fixedDeltaTime;
            if (_actionIgnores[i].timer <= 0)
                _actionIgnores.RemoveAt(i);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position,transform.position+new Vector3(0,-groundpostion));
    }

}




