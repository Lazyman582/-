using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static bool applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
                return null;

            if (instance == null)
            {
                // 1. 先在场景中查找是否有已有的实例
                instance = FindObjectOfType<T>();

                // 2. 没找到才新建
                if (instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(T).Name + "<Singleton>";
                    instance = obj.AddComponent<T>();
                    DontDestroyOnLoad(obj);
                }
            }

            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);  // 场景预制体也被标记为跨场景存活
        }
        else if (instance != this)
        {
            Destroy(gameObject);  // 重复实例，销毁
        }
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }
}