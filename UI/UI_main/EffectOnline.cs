using UnityEngine;
using DG.Tweening;

public class EffectOnLine : MonoBehaviour
{
    public SimpleTextReveal reveal;
    public Transform shakeTarget;   // 要震动的物体

    void Start()
    {
        reveal.onLineComplete.AddListener(OnLineDone);
        reveal.Play();
    }

    void OnLineDone(int index, string text)
    {
      
    }
}