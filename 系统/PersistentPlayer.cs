using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]
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
}
