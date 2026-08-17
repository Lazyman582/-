using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Game/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("Basic")]
    [SerializeField] private string enemyId = "enemy";
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private float moveSpeed = 10f;

    [Header("Combat")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackDuration = 0.45f;
    [SerializeField] private float attackCooldown = 0.25f;
    [SerializeField] private float seeRadius = 2f;

    [Header("Death")]
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 0.1f;

    public string EnemyId => enemyId;
    public float MaxHealth => Mathf.Max(1f, maxHealth);
    public float MoveSpeed => Mathf.Max(0f, moveSpeed);
    public float AttackDamage => Mathf.Max(0f, attackDamage);
    public float AttackRange => Mathf.Max(0f, attackRange);
    public float AttackDuration => Mathf.Max(0.05f, attackDuration);
    public float AttackCooldown => Mathf.Max(0f, attackCooldown);
    public float SeeRadius => Mathf.Max(0f, seeRadius);
    public bool DestroyOnDeath => destroyOnDeath;
    public float DestroyDelay => Mathf.Max(0f, destroyDelay);
}
