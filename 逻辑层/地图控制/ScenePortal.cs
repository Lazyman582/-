using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    // 降级模式（SceneManner 不存在时）自己加载的场景句柄：
    // Addressables 加载的场景必须用句柄卸载
    static readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> fallbackHandles =
        new Dictionary<string, AsyncOperationHandle<SceneInstance>>();

    // 到达后的短暂冷却：防止玩家被放到回程门位置时立刻又触发传送
    private static float arrivalCooldownUntil;

    [Header("从关卡数据库读取目标（推荐）")]
    [SerializeField] private bool useLevelDatabase = true;

    [Tooltip("数据库模式：取本关 jumpableScenes 列表的第几项（0 = 第一项）")]
    [SerializeField] private int jumpIndex;

    [Header("直连模式：手动填目标地址（Addressables 地址，默认为场景资源路径）")]
    [SerializeField] private string targetSceneAddress;

    [Header("黑屏最短持续时间（秒），保证转场不闪现")]
    [SerializeField] private float minTransitionDuration = 0.5f;

    [Tooltip("从回程门出来时相对门中心的落点偏移")]
    [SerializeField] private Vector2 arrivalOffset = new Vector2(0f, -0.6f);

    [Header("只触发一次（防止站在门里反复触发）")]
    [SerializeField] private bool oneShot = true;

    [Header("切换前自动存档")]
    [SerializeField] private bool saveBeforeSwitch;

    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 刚通过别的传送门到达，冷却期内不再触发
        if (Time.unscaledTime < arrivalCooldownUntil)
        {
            return;
        }

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
        Transform player = other.transform;

   
        var fade = SimpleFade.Show();
        if (fade != null)
        {
            fade.StartCoroutine(TransitionRoutine(ownScene, target, player));
        }
        else
        {
            StartCoroutine(TransitionRoutine(ownScene, target, player));
        }
    }

    private IEnumerator TransitionRoutine(Scene ownScene, string target, Transform player)
    {
        // 1. 变黑
        var fade = SimpleFade.Instance;
        if (fade != null)
        {
            yield return fade.FadeOut();
        }

        float startTime = Time.unscaledTime;

        // 2. 异步加载新关卡（黑屏期间进行）
        bool handledByManager = SceneManner.Instance != null;
        Scene arrivedScene = default;
        if (handledByManager)
        {
            Debug.Log($"传送门 {name}：异步切换到 {target}");
            SceneManner.Instance.LoadScene(target);

            // 等 SceneManner 把新场景标记为当前（内部也是 Addressables 异步）
            float guard = startTime;
            while (SceneManner.Instance.CurrentSceneName != target && Time.unscaledTime - guard < 15f)
            {
                yield return null;
            }
            arrivedScene = SceneManager.GetSceneByPath(target);
        }
        else
        {
            Debug.LogWarning($"传送门 {name}：SceneManner 不存在（引导场景未加载），降级为 Addressables 异步加载 {target}");
            var handle = Addressables.LoadSceneAsync(target, LoadSceneMode.Additive);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                fallbackHandles[target] = handle;
                arrivedScene = handle.Result.Scene;
                SceneManager.SetActiveScene(arrivedScene);
            }
            else
            {
                Debug.LogError($"传送门 {name}：加载 {target} 失败：{handle.OperationException}");
                triggered = false;
                if (fade != null) yield return fade.FadeIn();
                yield break;
            }
        }

        // 3. 到达点：优先放在目标场景里指向来源场景的回程传送门处
        ApplyArrivalPosition(arrivedScene, ownScene.path, player);

        // 4. 黑屏至少持续 minTransitionDuration 秒（加载快也不闪现）
        while (Time.unscaledTime - startTime < minTransitionDuration)
        {
            yield return null;
        }

        // 5. 黑屏期间卸载旧关卡，避免新旧关卡重叠（双 AudioListener 等）
        if (handledByManager && SceneManner.Instance.IsSceneTracked(ownScene.path))
        {
            SceneManner.Instance.UnloadScene(ownScene.path);
        }
        else
        {
            yield return UnloadOwnScene(ownScene);
        }

        // 6. 恢复画面
        if (fade != null)
        {
            yield return fade.FadeIn();
        }
    }

    private static void ApplyArrivalPosition(Scene arrivedScene, string fromAddress, Transform player)
    {
        if (!arrivedScene.IsValid() || player == null)
        {
            return;
        }

        foreach (var portal in Object.FindObjectsOfType<ScenePortal>(true))
        {
            if (portal.gameObject.scene != arrivedScene)
            {
                continue;
            }

            if (portal.ResolveTarget(false) != fromAddress)
            {
                continue;
            }

            player.position = portal.transform.position + (Vector3)portal.arrivalOffset;
            arrivalCooldownUntil = Time.unscaledTime + 2f;
            Debug.Log($"传送门：玩家经回程门 {portal.name} 到达 {player.position}");
            return;
        }
    }

    /// <summary>解析目标场景地址</summary>
    private string ResolveTarget()
    {
        return ResolveTarget(true);
    }

    private string ResolveTarget(bool logErrors)
    {
        if (!useLevelDatabase)
        {
            return targetSceneAddress;
        }

        var db = LevelDatabase.Instance;
        if (db == null)
        {
            if (logErrors) Debug.LogError("传送门：LevelDatabase 资产缺失，回退到直连地址");
            return targetSceneAddress;
        }

        var entry = db.GetEntry(gameObject.scene.path);
        if (entry == null)
        {
            if (logErrors) Debug.LogError($"传送门：关卡数据库里没有本关卡（{gameObject.scene.path}）的条目");
            return null;
        }

        if (jumpIndex < 0 || jumpIndex >= entry.jumpableScenes.Count)
        {
            if (logErrors) Debug.LogError($"传送门：jumpIndex={jumpIndex} 超出 {entry.displayName} 的 jumpableScenes 范围（共 {entry.jumpableScenes.Count} 项）");
            return null;
        }

        return entry.jumpableScenes[jumpIndex];
    }

    /// <summary>卸载旧关卡：Addressables 加载的用句柄卸载，编辑器叠加打开的用 SceneManager 卸载</summary>
    private static IEnumerator UnloadOwnScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            yield break;
        }

        var path = scene.path;
        if (fallbackHandles.TryGetValue(path, out var handle) && handle.IsValid())
        {
            var unloadOp = Addressables.UnloadSceneAsync(handle);
            fallbackHandles.Remove(path);
            while (!unloadOp.IsDone)
            {
                yield return null;
            }
        }
        else
        {
            var op = SceneManager.UnloadSceneAsync(scene);
            while (op != null && !op.isDone)
            {
                yield return null;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.8f, 0.5f);
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
    }
}
