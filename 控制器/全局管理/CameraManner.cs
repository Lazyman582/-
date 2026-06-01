using Cinemachine;
using System.Collections;
using System.Collections.Generic;
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

    public bool IsLeapingDamping { get; private set; } = false;
    public bool LeppedFromPlayerFalling { get;  set; } = false;
    private Coroutine _lerpVPanCoroutine;
    private CinemachineVirtualCamera _currentCamera;
    private CinemachineFramingTransposer _framingTransposer;

    private float  normaYpanAmount = 2f;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        for (int i =0;i<_allVirtualCameras.Length;i++) {

            if (_allVirtualCameras[i].enabled) { 
            
            _currentCamera = _allVirtualCameras[i];
            
            _framingTransposer = _currentCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            
            }
        
        
        
        }


    }

    #region Lrep the Y Dumping
    public void LrepDumping (bool isPlayerFalling)
    {
        _lerpVPanCoroutine = StartCoroutine(LerpAction(isPlayerFalling));
    }

    private IEnumerator LerpAction(bool isPlayerFalling)
    {
        IsLeapingDamping = true;

        // Grab the starting dumping amount
        float startDumpAmount = _framingTransposer.m_YDamping;
        float endDumpAmount = 0f;

        // Determine the end dumping amount
        if (isPlayerFalling)
        {
            endDumpAmount = _fallPanAmount;
            LeppedFromPlayerFalling = true;
        }
        else
        {
           
            endDumpAmount = normaYpanAmount;
           
        }
        float elapsedTime = 0f;
        while (elapsedTime < _fallVPAnTime)
        {
            elapsedTime += Time.deltaTime;

            float LeredPancAmount = Mathf.Lerp(startDumpAmount, endDumpAmount, (elapsedTime / _fallVPAnTime));
            _framingTransposer.m_YDamping = LeredPancAmount;

            yield return null;
        }
        IsLeapingDamping = false;
    }
    #endregion

}


