using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class paralax : MonoBehaviour
{

    private Transform _mainCamera;

    private Vector3 _lastPosition;

    [SerializeField] private float speed = 1f;

    // Only start following after the player enters the trigger area.
    // Turned on by BackgroundFollowTrigger.StartFollow().
    private bool _isFollowing = false;

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
        // Keep tracking the camera even before following starts,
        // so there is no sudden jump the moment follow is enabled.
        _lastPosition = _mainCamera.position;

        if (!_isFollowing)
            return;

        transform.position += new Vector3(deltaX * speed, 0, 0);

    }

    // Called by BackgroundFollowTrigger when the player enters its trigger area.
    public void StartFollow()
    {
        _isFollowing = true;
    }


}
