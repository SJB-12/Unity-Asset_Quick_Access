using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AssetQuickAccess
{
    [System.Serializable]
    public class QuickAccessItem
    {
        public string displayName;
        public Object targetObject;
        public string guid; // Unity Asset Database GUID for tracking asset location
        public bool isLocked;
        public List<UnityEditor.Presets.Preset> presets = new List<UnityEditor.Presets.Preset>();
    }

    [System.Serializable]
    public class QuickAccessContainer
    {
        public string name;
        public bool isExpanded = true;
        public List<QuickAccessItem> items = new List<QuickAccessItem>();
    }

    public class AssetQuickAccessData : ScriptableObject
    {
        public List<QuickAccessContainer> containers = new List<QuickAccessContainer>();
        public int maxPresetsLimit = 3;

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public static AssetQuickAccessData GetOrCreateData()
        {
            // Try to find the asset in the project
            string[] guids = AssetDatabase.FindAssets("t:AssetQuickAccessData");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                AssetQuickAccessData data = AssetDatabase.LoadAssetAtPath<AssetQuickAccessData>(path);
                if (data != null)
                {
                    return data;
                }
            }

            // Create new data file if not found
            AssetQuickAccessData newData = ScriptableObject.CreateInstance<AssetQuickAccessData>();
            string directory = "Assets/AssetQuickAccess/Editor";
            
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(directory))
            {
                if (!AssetDatabase.IsValidFolder("Assets/AssetQuickAccess"))
                {
                    AssetDatabase.CreateFolder("Assets", "AssetQuickAccess");
                }
                AssetDatabase.CreateFolder("Assets/AssetQuickAccess", "Editor");
            }

            string assetPath = $"{directory}/AssetQuickAccessData.asset";
            AssetDatabase.CreateAsset(newData, assetPath);
            AssetDatabase.SaveAssets();
            
            return newData;
        }
    }
}
