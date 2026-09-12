using UnityEngine;

public class Savepoint : MonoBehaviour
{
    [SerializeField] private float range = 2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private CharacterData player;
    [SerializeField] private SpriteRenderer interactionSprite;
    [SerializeField] private bool loadOnStart;

    private bool canInteract;

    private void OnEnable()
    {
        if (player == null)
        {
            player = FindObjectOfType<CharacterData>();
        }
    }

    private void Start()
    {
        if (loadOnStart)
        {
            SaveSystem.Instance.LoadGame();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        canInteract = true;
        if (interactionSprite != null)
        {
            interactionSprite.enabled = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        canInteract = false;
        if (interactionSprite != null)
        {
            interactionSprite.enabled = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(interactKey) && canInteract)
        {
            SaveSystem.Instance.SaveGame();
            Debug.Log("Game Saved!");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
