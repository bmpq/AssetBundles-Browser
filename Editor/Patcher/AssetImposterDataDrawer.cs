using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AssetImposterDataDrawer
{
    public const string CanonicalPathIDKey = "imposter.canonicalPathID";

    static AssetImposterDataDrawer()
    {
        Editor.finishedDefaultHeaderGUI += OnDrawHeaderGUI;
    }

    private static void OnDrawHeaderGUI(Editor editor)
    {
        Object targetObject = editor.target;
        string assetPath = AssetDatabase.GetAssetPath(targetObject);
        if (string.IsNullOrEmpty(assetPath))
            return;

        AssetImporter importer = AssetImporter.GetAtPath(assetPath);
        if (importer == null)
            return;

        long currentPathID = AssetUserDataHelper.GetData<long>(assetPath, CanonicalPathIDKey);
        bool isImposter = currentPathID != default;

        EditorGUI.BeginChangeCheck();

        GUILayout.BeginHorizontal();
        bool newIsImposter = EditorGUILayout.ToggleLeft("Is Imposter", isImposter);
        GUILayout.EndHorizontal();

        long newPathID = currentPathID;
        if (newIsImposter)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Replaced with PathID", GUILayout.Width(130));
            newPathID = EditorGUILayout.LongField(currentPathID);
            GUILayout.EndHorizontal();
        }

        if (EditorGUI.EndChangeCheck()) // user touched something
        {
            if (!newIsImposter)
            {
                AssetUserDataHelper.RemoveKey(assetPath, CanonicalPathIDKey);
                Debug.Log($"Removed Imposter flag from: {assetPath}");
            }
            else if (newPathID != currentPathID || newIsImposter != isImposter)
            {
                if (newPathID == default)
                {
                    newPathID = -1;
                }
                AssetUserDataHelper.SetData(assetPath, CanonicalPathIDKey, newPathID);
                Debug.Log($"Set Imposter GUID on {assetPath} to: {newPathID}");
            }
        }
    }
}