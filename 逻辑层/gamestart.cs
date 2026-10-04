using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gamestart : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        DontDestroyOnLoad(gameObject);

    }
    private void Start()
    {
        SceneManner.Instance.LoadScene("Assets/Scenes/MainUI.unity");


    }
    // Update is called once per frame
    void Update()
    {

    }
}
