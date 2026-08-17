using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-300)]
public class PersistentPlayer : MonoBehaviour
{
    public static PersistentPlayer Instance { get; private set; }

    public CharacterData CharacterData { get; private set; }
    public CharacterMovement CharacterMovement { get; private set; }
    public Transform PlayerTransform => transform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RefreshCachedComponents();
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        TryInitialSpawn();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    public void RefreshCachedComponents()
    {
        CharacterData = GetComponent<CharacterData>();
        CharacterMovement = GetComponent<CharacterMovement>();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        MoveToSceneSpawn(scene);
    }

    private void MoveToSceneSpawn(Scene scene)
    {
        PlayerSpawnPoint selectedSpawn = null;

        foreach (var spawn in FindObjectsOfType<PlayerSpawnPoint>(true))
        {
            if (!spawn.UseOnSceneLoad || spawn.gameObject.scene != scene)
            {
                continue;
            }

            if (selectedSpawn == null || spawn.Priority > selectedSpawn.Priority)
            {
                selectedSpawn = spawn;
            }
        }

        if (selectedSpawn != null)
        {
            transform.position = selectedSpawn.transform.position;
        }
    }

    /// <summary>
    /// 角色首次激活时，在已加载的所有场景中找出生点
    /// —— 解决关卡先于角色加载、sceneLoaded 事件已错过的问题
    /// </summary>
    private void TryInitialSpawn()
    {
        PlayerSpawnPoint best = null;

        foreach (var spawn in FindObjectsOfType<PlayerSpawnPoint>(true))
        {
            if (!spawn.UseOnSceneLoad) continue;

            if (best == null || spawn.Priority > best.Priority)
                best = spawn;
        }

        if (best != null)
        {
            transform.position = best.transform.position;
        }
    }
}
