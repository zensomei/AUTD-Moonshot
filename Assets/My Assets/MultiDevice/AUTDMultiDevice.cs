using System;
using System.Linq;
using AUTD3Sharp;
using AUTD3Sharp.Link;
using AUTD3Sharp.NativeMethods;
using Intel.RealSense;
using UnityEngine;

#if UNITY_2020_2_OR_NEWER
#nullable enable
#endif

public class AUTDMultiDevice : MonoBehaviour
{
    private Controller<AUTD3Sharp.Link.SOEM>? _autd = null;
    public GameObject? Target = null;

    private Vector3 _oldPosition;

    private static bool _isPlaying = true;


    ////Self
    private PointsCalculator _pointscalculator;

    // Camera_Realsense�I�u�W�F�N�g���i�[���邽�߂̕ϐ�
    //private Transform? cameraRealsenseTransform;
    Vector3 realsensePosition;
    Quaternion realsenseRotation;
    Vector3 relativePosition;


    private async void Awake()
    {
        try
        {
            var builder = new ControllerBuilder();

            // AUTD3Device�R���|�[�l���g�������ׂĂ�GameObject���������A�f�o�C�X�Ƃ��Ēǉ�
            foreach (var obj in FindObjectsOfType<AUTD3Device>(false).OrderBy(obj => obj.ID))
            {
                builder.AddDevice(new AUTD3(obj.transform.position).WithRotation(obj.transform.rotation));
            }

            _autd = await builder.OpenWithAsync(SOEM.Builder()
                        .WithErrHandler((slave, status, msg) =>
                        {
                            switch (status)
                            {
                                case Status.Error:
                                    Debug.LogError($"Error [{slave}]: {msg}");
                                    break;
                                case Status.Lost:
                                    Debug.LogError($"Lost [{slave}]: {msg}");
#if UNITY_EDITOR
                                    UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
                                UnityEngine.Application.Quit();
#endif
                                    break;
                                case Status.StateChanged:
                                    Debug.LogError($"StateChanged [{slave}]: {msg}");
                                    break;
                            }
                        }));
        }
        catch (Exception)
        {
            Debug.LogError("Failed to open AUTD3 controller!");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
        UnityEngine.Application.Quit();
#endif
        }

        if (_autd != null && Target != null)
        {
            await _autd.SendAsync(new AUTD3Sharp.Modulation.Sine(150)); // 150 Hz
            await _autd.SendAsync(new AUTD3Sharp.Gain.Focus(Target.transform.position));
            _oldPosition = Target.transform.position;
        }
    }
    void Start()
    {
        _pointscalculator = GameObject.Find("PointCloud").GetComponent<PointsCalculator>();

        // �V�[��������Camera_Realsense�I�u�W�F�N�g���������A����Transform���擾
        GameObject cameraRealsense = GameObject.Find("Camera_Realsense");
        if (cameraRealsense != null)
        {
            realsensePosition = cameraRealsense.transform.position;
            realsenseRotation = cameraRealsense.transform.rotation;
            Debug.Log($"Camera_Realsense Position: {realsensePosition}, Rotation: {realsenseRotation.eulerAngles}");
        }
    }

    private async void LateUpdate()
    {
#if UNITY_EDITOR
        if (!_isPlaying)
        {
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        }
#endif
        if (_autd == null) return;

        //relativePosition = _pointscalculator.Center;
        if (Target != null) Target.transform.position = ConvertPosition(relativePosition);

        //if (Target != null) Target.transform.position = realsensePosition + (realsenseRotation * _pointscalculator.Center);
        //Debug.Log($"relativePosition: {_pointscalculator.Center}");

        if (Target == null || Target.transform.position == _oldPosition) return;
        await _autd.SendAsync(new AUTD3Sharp.Gain.Focus(Target.transform.position));
        _oldPosition = Target.transform.position;
    }

    private void FixedUpdate()
    {
        relativePosition = _pointscalculator.Center;
        //Debug.Log($"relativePosition: {_pointscalculator.Center}");
    }

    Vector3 ConvertPosition(Vector3 relativePosition)
    {
        Vector3 RealPosition = realsensePosition + (realsenseRotation * relativePosition);
        //Debug.Log($"relativePosition: {relativePosition}, RealPosition: {RealPosition}");
        return RealPosition;
    }

    private void OnApplicationQuit()
    {
        _autd?.Dispose();
    }
}

#if UNITY_2020_2_OR_NEWER
#nullable restore
#endif
