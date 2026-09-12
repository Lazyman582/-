using UnityEngine;

public class EmeryAnimalContrller : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private string attackStateName = "Stone_Atack";

    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int IsDeath = Animator.StringToHash("IsDeath");
    private int attackStateHash;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        attackStateHash = Animator.StringToHash(attackStateName);
    }

    public void SetMove(bool isMoving)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsMoving, isMoving);
    }

    public void SetAttack(bool isAttacking)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsAttacking, isAttacking);
    }

    public void SetDeath(bool isDeath) {


        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsDeath, isDeath);


    }

    public bool IsInAttackState()
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.shortNameHash == attackStateHash;
    }

    public bool IsAttackAnimationFinished()
    {
        if (animator == null)
        {
            return true;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.shortNameHash == attackStateHash && stateInfo.normalizedTime >= 1f;
    }

    public bool IsFacingRight()
    {
        if (spriteRenderer != null)
        {
            return !spriteRenderer.flipX;
        }

        return transform.localScale.x >= 0f;
    }

    public void FaceTarget(Vector3 from, Vector3 to)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (to.x > from.x)
        {
            spriteRenderer.flipX = false;
        }
        else if (to.x < from.x)
        {
            spriteRenderer.flipX = true;
        }
    }
}
