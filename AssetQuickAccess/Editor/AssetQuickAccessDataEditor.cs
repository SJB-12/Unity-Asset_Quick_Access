using UnityEditor;
using UnityEngine;

namespace AssetQuickAccess
{
    [CustomEditor(typeof(AssetQuickAccessData))]
    public class AssetQuickAccessDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            AssetQuickAccessData data = (AssetQuickAccessData)target;

            serializedObject.Update();

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Asset Quick Access Config", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            EditorGUI.BeginChangeCheck();
            int newLimit = EditorGUILayout.IntSlider("Max Presets Per Bookmark", data.maxPresetsLimit, 1, 10);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(data, "Change Max Presets Limit");
                data.maxPresetsLimit = Mathf.Clamp(newLimit, 1, 10);
                data.Save();
            }

            GUILayout.Space(10);
            EditorGUILayout.HelpBox("This data asset stores all your bookmarked containers and items. Click the gear icon on the toolbar of the Quick Access window to view this configuration.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
