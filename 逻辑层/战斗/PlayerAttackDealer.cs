using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackDealer : MonoBehaviour
{
    [Header("Hit Detect")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 attackOffset = new Vector2(0.8f, 0f);
    [SerializeField] private float attackRadius = 0.75f;
    [SerializeField] private LayerMask enemyLayerMask;

    [Header("Damage")]
    [SerializeField] private float defaultDamage = 10f;
    [SerializeField] private float[] comboDamages = { 10f, 12f, 15f };

    public void DealDamage(int comboIndex)
    {
        Vector2 hitCenter = GetHitCenter();
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, attackRadius, enemyLayerMask);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        // 命中音效（与挥刀音效独立，每次挥刀只有真正打中敌人才触发）
        AudioManager.Instance?.PlayAttackHit();
        TimeManager.Instance.Freeze(0.1f,0.5f);
        // 命中震动：沿攻击方向短促抖动
        if (ScreenShakeManager.Instance != null)
        {
            float facing = transform.localScale.x >= 0f ? 1f : -1f;
            ScreenShakeManager.Instance.Shake(0.4f,3, 0.1f, new Vector2(facing, 0.3f));
        }
        var processedTargets = new HashSet<EnemyHealth>();
        float damage = GetDamage(comboIndex);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            EnemyHealth enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            if (enemyHealth == null || enemyHealth.IsDead || !processedTargets.Add(enemyHealth))
            {
                continue;
            }

            enemyHealth.TakeDamage(damage, transform.position);
        }
    }

    private float GetDamage(int comboIndex)
    {
        if (comboDamages == null || comboDamages.Length == 0)
        {
            return defaultDamage;
        }

        int index = Mathf.Clamp(comboIndex - 1, 0, comboDamages.Length - 1);
        return comboDamages[index];
    }

    private Vector2 GetHitCenter()
    {
        if (attackPoint != null)
        {
            return attackPoint.position;
        }

        float facing = transform.localScale.x >= 0f ? 1f : -1f;
        Vector2 offset = new Vector2(attackOffset.x * facing, attackOffset.y);
        return (Vector2)transform.position + offset;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(GetHitCenter(), attackRadius);
    }
}
