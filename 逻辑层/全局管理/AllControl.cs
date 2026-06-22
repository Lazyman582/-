using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllControl : MonoBehaviour
{
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
public static class AllWay{

    
    public static T FindComponent<T>(out string errorMsg) where T : Component
    {
        errorMsg = string.Empty;

        // 1. 通过对象名查找（假设对象名与组件名相同）
        GameObject obj = GameObject.Find(typeof(T).Name);
        if (obj != null)
        {
            T component = obj.GetComponent<T>();
            if (component != null) return component;
        }

        // 2. 使用 UnityEngine.Object 的静态方法查找（更通用）
        //    注意：这里使用了 Object.FindObjectOfType<T>()，而不是 FindObjectOfType<T>()
        T found = Object.FindObjectOfType<T>();
        if (found != null) return found;

        // 3. 未找到，返回错误信息
        errorMsg = $"AllWay: 未找到 {typeof(T).Name} 组件！请确保场景中有挂载该脚本的对象。";
        return null;
    }
    public static T FindComponent<T>() where T : Component
    {
        // 直接调用完整版，丢弃错误信息
        return FindComponent<T>(out _);
    }


}