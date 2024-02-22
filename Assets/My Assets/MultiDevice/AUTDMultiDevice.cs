using System;
using System.Linq;
using AUTD3Sharp;
using AUTD3Sharp.Link;
using AUTD3Sharp.NativeMethods;
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

    private async void Update()
    {
#if UNITY_EDITOR
        if (!_isPlaying)
        {
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        }
#endif
        if (_autd == null) return;

        if (Target == null || Target.transform.position == _oldPosition) return;
        await _autd.SendAsync(new AUTD3Sharp.Gain.Focus(Target.transform.position));
        _oldPosition = Target.transform.position;
    }

    private void OnApplicationQuit()
    {
        _autd?.Dispose();
    }
}

#if UNITY_2020_2_OR_NEWER
#nullable restore
#endif
