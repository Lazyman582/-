using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MusicManager : MonoBehaviour
{
    [Header("音频源设置")]
    private AudioSource musicSource;  // 用于播放BGM的AudioSource

    [Header("音乐资源文件夹")]
    public string musicFolderPath = "Music/";  // Resources下的文件夹路径

    // 单例模式，方便其他场景调用
    public static MusicManager Instance { get; private set; }

    private void Awake()
    {
        // 单例设置
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // 场景切换时不销毁
        }
        else
        {
            Destroy(gameObject);  // 保证只有一个
            return;
        }

        // 获取或添加AudioSource组件
        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        // 设置BGM默认属性
        musicSource.loop = true;  // 音乐默认循环
        musicSource.playOnAwake = false;
    }

    /// <summary>
    /// 播放音乐 - 供其他场景调用的公开方法
    /// </summary>
    /// <param name="musicName">音乐文件名（不含扩展名）</param>
    public void PlayMusic(string musicName)
    {
        if (string.IsNullOrEmpty(musicName))
        {
            Debug.LogWarning("音乐名称为空！");
            return;
        }

        // 如果正在播放同一首音乐，可以不打断（看需求）
        if (musicSource.isPlaying && musicSource.clip != null && musicSource.clip.name == musicName)
        {
            Debug.Log($"已经在播放 {musicName}，无需切换");
            return;
        }

        // 从Resources加载音乐
        string fullPath = musicFolderPath + musicName;
        AudioClip clip = Resources.Load<AudioClip>(fullPath);

        if (clip == null)
        {
            Debug.LogError($"找不到音乐资源：{fullPath}");
            return;
        }

        // 播放新音乐
        musicSource.Stop();  // 先停止当前音乐
        musicSource.clip = clip;
        musicSource.Play();

        Debug.Log($"开始播放音乐：{musicName}");
    }

    /// <summary>
    /// 停止音乐
    /// </summary>
    public void StopMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
            Debug.Log("音乐已停止");
        }
    }

    /// <summary>
    /// 暂停音乐
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    /// <summary>
    /// 继续播放
    /// </summary>
    public void ResumeMusic()
    {
        if (!musicSource.isPlaying && musicSource.clip != null)
        {
            musicSource.Play();
        }
    }

    /// <summary>
    /// 设置音量（0-1）
    /// </summary>
    public void SetVolume(float volume)
    {
        musicSource.volume = Mathf.Clamp01(volume);
    }
}