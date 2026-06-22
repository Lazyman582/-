using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFollowObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _playerTransform;

    [Header("Flip Rotation Stats")]
    [SerializeField] private float _flipYRotationTime = 0.5f;

    [SerializeField] private bool _isFacingRight;

    private Coroutine _turnCoroutine;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindPlayer();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        if (_playerTransform != null)
        {
            transform.position = _playerTransform.position;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindPlayer();
    }

    private void BindPlayer()
    {
        if (PersistentPlayer.Instance != null)
        {
            _playerTransform = PersistentPlayer.Instance.PlayerTransform;
        }
        else
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            _playerTransform = player != null ? player.transform : null;
        }

        if (_playerTransform != null)
        {
            transform.position = _playerTransform.position;
        }
    }

    public void StartFlipRotation(bool newFacingRight)
    {
        _isFacingRight = newFacingRight;

        if (_turnCoroutine != null)
        {
            StopCoroutine(_turnCoroutine);
        }
        _turnCoroutine = StartCoroutine(SmoothFlipRotation());
    }

    private IEnumerator SmoothFlipRotation()
    {
        float startRotation = transform.localEulerAngles.y;
        float endRotationAmount = DetermineEndRotation();
        float elapsedTime = 0f;

        while (elapsedTime < _flipYRotationTime)
        {
            elapsedTime += Time.deltaTime;
            float yRotation = Mathf.Lerp(startRotation, endRotationAmount, elapsedTime / _flipYRotationTime);
            transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0f, endRotationAmount, 0f);
    }

    private float DetermineEndRotation()
    {
        bool facingRight = !_isFacingRight;

        if (!facingRight)
        {
            return 180f;
        }

        return 0f;
    }
}
