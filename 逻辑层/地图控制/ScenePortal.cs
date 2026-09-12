using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

/// <summary>
/// 关卡传送门：玩家进入触发区后切换到目标关卡。
/// 目标场景的解析方式（二选一）：
///   1. 关卡数据库模式（推荐）：useLevelDatabase = true，
///      从 LevelDatabase 里查"本关卡"的条目，取 jumpableScenes[jumpIndex] 作为目标。
///      好处：关卡之间能跳去哪，统一在 LevelDatabase 资产里配置。
///   2. 直连模式：useLevelDatabase = false，直接填 targetSceneAddress。
///
/// 使用方法：
///   1. 在关卡里创建空物体挂本脚本
///   2. 添加 BoxCollider2D 并勾选 Is Trigger，调整大小盖住门口区域
/// </summary>
public class ScenePortal : MonoBehaviour
{
    // 降级模式（SceneManner 不存在时）自己加载的场景句柄：
    // Addressables 加载的场景必须用句柄卸载
    static readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> fallbackHandles =
        new Dictionary<string, AsyncOperationHandle<SceneInstance>>();
    [Header("从关卡数据库读取目标（推荐）")]
    [SerializeField] private bool useLevelDatabase = true;

    [Tooltip("数据库模式：取本关 jumpableScenes 列表的第几项（0 = 第一项）")]
    [SerializeField] private int jumpIndex;

    [Header("直连模式：手动填目标地址（Addressables 地址，默认为场景资源路径）")]
    [SerializeField] private string targetSceneAddress;

    [Header("只触发一次（防止站在门里反复触发）")]
    [SerializeField] private bool oneShot = true;

    [Header("切换前自动存档")]
    [SerializeField] private bool saveBeforeSwitch;

    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (oneShot && triggered)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        string target = ResolveTarget();
        if (string.IsNullOrEmpty(target))
        {
            return;
        }

        triggered = true;

        if (saveBeforeSwitch && SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame();
        }

        var ownScene = gameObject.scene;
        bool handledByManager = SceneManner.Instance != null;

        if (handledByManager)
        {
            Debug.Log($"传送门 {name}：切换到 {target}");
            SceneManner.Instance.SwitchScene(target);
        }
        else
        {
            Debug.LogWarning($"传送门 {name}：SceneManner 不存在（引导场景未加载），降级为 Addressables 直接加载 {target}");
            var handle = Addressables.LoadSceneAsync(target, LoadSceneMode.Additive);
            if (handle.IsValid())
            {
                fallbackHandles[target] = handle;
            }
            else
            {
                Debug.LogError($"传送门 {name}：Addressables 加载 {target} 失败，请确认场景已注册进 Addressables");
            }
        }

        // 本关卡若不是由 SceneManner 加载的（例如编辑器里多场景直接 Play），
        // SceneManner 不会负责卸载它，这里自己卸，避免新旧关卡叠在一起
        bool managerOwnsThisScene = handledByManager && SceneManner.Instance.CurrentSceneName == ownScene.path;
        if (!managerOwnsThisScene)
        {
            StartCoroutine(UnloadOwnSceneAfterDelay(ownScene));
        }
    }

    /// <summary>解析目标场景地址</summary>
    private string ResolveTarget()
    {
        if (!useLevelDatabase)
        {
            return targetSceneAddress;
        }

        var db = LevelDatabase.Instance;
        if (db == null)
        {
            Debug.LogError("传送门：LevelDatabase 资产缺失（应在 Assets/Resources/Data/LevelDatabase.asset），回退到直连地址");
            return targetSceneAddress;
        }

        var entry = db.GetEntry(gameObject.scene.path);
        if (entry == null)
        {
            Debug.LogError($"传送门：关卡数据库里没有本关卡（{gameObject.scene.path}）的条目");
            return null;
        }

        if (jumpIndex < 0 || jumpIndex >= entry.jumpableScenes.Count)
        {
            Debug.LogError($"传送门：jumpIndex={jumpIndex} 超出 {entry.displayName} 的 jumpableScenes 范围（共 {entry.jumpableScenes.Count} 项）");
            return null;
        }

        return entry.jumpableScenes[jumpIndex];
    }

    private static IEnumerator UnloadOwnSceneAfterDelay(Scene scene)
    {
        yield return new WaitForSeconds(1.5f);
        if (!scene.IsValid() || !scene.isLoaded || SceneManager.sceneCount <= 1)
        {
            yield break;
        }

        var path = scene.path;
        if (fallbackHandles.TryGetValue(path, out var handle) && handle.IsValid())
        {
            // 本场景是降级模式下用 Addressables 加载的，必须用句柄卸载
            Addressables.UnloadSceneAsync(handle);
            fallbackHandles.Remove(path);
        }
        else
        {
            // 本场景是编辑器里叠加打开的，用 SceneManager 卸载
            SceneManager.UnloadSceneAsync(scene);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.8f, 0.5f);
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
    }
}
