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

    // Camera_Realsenseオブジェクトを格納するための変数
    //private Transform? cameraRealsenseTransform;
    Vector3 realsensePosition;
    Quaternion realsenseRotation;
    Vector3 relativePosition;

    private void Awake()
    {
        try
        {
            var builder = new ControllerBuilder();

            // AUTD3Deviceコンポーネントを持つすべてのGameObjectを検索し、デバイスとして追加
            foreach (var obj in FindObjectsOfType<AUTD3Device>(false).OrderBy(obj => obj.ID))
            {
                builder.AddDevice(new AUTD3(obj.transform.position).WithRotation(obj.transform.rotation));
            }

            _autd = builder.OpenWith(SOEM.Builder()
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

        if (_autd != null) _autd.Send(new AUTD3Sharp.Modulation.Sine(150));
    }

    void Start()
    {
        _pointscalculator = GameObject.Find("PointCloud").GetComponent<PointsCalculator>();
        if (_pointscalculator == null) Debug.Log("Points Calculator is not found");

        // シーン内からCamera_Realsenseオブジェクトを検索し、そのTransformを取得
        GameObject cameraRealsense = GameObject.Find("Camera_Realsense");
        if (cameraRealsense != null)
        {
            realsensePosition = cameraRealsense.transform.position;
            realsenseRotation = cameraRealsense.transform.rotation;
            Debug.Log($"Camera_Realsense Position: {realsensePosition}, Rotation: {realsenseRotation.eulerAngles}");
        }

        
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!_isPlaying)
        {
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        }
#endif
        if (_autd == null) return;
        if (Target == null) return;

        relativePosition = _pointscalculator.Center;
        Target.transform.position = ConvertPosition(relativePosition);

        //Debug.Log($"relativePosition: {_pointscalculator.Center}");

        if (Target.transform.position == _oldPosition) return;
        _autd.Send(new AUTD3Sharp.Gain.Focus(Target.transform.position));
        _oldPosition = Target.transform.position;
    }

    Vector3 ConvertPosition(Vector3 relativePosition)
    {
        Vector3 RealPosition = realsensePosition + (realsenseRotation * relativePosition);
        //Debug.Log($"relativePosition: {_pointscalculator.Center}, RealPosition: {RealPosition}");
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
