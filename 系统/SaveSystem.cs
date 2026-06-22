using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveSystem : Singleton<SaveSystem>
{
    private const string SaveFileName = "save.json";

    [Header("PlayerData")]
    [SerializeField] private CharacterData characterData;

    public Action<GameSaveData> OnSaveCompleted;
    public Action<GameSaveData> OnLoadCompleted;

    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public CharacterData CharacterData
    {
        get
        {
            if (characterData == null)
            {
                characterData = PersistentPlayer.Instance != null
                    ? PersistentPlayer.Instance.CharacterData
                    : FindObjectOfType<CharacterData>();
            }

            return characterData;
        }
    }

    private void Awake()
    {
        characterData = PersistentPlayer.Instance != null
            ? PersistentPlayer.Instance.CharacterData
            : FindObjectOfType<CharacterData>();
    }

    public bool HasSaveData()
    {
        return File.Exists(SavePath);
    }

    public GameSaveData SaveGame()
    {
        var player = CharacterData;
        if (player == null)
        {
            Debug.LogError("SaveSystem: CharacterData not found, save aborted.");
            return null;
        }

        var saveData = new GameSaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            character = player.ExportSaveData()
        };

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(SavePath, json);
        OnSaveCompleted?.Invoke(saveData);

        Debug.Log($"SaveSystem: save completed -> {SavePath}");
        return saveData;
    }

    public GameSaveData LoadGame()
    {
        if (!HasSaveData())
        {
            Debug.LogWarning("SaveSystem: save file not found.");
            return null;
        }

        string json = File.ReadAllText(SavePath);
        var saveData = JsonUtility.FromJson<GameSaveData>(json);
        if (saveData == null)
        {
            Debug.LogError("SaveSystem: failed to parse save file.");
            return null;
        }

        var player = CharacterData;
        if (player == null)
        {
            Debug.LogError("SaveSystem: CharacterData not found, load apply skipped.");
            return saveData;
        }

        player.ApplySaveData(saveData.character);
        OnLoadCompleted?.Invoke(saveData);
        Debug.Log($"SaveSystem: load completed <- {SavePath}");
        return saveData;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SaveGame();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            LoadGame();
        }
    }
}
