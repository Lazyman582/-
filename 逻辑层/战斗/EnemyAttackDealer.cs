using UnityEngine;

public class EnemyAttackDealer : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private EnemyConfig enemyConfig;
    [SerializeField] private Emery aiController;
    [SerializeField] private EmeryAnimalContrller animationController;

    [Header("Hit Detect")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 attackOffset = new Vector2(0.8f, 0f);
    [SerializeField] private float attackRadius = 0.75f;
    [SerializeField] private LayerMask playerLayerMask;

    public void DealDamageToPlayer()
    {
        if (EventManager.Instance == null)
        {
            return;
        }

        Vector2 hitCenter = GetHitCenter();
        Collider2D hit = Physics2D.OverlapCircle(hitCenter, attackRadius, playerLayerMask);
        if (hit == null)
        {
            return;
        }

        Transform playerTransform = ResolvePlayerTransform(hit);
        if (playerTransform == null)
        {
            return;
        }

        EventManager.Instance.TriggerDamage(GetAttackDamage(), transform.position);

        // 玩家受击震动：沿击退方向（敌人指向玩家）
        if (ScreenShakeManager.Instance != null)
        {
            Vector2 knockDir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            ScreenShakeManager.Instance.Shake(2f, 6, 0.25f, knockDir);
        }
    }

    private Transform ResolvePlayerTransform(Collider2D hit)
    {
        if (PersistentPlayer.Instance != null)
        {
            return PersistentPlayer.Instance.PlayerTransform;
        }

        CharacterData playerData = hit.GetComponentInParent<CharacterData>();
        return playerData != null ? playerData.transform : null;
    }

    private float GetAttackDamage()
    {
        EnemyConfig config = ResolveConfig();
        return config != null ? config.AttackDamage : 10f;
    }

    private EnemyConfig ResolveConfig()
    {
        if (enemyConfig != null)
        {
            return enemyConfig;
        }

        if (aiController == null)
        {
            aiController = GetComponent<Emery>();
        }

        if (aiController != null)
        {
            enemyConfig = aiController.Config;
        }

        return enemyConfig;
    }

    private Vector2 GetHitCenter()
    {
        if (attackPoint != null)
        {
            return attackPoint.position;
        }

        float facing = IsFacingRight() ? 1f : -1f;
        Vector2 offset = new Vector2(attackOffset.x * facing, attackOffset.y);
        return (Vector2)transform.position + offset;
    }

    private bool IsFacingRight()
    {
        if (animationController == null)
        {
            animationController = GetComponent<EmeryAnimalContrller>();
        }

        if (animationController != null)
        {
            return animationController.IsFacingRight();
        }

        return transform.localScale.x >= 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(GetHitCenter(), attackRadius);
    }
}
