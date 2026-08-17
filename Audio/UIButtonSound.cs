using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 按钮音效 — 挂到任意 Button 上自动给 onClick 添加点击音效。
/// 无需手动绑定事件，拖上即可用。
/// </summary>
public class UIButtonSound : MonoBehaviour
{
    private void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(PlayClick);
        }
    }

    public void PlayClick()
    {
        AudioManager.Instance?.PlayButtonClick();
    }
}
