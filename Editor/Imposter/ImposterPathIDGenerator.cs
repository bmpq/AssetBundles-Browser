using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;

namespace AssetBundleBrowser
{
    public class ImposterPathIDGenerator : IDeterministicIdentifiers
    {
        private readonly IDeterministicIdentifiers m_FallbackGenerator;

        public ImposterPathIDGenerator()
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

        public string GenerateInternalFileName(string name)
        {
            return m_FallbackGenerator.GenerateInternalFileName(name);
        }
    }
}