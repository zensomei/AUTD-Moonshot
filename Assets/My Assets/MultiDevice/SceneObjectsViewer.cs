using UnityEngine;
using UnityEditor; // �G�f�B�^�[�g���ɕK�v

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
            // Quaternion����Euler�p�֕ϊ����ēx���ŕ\��
            Vector3 rotationInEuler = obj.transform.rotation.eulerAngles;

            // �ʒu�Ɖ�]�������_4���܂ŕ\��
            string positionFormatted = string.Format("({0:F4}, {1:F4}, {2:F4})", obj.transform.position.x, obj.transform.position.y, obj.transform.position.z);
            //string rotationFormatted = string.Format("({0:F4}, {1:F4}, {2:F4})", rotationInEuler.x, rotationInEuler.y, rotationInEuler.z);

            Debug.Log($"Object Name: {obj.name}, Position: {positionFormatted}, Rotation: {rotationInEuler}");
        }
    }
}
