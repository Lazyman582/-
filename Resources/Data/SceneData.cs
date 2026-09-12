using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneData", menuName = "Game/SceneData")]
public class SceneData : ScriptableObject
{
    [Header("场景唯一标识（Addressable 名称）")]
    public string sceneAddress; // 场景的 Addressable 地址

    [Header("当前场景可直接跳转到的目标场景列表")]
    [Tooltip("设置当前场景可以跳转的目标场景（Addressable 名称）")]
    public List<string> nextSceneAddresses = new List<string>();
}