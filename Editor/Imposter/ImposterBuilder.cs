using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using AssetBundleBrowser.AssetBundleDataSource;
using System.Collections.Generic;
using BuildCompression = UnityEngine.BuildCompression;
using System.IO;


namespace AssetBundleBrowser.Imposter
{
    public static class ImposterBuilder
    {
        public const string CanonicalPathIDKey = "imposter.canonicalPathID";
        public const string CanonicalCabIDKey = "imposter.canonicalCabID";

        public static bool BuildAssetBundles(ABBuildInfo info, ABDataSource dataSource)
        {
            var customIdGenerator = new ImposterIdentifierGenerator();

            var allBundleNames = dataSource.GetAllAssetBundleNames();
            var assetBundleBuilds = new List<AssetBundleBuild>();
            foreach (var bundleName in allBundleNames)
            {
                var assetPaths = dataSource.GetAssetPathsFromAssetBundle(bundleName);
                assetBundleBuilds.Add(new AssetBundleBuild
                {
                    assetBundleName = bundleName,
                    assetNames = assetPaths
                });

                if (assetPaths.Length > 0)
                {
                    string customCabId = AssetUserDataHelper.GetData<string>(assetPaths[0], CanonicalCabIDKey);
                    if (!string.IsNullOrEmpty(customCabId))
                    {
                        customIdGenerator.CustomCabIdMap[bundleName] = customCabId;
                        Debug.Log($"Mapping bundle '{bundleName}' to custom CAB ID: '{customCabId}'");
                    }
                }
            }

            if (assetBundleBuilds.Count == 0)
            {
                Debug.LogWarning("No AssetBundles configured for build.");
                return true;
            }

            var content = new BundleBuildContent(assetBundleBuilds);

            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(info.buildTarget);
            var buildParams = new BundleBuildParameters(info.buildTarget, buildTargetGroup, info.outputDirectory);

            // Translate legacy BuildAssetBundleOptions to SBP parameters
            if ((info.options & BuildAssetBundleOptions.ForceRebuildAssetBundle) != 0)
                buildParams.UseCache = false;
            if ((info.options & BuildAssetBundleOptions.AppendHashToAssetBundleName) != 0)
                buildParams.AppendHash = true;
            if ((info.options & BuildAssetBundleOptions.DisableWriteTypeTree) != 0)
                buildParams.ContentBuildFlags |= ContentBuildFlags.DisableWriteTypeTree;
            if ((info.options & BuildAssetBundleOptions.ChunkBasedCompression) != 0)
                buildParams.BundleCompression = BuildCompression.LZ4;
            else if ((info.options & BuildAssetBundleOptions.UncompressedAssetBundle) != 0)
                buildParams.BundleCompression = BuildCompression.Uncompressed;
            else
                buildParams.BundleCompression = BuildCompression.LZMA;

            IBundleBuildResults results;
            var taskList = DefaultBuildTasks.Create(DefaultBuildTasks.Preset.AssetBundleCompatible);

            ReturnCode exitCode = ContentPipeline.BuildAssetBundles(
                buildParams,
                content,
                out results,
                taskList,
                customIdGenerator
            );

            if (exitCode == ReturnCode.Success)
            {
                Debug.Log("AssetBundle build successful with custom PathIDs.");

                foreach (var kvp in results.BundleInfos)
                {
                    string fullAbsPath = ResolveBuildPath(kvp.Value.FileName);
                    if (IsImposterBundle(kvp.Key))
                    {
                        if (File.Exists(fullAbsPath))
                        {
                            File.Delete(fullAbsPath);
                            Debug.Log($"Deleted temporary imposter bundle build: {fullAbsPath}");
                        }
                    }

                    info.onBuild?.Invoke(kvp.Key);
                }
                return true;
            }

            Debug.LogError($"AssetBundle build failed with ReturnCode: {exitCode}");
            return false;
        }

        public static bool IsImposterBundle(string bundleName)
        {
            var assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            foreach (var assetPath in assetPaths)
            {
                if (AssetUserDataHelper.GetData<long>(assetPath, CanonicalPathIDKey) != default)
                {
                    return true;
                }
            }
            return false;
        }

        public static string ResolveBuildPath(string inputPath)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                return GetProjectRootPath();
            }

            if (Path.IsPathRooted(inputPath))
            {
                return Path.GetFullPath(inputPath);
            }
            else
            {
                string projectRoot = GetProjectRootPath();
                string combinedPath = Path.Combine(projectRoot, inputPath);

                return Path.GetFullPath(combinedPath);
            }
        }

        public static string GetProjectRootPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }
    }
}