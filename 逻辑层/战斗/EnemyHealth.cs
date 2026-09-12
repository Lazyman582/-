using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private EnemyConfig enemyConfig;
    [SerializeField] private float currentHealth = 30f;

    [Header("Death")]
    [SerializeField] private Emery aiController;
    [SerializeField] private Rigidbody2D targetRigidbody;
    [SerializeField] private Collider2D[] hitColliders;
    [Header("动画")]
    [SerializeField] private EmeryAnimalContrller animalContrller;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color originalColor;
    public float MaxHealth => ResolveMaxHealth();
    public float CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        if (aiController == null)
        {
            aiController = GetComponent<Emery>();
        }

        if (enemyConfig == null && aiController != null)
        {
            enemyConfig = aiController.Config;
        }

        if (targetRigidbody == null)
        {
            targetRigidbody = GetComponent<Rigidbody2D>();
        }

        if (animalContrller == null) {


            animalContrller = GetComponent<EmeryAnimalContrller>();


        }

        if (hitColliders == null || hitColliders.Length == 0)
        {
            hitColliders = GetComponentsInChildren<Collider2D>(true);
        }
        // 获取 SpriteRenderer 组件
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // 记录原始颜色
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);
        if (currentHealth <= 0f)
        {
            currentHealth = MaxHealth;
        }




    }

    public void TakeDamage(float amount, Vector3 attackerPosition)
    {
        if (IsDead || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, MaxHealth);
        if (animalContrller != null)
        {
            FlashHurt();
            if (currentHealth <= 0f)
            {
                Die();
            }
           
           
        }
       
    }

    public void RestoreFullHealth()
    {
        IsDead = false;
        currentHealth = MaxHealth;
    }

    public void FlashHurt()
    {
        Invoke(nameof(ResetColor), 0.1f);
        spriteRenderer.color = Color.red;
    }

    void ResetColor()
    {
        spriteRenderer.color = originalColor;
    }
    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;

        if (aiController != null)
        {
            aiController.enabled = false;
        }

        if (targetRigidbody != null)
        {
            targetRigidbody.velocity = Vector2.zero;
            targetRigidbody.angularVelocity = 0f;
            targetRigidbody.simulated = false;
        }
      
        if (hitColliders != null)
        {
            for (int i = 0; i < hitColliders.Length; i++)
            {
                if (hitColliders[i] != null)
                {
                    hitColliders[i].enabled = false;
                }
            }
        }

        if (animalContrller!=null) {

            animalContrller.SetDeath(true);
            animalContrller.SetMove(false);
            animalContrller.SetAttack(false);
        }


        
    }

    public void HandleDeathAnimationEvent()
    {
        if (ShouldDestroyOnDeath())
        {
            Destroy(gameObject, GetDestroyDelay());
        }
    }
    private float ResolveMaxHealth()
    {
        return enemyConfig != null ? enemyConfig.MaxHealth : 30f;
    }

    private bool ShouldDestroyOnDeath()
    {
        return enemyConfig == null || enemyConfig.DestroyOnDeath;
    }

    private float GetDestroyDelay()
    {
        return enemyConfig != null ? enemyConfig.DestroyDelay : 0.1f;
    }
}
