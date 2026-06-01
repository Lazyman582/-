using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class paralax : MonoBehaviour
{

    private Transform _mainCamera;

    private Vector3 _lastPosition;

    [SerializeField] private float speed = 1f;

    void Start()
    {
        _mainCamera = Camera.main.transform;
        _lastPosition = _mainCamera.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        ParallaxMove();
    }

    private void ParallaxMove() { 
    
    float deltaX = _mainCamera.position.x - _lastPosition.x;
        transform.position += new Vector3(deltaX * speed, 0, 0);
        _lastPosition =_mainCamera.position;
    
    }


}
