using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 12f; // »ðÒ»°ã 10~15 Ö¡/Ãë

    Image img;
    float timer;
    int i;

    void Awake() => img = GetComponent<Image>();

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        timer += Time.deltaTime;
        if (timer >= 1f / fps)
        {
            timer = 0;
            i = (i + 1) % frames.Length;
            img.sprite = frames[i];
        }
    }
}