using UnityEngine;
using UnityEditor; // エディター拡張に必要

public class SceneObjectsViewer : EditorWindow
{
    [MenuItem("Tools/Show Scene Objects Info")]
    public static void ShowWindow()
    {
        GetWindow<SceneObjectsViewer>("Scene Objects Viewer");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Show Scene Objects Info"))
        {
            ShowSceneObjectsInfo();
        }
    }

    private static void ShowSceneObjectsInfo()
    {
        foreach (GameObject obj in Object.FindObjectsOfType<GameObject>())
        {
            // QuaternionからEuler角へ変換して度数で表示
            Vector3 rotationInEuler = obj.transform.rotation.eulerAngles;

            // 位置と回転を小数点4桁まで表示
            string positionFormatted = string.Format("({0:F4}, {1:F4}, {2:F4})", obj.transform.position.x, obj.transform.position.y, obj.transform.position.z);
            //string rotationFormatted = string.Format("({0:F4}, {1:F4}, {2:F4})", rotationInEuler.x, rotationInEuler.y, rotationInEuler.z);

            Debug.Log($"Object Name: {obj.name}, Position: {positionFormatted}, Rotation: {rotationInEuler}");
        }
    }
}
