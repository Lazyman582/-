using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundScrolller : MonoBehaviour
{
    // 背景
    private Camera mainCamera;
    private float bgWidth; // 背景宽度

    void Start()
    {
        mainCamera = Camera.main;
        getBgWidth();
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


        float distance = mainCamera.transform.position.x - transform.position.x;
        if (Mathf.Abs(distance)> bgWidth) {


            transform.position += Vector3.right * bgWidth * 2 * Mathf.Sign(distance);
        
        }
    
    }
}