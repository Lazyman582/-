using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackgroundScrolller : MonoBehaviour
{
    // 背景
    private Camera mainCamera;
    private float bgWidth; // 背景宽度

    void Start()
    {
        AcquireCamera();
        getBgWidth();
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
            mainCamera = cam;
        }
    }

    void Update()
    {
        BgMove();
    }

    // 获取背景显示宽度

    // 设置背景的显示宽度
    public void getBgWidth()
    {
       SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        bgWidth = spriteRenderer.bounds.size.x;
        Debug.Log(bgWidth);
    }

    public void BgMove() {

        // 缓存的摄像机已被销毁（旧关卡卸载）时，重新抓取当前主摄像机
        if (mainCamera == null)
        {
            AcquireCamera();
            if (mainCamera == null)
                return;
        }

        float distance = mainCamera.transform.position.x - transform.position.x;
        if (Mathf.Abs(distance)> bgWidth) {


            transform.position += Vector3.right * bgWidth * 2 * Mathf.Sign(distance);

        }

    }
}
