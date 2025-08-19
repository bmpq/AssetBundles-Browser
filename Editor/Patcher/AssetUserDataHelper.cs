using UnityEditor;
using Newtonsoft.Json;
using System.Collections.Generic;

public static class AssetUserDataHelper
{
    public static void SetData(string assetPath, string key, object value)
    {
        AssetImporter importer = AssetImporter.GetAtPath(assetPath);
        if (importer == null) return;

        var userData = Deserialize(importer.userData);

        userData[key] = value;

        importer.userData = Serialize(userData);
        importer.SaveAndReimport();
    }

    public static T GetData<T>(string assetPath, string key)
    {
        AssetImporter importer = AssetImporter.GetAtPath(assetPath);
        if (importer == null) return default(T);

        var userData = Deserialize(importer.userData);

        if (userData.TryGetValue(key, out object value))
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
            }
            catch
            {
                return default(T);
            }
        }

        return default(T);
    }

    private static Dictionary<string, object> Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new Dictionary<string, object>();
        }

        try
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            return data ?? new Dictionary<string, object>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, object>();
        }
    }

    private static string Serialize(Dictionary<string, object> data)
    {
        return JsonConvert.SerializeObject(data, Formatting.Indented);
    }
}