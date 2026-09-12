using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;


public class SceneManner : MonoBehaviour
{
    // ==== Inspector 可视化字段 ====
    [Header("当前场景信息（仅供查看）")]
    public string CurrentSceneNameInspector = "无场景加载";
    public string CurrentSceneInfoInspector = "未加载任何场景";

    [Header("基础场景保护")]
    [SerializeField] private List<string> persistentSceneAddresses;
    // ==== 单例模式 ====
    public static SceneManner Instance { get; private set; }

    // 已加载的场景字典（key: 场景名称, value: 场景实例）
    private readonly Dictionary<string, SceneInstance> loadedScenes = new Dictionary<string, SceneInstance>();

    // LRU 缓存策略
    private const int MAX_CACHE_SIZE = 5; // 根据实际需求调整
    private readonly LinkedList<string> usageOrder = new LinkedList<string>(); // 使用顺序记录

    // 预加载场景字典（key: 场景名称, value: 场景实例）
    private readonly Dictionary<string, SceneInstance> preloadedScenes = new Dictionary<string, SceneInstance>();

    // 当前活动场景名称（内部使用）
    private string currentSceneName = string.Empty;

    // 当前活动场景实例（只读属性）
    public SceneInstance CurrentScene =>
        loadedScenes.ContainsKey(currentSceneName) ? loadedScenes[currentSceneName] : default;

    // 当前活动场景名称（只读属性）
    public string CurrentSceneName => currentSceneName;

    private void Awake()
    {
        // 保证单例唯一
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);


      
    }

    #region 场景操作方法

    /// <summary>
    /// 加载场景（如果已经预加载，则直接激活）
    /// </summary>
    public void LoadScene(string sceneAddress)
    {
        // 检查是否已经预加载
        if (preloadedScenes.ContainsKey(sceneAddress))
        {
            // 将预加载的场景移入已加载字典
            var preloadedInstance = preloadedScenes[sceneAddress];
            loadedScenes[sceneAddress] = preloadedInstance;
            preloadedScenes.Remove(sceneAddress);
            Debug.Log($"场景 {sceneAddress} 已预加载，直接激活");

            // 激活新场景
            SceneManager.SetActiveScene(preloadedInstance.Scene);
            currentSceneName = sceneAddress;
            UpdateInspectorInfo(sceneAddress, preloadedInstance);
            UpdateUsageOrder(sceneAddress);
            EnforceCacheLimit();
            return;
        }

        // 防止重复加载
        if (loadedScenes.ContainsKey(sceneAddress))
        {
            Debug.LogWarning($"场景 {sceneAddress} 已经加载过了！");
            UpdateUsageOrder(sceneAddress);
            return;
        }

        // 正常加载场景
        Addressables.LoadSceneAsync(sceneAddress, LoadSceneMode.Additive).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedScenes[sceneAddress] = handle.Result;
                Debug.Log($"场景 {sceneAddress} 加载成功");

                // 激活新场景
                SceneManager.SetActiveScene(handle.Result.Scene);
                currentSceneName = sceneAddress;
                UpdateInspectorInfo(sceneAddress, handle.Result);
                UpdateUsageOrder(sceneAddress);
                EnforceCacheLimit();
            }
            else
            {
                Debug.LogError($"场景 {sceneAddress} 加载失败: {handle.OperationException}");
            }
        };

        Debug.Log(usageOrder.Count);
    }

    /// <summary>
    /// 预加载场景（不激活，只加载资源）
    /// </summary>
    public void PreloadScene(string sceneAddress)
    {
        // 已加载或已预加载则跳过
        if (loadedScenes.ContainsKey(sceneAddress) || preloadedScenes.ContainsKey(sceneAddress))
        {
            Debug.Log($"场景 {sceneAddress} 已经加载或预加载过，跳过");
            return;
        }

        // 开始预加载
        Addressables.LoadSceneAsync(sceneAddress, LoadSceneMode.Additive).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // 暂时隐藏场景，防止影响当前游戏
                handle.Result.ActivateAsync().completed += (op) => {
                    // 这里不做任何处理，只是确保资源完整加载
                };
                preloadedScenes[sceneAddress] = handle.Result;
                Debug.Log($"场景 {sceneAddress} 预加载成功（未激活）");
            }
            else
            {
                Debug.LogError($"场景 {sceneAddress} 预加载失败: {handle.OperationException}");
            }
        };
    }

    /// <summary>
    /// 根据当前场景跳转到指定的下一个场景
    /// </summary>
    public void JumpToNextScene(string nextSceneAddress)
    {
        if (string.IsNullOrEmpty(CurrentSceneName))
        {
            Debug.LogWarning("当前没有活动场景，无法跳转");
            return;
        }

        // 这里可以添加逻辑检查，确保 nextSceneAddress 在当前场景的可跳转列表中
        // 例如：如果你有一个场景数据表可以验证
        // if (!SceneDataValidator.IsValidNextScene(CurrentSceneName, nextSceneAddress)) { ... }

        // 切换场景
        SwitchScene(nextSceneAddress);
    }

    /// <summary>
    /// 卸载场景
    /// </summary>
    public void UnloadScene(string sceneAddress)
    {
        if (!loadedScenes.ContainsKey(sceneAddress))
        {
            Debug.LogWarning($"场景 {sceneAddress} 未加载或已经卸载");
            return;
        }

        // 卸载场景
        Addressables.UnloadSceneAsync(loadedScenes[sceneAddress]).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"场景 {sceneAddress} 卸载成功");
                loadedScenes.Remove(sceneAddress);
                usageOrder.Remove(sceneAddress);

                // 如果卸载的是当前活动场景，需要清空 Inspector 信息
                if (currentSceneName == sceneAddress)
                {
                    currentSceneName = string.Empty;
                    CurrentSceneNameInspector = "无场景加载";
                    CurrentSceneInfoInspector = "未加载任何场景";
                }
            }
            else
            {
                Debug.LogError($"场景 {sceneAddress} 卸载失败: {handle.OperationException}");
            }
        };
    }

    /// <summary>
    /// 切换到另一个场景（加载新场景并卸载旧场景）
    /// </summary>
    public void SwitchScene(string newSceneAddress)
    {
        // 加载新场景
        LoadScene(newSceneAddress);

        // 卸载旧场景（如果有）
        if (!string.IsNullOrEmpty(currentSceneName) && loadedScenes.ContainsKey(currentSceneName))
        {
            // 延迟卸载，确保新场景加载完成后再卸载旧场景
            Invoke(nameof(UnloadCurrentScene), 1f);
        }
    }

    /// <summary>
    /// 卸载当前活动场景（内部调用）
    /// </summary>
    private void UnloadCurrentScene()
    {
        if (!string.IsNullOrEmpty(currentSceneName) && loadedScenes.ContainsKey(currentSceneName))
        {
            UnloadScene(currentSceneName);
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 更新 Inspector 中的场景信息显示
    /// </summary>
    private void UpdateInspectorInfo(string sceneAddress, SceneInstance instance)
    {
        CurrentSceneNameInspector = sceneAddress;
        CurrentSceneInfoInspector = $"场景名称: {instance.Scene.name}\n根对象数量: {instance.Scene.rootCount}";
    }

    /// <summary>
    /// 更新场景的使用顺序（LRU策略）
    /// </summary>
    private void UpdateUsageOrder(string sceneAddress)
    {
        // 如果已经存在，先移除旧的节点
        if (usageOrder.Contains(sceneAddress))
        {
            usageOrder.Remove(sceneAddress);
        }

        // 将最新使用的场景放到最前面
        usageOrder.AddFirst(sceneAddress);
    }

    /// <summary>
    /// 强制执行缓存上限，如果超过阈值则卸载最久未使用的场景
    /// </summary>
    private void EnforceCacheLimit()
    {
        while (usageOrder.Count > MAX_CACHE_SIZE)
        {
            string leastUsedScene = usageOrder.Last.Value;

            // ✅ 检查是否是基础场景
            if (persistentSceneAddresses.Contains(leastUsedScene))
            {
                // 如果是基础场景，跳过并检查下一个
                Debug.Log($"基础场景 {leastUsedScene} 受保护，不会被卸载");

                // 把它移到前面，表示最近使用，避免下次又被选中
                UpdateUsageOrder(leastUsedScene);

                // 如果只剩基础场景了，就停止清理
                if (usageOrder.Count <= persistentSceneAddresses.Count)
                    break;

                continue;
            }

            // 检查是否是当前活动场景
            if (leastUsedScene == currentSceneName)
            {
                // 当前场景也不能卸载
                UpdateUsageOrder(leastUsedScene);
                continue;
            }

            Debug.Log($"缓存已满，自动卸载最久未使用的场景: {leastUsedScene}");
            UnloadScene(leastUsedScene);
        }
    }
}

    #endregion

