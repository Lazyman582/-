using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class hit_object : MonoBehaviour
{
    // Start is called before the first frame update
    
    private Collider2D myCollider;
    public CharacterData characterData;
    // 是否触发过（避免重复执行）
    private bool hasCollision = false;

    private void Awake()
    {

     


        // 获取自身的Collider2D组件
        myCollider = GetComponent<Collider2D>();

        if (myCollider == null)
        {
            Debug.LogError(gameObject.name + " 缺少 Collider2D 组件");
        }

       
    }
    private void Start()
    {
       
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.P)) {

            EventManager.Instance.TriggerDamage(10,transform.position);



        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            EventManager.Instance.TriggerDamage(10,collision.transform.position);
        }
    }
}
