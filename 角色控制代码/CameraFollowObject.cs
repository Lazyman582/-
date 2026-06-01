using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _playerTransform;

    [Header("Flip Rotation Stats")]
    [SerializeField] private float _flipYRotationTime = 0.5f;

    [SerializeField] private bool _isFacingRight; // 玩家当前朝向

    private Coroutine _turnCoroutine;

    private void Update()
    {
        // 让 cameraFollowObject 跟随玩家的位置
        if (_playerTransform != null)
        {
            transform.position = _playerTransform.position;
        }
    }

    /// <summary>
    /// 开始平滑旋转（在玩家朝向改变时调用）
    /// </summary>
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

            // 平滑插值旋转
            float yRotation = Mathf.Lerp(startRotation, endRotationAmount, elapsedTime / _flipYRotationTime);
            transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

            yield return null;
        }

        // 确保最终旋转精确
        transform.rotation = Quaternion.Euler(0f, endRotationAmount, 0f);
    }

    private float DetermineEndRotation()
    {
        // 注意：这里根据图片中的逻辑，先取反再判断
        bool facingRight = !_isFacingRight;

        if (!facingRight)
        {
            return 180f;
        }

        return 0f; // 面向右侧时返回0度
    }
}