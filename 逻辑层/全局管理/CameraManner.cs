using Cinemachine;
using System.Collections;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    [SerializeField]
    private CinemachineVirtualCamera[] _allVirtualCameras;

    [Header("Controls for leaping the Y Damping during player jump/fall")]
    [SerializeField] private float _fallPanAmount = 0.25f;
    [SerializeField] private float _fallVPAnTime = 0.35f;
    public float _fallSpeedDampingChangeThreshold = -5f;

    public bool IsLeapingDamping { get; private set; }
    public bool LeppedFromPlayerFalling { get; set; }

    private Coroutine _lerpVPanCoroutine;
    private CinemachineVirtualCamera _currentCamera;
    private CinemachineFramingTransposer _framingTransposer;
    private float normaYpanAmount = 2f;

    private void Awake()
    {
        instance = this;

        for (int i = 0; i < _allVirtualCameras.Length; i++)
        {
            if (_allVirtualCameras[i].enabled)
            {
                _currentCamera = _allVirtualCameras[i];
                _framingTransposer = _currentCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            }
        }
    }

    public void LrepDumping(bool isPlayerFalling)
    {
        if (_lerpVPanCoroutine != null)
        {
            StopCoroutine(_lerpVPanCoroutine);
        }

        _lerpVPanCoroutine = StartCoroutine(LerpAction(isPlayerFalling));
    }

    private IEnumerator LerpAction(bool isPlayerFalling)
    {
        if (_framingTransposer == null)
        {
            yield break;
        }

        IsLeapingDamping = true;
        float startDumpAmount = _framingTransposer.m_YDamping;
        float endDumpAmount = isPlayerFalling ? _fallPanAmount : normaYpanAmount;

        if (isPlayerFalling)
        {
            LeppedFromPlayerFalling = true;
        }

        float elapsedTime = 0f;
        while (elapsedTime < _fallVPAnTime)
        {
            elapsedTime += Time.deltaTime;
            float lerpedPanAmount = Mathf.Lerp(startDumpAmount, endDumpAmount, elapsedTime / _fallVPAnTime);
            _framingTransposer.m_YDamping = lerpedPanAmount;
            yield return null;
        }

        IsLeapingDamping = false;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
