using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NPC — 接近检测 + 触发对话，其余由 DialogueController / DialogueUI 接管
/// </summary>
public class NPC : MonoBehaviour
{
    [Header("对话数据")]
    [SerializeField] private DialogueData dialogueData;

    [Header("对话后动作")]
    [Tooltip("对话结束后执行的动作列表（给予物品等），可为空")]
    [SerializeField] private DialogueAction[] onDialogueEndActions;

    [Header("交互")]
    [SerializeField] private GameObject interactPrompt;

    private bool playerInRange;
    private bool inDialogue;
    private bool eventSubscribed;

    private DialogueController controller;

    private void Start()
    {
        controller = DialogueController.Instance;
        if (controller != null)
            controller.OnDialogueEnded += HandleDialogueEnded;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TrySubscribeInteract();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnsubscribeInteract();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnsubscribeInteract();
        if (controller != null)
            controller.OnDialogueEnded -= HandleDialogueEnded;
    }

    /// <summary>场景加载后重试订阅（解决多场景加载顺序问题）</summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySubscribeInteract();
    }

    private void TrySubscribeInteract()
    {
        if (eventSubscribed) return;

        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnInteractRequested.Subscribe(HandleInteract, priority: 10);
            eventSubscribed = true;
            Debug.Log($"[NPC] {gameObject.name} 订阅 OnInteractRequested 成功");
        }
        else
        {
            Debug.LogWarning($"[NPC] {gameObject.name} 无法订阅：EventManager.Instance 为 null，等待场景加载重试...");
        }
    }

    private void UnsubscribeInteract()
    {
        if (!eventSubscribed) return;

        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnInteractRequested.Unsubscribe(HandleInteract);
        }
        eventSubscribed = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (interactPrompt != null && !inDialogue)
                interactPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (interactPrompt != null)
                interactPrompt.SetActive(false);
        }
    }

    private void HandleInteract()
    {
        Debug.Log($"[NPC] {gameObject.name} HandleInteract: playerInRange={playerInRange}, inDialogue={inDialogue}, hasData={dialogueData != null}");

        if (playerInRange && !inDialogue && dialogueData != null)
        {
            inDialogue = true;
            if (interactPrompt != null)
                interactPrompt.SetActive(false);
            controller?.StartDialogue(dialogueData);
        }
       
    }

    private void HandleDialogueEnded()
    {
        // 执行对话后动作（发物品等）
        if (onDialogueEndActions != null)
        {
            foreach (var action in onDialogueEndActions)
                action?.Execute();
        }

        StartCoroutine(CooldownRoutine());
    }

    private System.Collections.IEnumerator CooldownRoutine()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        inDialogue = false;
        if (playerInRange && interactPrompt != null)
            interactPrompt.SetActive(true);
    }
}
