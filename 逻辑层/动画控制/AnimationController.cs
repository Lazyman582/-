using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public static AnimationController Instance { get; private set; }

    public Animator animator;
    void Start()
    {
        animator = AllWay.FindComponent<Animator>(out string errorMsg);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
