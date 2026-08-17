using System;
using System.Collections.Generic;
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
            savedAt   = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            character = player.ExportSaveData(),
            inventory = ExportInventory()
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
        ApplyInventory(saveData.inventory);
        OnLoadCompleted?.Invoke(saveData);
        Debug.Log($"SaveSystem: load completed <- {SavePath}");
        return saveData;
    }

    // ==================== 背包序列化 ====================

    private static List<InventorySlotSaveData> ExportInventory()
    {
        var result = new List<InventorySlotSaveData>();
        var slots  = InventoryManager.Instance.slots;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.itemConfig != null && slot.count > 0)
            {
                result.Add(new InventorySlotSaveData
                {
                    slotIndex = i,
                    itemId    = slot.itemConfig.ItemId,
                    count     = slot.count
                });
            }
        }
        return result;
    }

    private static void ApplyInventory(List<InventorySlotSaveData> saveData)
    {
        var manager = InventoryManager.Instance;
        manager.slots.Clear();

        if (saveData == null || saveData.Count == 0) return;

        saveData.Sort((a, b) => a.slotIndex.CompareTo(b.slotIndex));

        foreach (var entry in saveData)
        {
            var cfg = manager.GetConfig(entry.itemId);
            if (cfg == null)
            {
                Debug.LogWarning($"[SaveSystem] 存档中的物品 '{entry.itemId}' 未找到配置，已跳过");
                continue;
            }

            while (manager.slots.Count < entry.slotIndex)
                manager.slots.Add(new InventorySlot(null, 0));

            manager.slots.Add(new InventorySlot(cfg, entry.count));
        }

        manager.NotifyChanged();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            SaveGame();

        if (Input.GetKeyDown(KeyCode.T))
            LoadGame();
    }
}
