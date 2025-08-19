using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AssetImposterDataDrawer
{
    public const string CanonicalPathIDKey = "imposter.canonicalPathID";
    public const string CanonicalCabIDKey = "imposter.canonicalCabID";

    static AssetImposterDataDrawer()
    {
        Editor.finishedDefaultHeaderGUI += OnDrawHeaderGUI;
    }

    private static void OnDrawHeaderGUI(Editor editor)
    {
        if (editor.targets.Length > 1)
            return;

        Object targetObject = editor.target;
        string assetPath = AssetDatabase.GetAssetPath(targetObject);
        if (string.IsNullOrEmpty(assetPath))
            return;

        AssetImporter importer = AssetImporter.GetAtPath(assetPath);
        if (importer == null)
            return;

        long currentPathID = AssetUserDataHelper.GetData<long>(assetPath, CanonicalPathIDKey);
        string currentCabID = AssetUserDataHelper.GetData<string>(assetPath, CanonicalCabIDKey);
        bool isImposter = currentPathID != default;

        EditorGUI.BeginChangeCheck();

        GUILayout.BeginHorizontal();
        bool newIsImposter = EditorGUILayout.ToggleLeft("Is Imposter", isImposter);
        GUILayout.EndHorizontal();

        long newPathID = currentPathID;
        string newCabID = currentCabID;

        if (newIsImposter)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Replaced with PathID", GUILayout.Width(130));
            newPathID = EditorGUILayout.LongField(currentPathID);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Replaced with CabID", GUILayout.Width(130));
            newCabID = EditorGUILayout.TextField(currentCabID);
            GUILayout.EndHorizontal();
        }

        if (!EditorGUI.EndChangeCheck()) // User did not touch anything
            return;

        bool hasChanged = false;

        // Handle the "Is Imposter" toggle change
        if (newIsImposter != isImposter)
        {
            if (!newIsImposter)
            {
                AssetUserDataHelper.RemoveKey(assetPath, CanonicalPathIDKey);
                AssetUserDataHelper.RemoveKey(assetPath, CanonicalCabIDKey);
                Debug.Log($"Removed Imposter flag from: {assetPath}");
            }
            hasChanged = true;
        }

        if (newIsImposter)
        {
            if (newPathID != currentPathID || (newIsImposter != isImposter && newPathID == default))
            {
                if (newPathID == default)
                {
                    newPathID = -1;
                }
                AssetUserDataHelper.SetData(assetPath, CanonicalPathIDKey, newPathID);
                Debug.Log($"Set Imposter PathID on {assetPath} to: {newPathID}");
                hasChanged = true;
            }

            if (newCabID != currentCabID && newCabID.Length == 32)
            {
                AssetUserDataHelper.SetData(assetPath, CanonicalCabIDKey, newCabID);
                Debug.Log($"Set Imposter CabID on {assetPath} to: {newCabID}");
                hasChanged = true;

                PropagateCabIDToBundle(importer, newCabID);
            }
        }

        if (hasChanged)
        {
            importer.SaveAndReimport();
        }
    }

    private static void PropagateCabIDToBundle(AssetImporter sourceImporter, string newCabID)
    {
        string bundleName = sourceImporter.assetBundleName;
        if (string.IsNullOrEmpty(bundleName))
        {
            return;
        }

        string[] allAssetPathsInBundle = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);

        string dialogTitle = "Propagate CabID?";
        string dialogMessage = $"This will apply the CabID '{newCabID}' to {allAssetPathsInBundle.Length - 1} other asset(s) in the asset bundle '{bundleName}'.\n\nThis operation cannot be undone. Do you want to proceed?";
        if (!EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes, Propagate", "No"))
        {
            return;
        }

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < allAssetPathsInBundle.Length; i++)
            {
                string otherAssetPath = allAssetPathsInBundle[i];

                if (otherAssetPath == sourceImporter.assetPath)
                    continue;

                float progress = (float)i / allAssetPathsInBundle.Length;
                EditorUtility.DisplayProgressBar("Propagating CabID", $"Processing: {otherAssetPath}", progress);

                AssetImporter otherImporter = AssetImporter.GetAtPath(otherAssetPath);
                if (otherImporter != null)
                {
                    long otherPathID = AssetUserDataHelper.GetData<long>(otherAssetPath, CanonicalPathIDKey);
                    if (otherPathID != default)
                    {
                        AssetUserDataHelper.SetData(otherAssetPath, CanonicalCabIDKey, newCabID);
                        otherImporter.SaveAndReimport();
                    }
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }
    }
}