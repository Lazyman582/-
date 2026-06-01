using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class CharacterData : MonoBehaviour
{
    public event Action<float> OnDamaged; // 声明事件
    public event Action<float> OnRecover;
    

    private float  health = 100;
    public float maxHealth;
    public float Health
    {
        get => health;
        set
        {
            health = Mathf.Clamp(value, 0f, maxHealth);
         
        }
    }
    public void TakeDamage(float a)
    {
     
        health -= a;  // 先计算 HP
       
        OnDamaged?.Invoke(a);
    }

    public void Recover(float a) {

        OnRecover?.Invoke(a);


    }

    private void Start()
    {
     
    }


   

}
