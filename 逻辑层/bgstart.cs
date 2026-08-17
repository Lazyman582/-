using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bgstart : MonoBehaviour
{
    public AudioClip bgClip;
    void Start()
    {
        AudioManager.Instance.PlayBGM(bgClip);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
