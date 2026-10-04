using System.Collections;
using UnityEngine;

/// <summary>
/// 主菜单的"开始游戏"流程控制器。
/// 挂载在 MainUI 场景里（如一个空的 GameStarter 物体），
/// 由"游戏开始"按钮的 OnClick 调用 StartGame()。
///
/// 流程：黑屏 → 按依赖顺序逐个叠加加载场景（管理器 → 角色 → HUD → 第一关）→
/// 等主菜单卸载完成 → 恢复画面。
/// 协程跑在 SimpleFade（DontDestroyOnLoad）上，主菜单被卸载后流程不会中断。
/// 角色落位不需要额外代码：PersistentPlayer 监听 sceneLoaded，按关卡里的
/// PlayerSpawnPoint 自动出生（它已处理"关卡先于角色加载"的时序竞态）。
/// </summary>
public class StartGameController : MonoBehaviour
{
    [Header("游戏基础场景（Addressables 地址 = 场景资源路径）")]
    [SerializeField] private string managerSceneAddress = "Assets/Scenes/Mannerger.unity";
    [SerializeField] private string playerSceneAddress = "Assets/Scenes/常驻数据.unity";
    [SerializeField] private string uiSceneAddress = "Assets/Scenes/UI.unity";

    [Header("第一个关卡")]
    [Tooltip("与 LevelDatabase 中第一关的 sceneAddress 保持一致")]
    [SerializeField] private string firstLevelAddress = "Assets/关卡一.unity";

    [Header("主菜单场景（开始游戏后被卸载）")]
    [SerializeField] private string mainMenuAddress = "Assets/Scenes/MainUI.unity";

    [Header("加载等待上限（秒）")]
    [SerializeField] private float loadTimeout = 15f;

    private bool _starting = false;
    private bool _loadFailed = false;

    /// <summary>由主菜单"游戏开始"按钮 OnClick 调用</summary>
    public void StartGame()
    {
        if (_starting)
        {
            Debug.LogWarning("StartGameController: 已在启动流程中，忽略重复点击");
            return;
        }
        _starting = true;
        AudioManager.Instance?.PlayButtonStart();
        // 协程必须跑在 SimpleFade 上：主菜单会在切关时被卸载，
        // 挂在本物体上的协程会随之终止，导致黑屏无法恢复。
        var fade = SimpleFade.Show();
        fade.StartCoroutine(StartGameRoutine());
    }

    /// <summary>由主菜单"退出"按钮 OnClick 调用</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator StartGameRoutine()
    {
        var fade = SimpleFade.Instance;

        // 1. 黑屏
        if (fade != null)
        {
            yield return fade.FadeOut();
        }

        var manner = SceneManner.Instance;
        if (manner == null)
        {
            Debug.LogError("StartGameController: SceneManner 不存在（引导场景未加载），无法开始游戏");
            yield return Abort(fade);
            yield break;
        }

        // 2. 按依赖顺序逐个加载（不再并发）：
        //    管理器（UserInput/EventManager 等）→ 角色（StateController/CharacterMovement 依赖管理器）
        //    → HUD（依赖角色）→ 第一关。避免跨场景 Awake/Start 顺序竞争。
        yield return LoadAndWait(manner, managerSceneAddress);
        if (_loadFailed) { yield return Abort(fade); yield break; }

        yield return LoadAndWait(manner, playerSceneAddress);
        if (_loadFailed) { yield return Abort(fade); yield break; }

        yield return LoadAndWait(manner, uiSceneAddress);
        if (_loadFailed) { yield return Abort(fade); yield break; }

        // 3. 加载第一关，然后显式卸载主菜单。
        //    注意不能用 SwitchScene：串行叠加之后 currentSceneName 是最后加载的 UI，
        //    SwitchScene 会把 UI 当成"旧场景"卸载掉——主菜单必须在这里显式卸载。
        yield return LoadAndWait(manner, firstLevelAddress);
        if (_loadFailed) { yield return Abort(fade); yield break; }

        manner.UnloadScene(mainMenuAddress);

        // 4. 等主菜单卸载完成（卸载是异步的），保证淡入时无菜单残留
        float unloadStart = Time.unscaledTime;
        while (manner.IsSceneTracked(mainMenuAddress) && Time.unscaledTime - unloadStart < 5f)
        {
            yield return null;
        }

        // 5. 等一帧，让 sceneLoaded 相关的出生点定位、相机绑定逻辑先跑完
        yield return null;

        // 6. 恢复画面
        if (fade != null)
        {
            yield return fade.FadeIn();
        }
    }

    /// <summary>
    /// 发起单个场景加载并等待完成。
    /// 依赖 SceneManner.LoadScene 新增的 onCompleted 回调：
    /// 成功（进入已加载字典）或失败都会及时回调，超时只是兜底。
    /// </summary>
    private IEnumerator LoadAndWait(SceneManner manner, string address)
    {
        _loadFailed = false;
        bool done = false;
        bool success = false;

        manner.LoadScene(address, onCompleted: s =>
        {
            success = s;
            done = true;
        });

        float startTime = Time.unscaledTime;
        while (!done && Time.unscaledTime - startTime < loadTimeout)
        {
            yield return null;
        }

        if (!done)
        {
            Debug.LogError($"StartGameController: 加载超时：{address}");
            _loadFailed = true;
        }
        else if (!success)
        {
            Debug.LogError($"StartGameController: 加载失败：{address}，请检查 Addressables 标记");
            _loadFailed = true;
        }
    }

    private IEnumerator Abort(SimpleFade fade)
    {
        _starting = false;
        if (fade != null)
        {
            yield return fade.FadeIn();
        }
    }
}
