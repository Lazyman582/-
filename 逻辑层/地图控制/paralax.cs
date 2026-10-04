using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class paralax : MonoBehaviour
{

    private Transform _mainCamera;

    [SerializeField] private float speed = 1f;

    // Only start following after the player enters the trigger area.
    // Turned on by BackgroundFollowTrigger.StartFollow().
    private bool _isFollowing = false;

    // 是否已经触发过一次跟随。static：场景卸载重载后仍然保留，
    // 从别的关卡返回、场景重载时，背景可以立即恢复跟随，不用再走一次碰撞检测。
    private static bool _followEverStarted = false;

    // 每个场景首次触发跟随时记录的"基准相机 x"（背景摆放位置与之对应的时刻）。
    // static：场景重载后仍然保留，返回关卡时用它重算背景位置。
    private static readonly Dictionary<string, float> _sceneOriginCamX = new Dictionary<string, float>();

    // 背景在场景里的摆放位置 x（每次加载时从场景读取，是稳定的设计值）
    private float _originBgX;

    void Start()
    {
        AcquireCamera();
        // 之前已触发过跟随（如从别的关卡返回、场景重载）时，直接恢复跟随状态
        _isFollowing = _followEverStarted;
        _originBgX = transform.position.x;
        // 切场景后旧摄像机会被卸载，场景加载完成时重新抓取
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AcquireCamera();
    }

    private void AcquireCamera()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            _mainCamera = cam.transform;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        ParallaxMove();
    }

    private void ParallaxMove() {

        // 缓存的摄像机已被销毁（旧关卡卸载）时，重新抓取当前主摄像机
        if (_mainCamera == null)
        {
            AcquireCamera();
            if (_mainCamera == null)
                return;
        }

        if (!_isFollowing)
            return;

        // 位置式视差：背景 x = 摆放基准 + (相机相对基准的位移 × 速度)。
        // 每帧直接算位置而不是累积位移：场景重载、传送后自动对齐，不会漂移。
        if (_sceneOriginCamX.TryGetValue(gameObject.scene.name, out float originCamX))
        {
            Vector3 p = transform.position;
            p.x = _originBgX + (_mainCamera.position.x - originCamX) * speed;
            transform.position = p;
        }

    }

    // Called by BackgroundFollowTrigger when the player enters its trigger area.
    public void StartFollow()
    {
        _isFollowing = true;
        _followEverStarted = true;

        // 首次触发时记录本场景的基准相机 x（背景摆放与相机位置的对应关系）
        string sceneKey = gameObject.scene.name;
        if (!_sceneOriginCamX.ContainsKey(sceneKey))
        {
            _sceneOriginCamX[sceneKey] = _mainCamera != null ? _mainCamera.position.x : 0f;
        }
    }


}
