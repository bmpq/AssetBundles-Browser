using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;

namespace AssetBundleBrowser
{
    public class ImposterIdentifierGenerator : IDeterministicIdentifiers
    {
        private readonly IDeterministicIdentifiers m_FallbackGenerator;
        public Dictionary<string, string> CustomCabIdMap { get; } = new Dictionary<string, string>();

        public ImposterIdentifierGenerator()
        {
            m_FallbackGenerator = new PrefabPackedIdentifiers();
        }

        public long SerializationIndexFromObjectIdentifier(ObjectIdentifier objectID)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(objectID.guid);

            // custom data stored in an asset's .meta file at 'userData'
            long canonicalPathID = AssetUserDataHelper.GetData<long>(assetPath, Imposter.ImposterBuilder.CanonicalPathIDKey);
            if (canonicalPathID != default)
            {
                return canonicalPathID;
            }

            return m_FallbackGenerator.SerializationIndexFromObjectIdentifier(objectID);
        }

        public string GenerateInternalFileName(string bundleName)
        {
            if (CustomCabIdMap.TryGetValue(bundleName, out string customCabId))
            {
                return customCabId;
            }

            return m_FallbackGenerator.GenerateInternalFileName(bundleName);
        }
    }
}