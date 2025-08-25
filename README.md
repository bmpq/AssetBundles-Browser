# Unity Asset Bundle Imposter Builder tool
A fork of Unity's AssetBundleBrowser tailored for modding workflows.  

This fork allows you to create assets that reference the original game content without directly embedding those assets. Instead, it uses a system of **"imposter" assets** that point to existing assets in the base game by their internal IDs.

## How It Works

The core concept is **imposter assets**:

1. **Create an imposter asset** in your own Unity project, that acts as a placeholder for the original game asset.
   
2. **Select the imposter asset and mark it as imposter in the inspector**.
   This fork includes a custom inspector that adds `CabID` and `PathID` fields to the asset’s `.meta` file:
   - **CabID**: The ID of the original game bundle containing the target asset.
   - **PathID**: The ID of the asset within that bundle.
   
   You can find these values by analyzing the original game bundles using [UABEA](https://github.com/nesrak1/UABEA/)

3. **Assign your imposter asset to a new bundle**. This new bundle will be marked as an imposter bundle automatically. Your new content bundle should depend on this imposter bundle if it depends on original game assets.

5. **Build your bundles**
   The imposter bundles will be exported alongside your new content bundles, but will be deleted from the build directory when all bundles have finished exporting.
   When loaded by the game, the reference will resolve to the original asset (via CabID + PathID), ensuring proper linkage.

<img width="1375" height="527" alt="Screenshot 2025-08-21 021349" src="https://github.com/user-attachments/assets/839a792a-03b3-4e3f-b625-3182055376db" />

## Requirements
Unity `2019.4+`

## Installation
1. Open UPM in Unity: **Window > Package Manager**
2. Click **"+"** button at the top left
3. Select **"Add package from git URL..."** and paste following URL:
```
https://github.com/bmpq/AssetBundles-Browser-Imposter.git
```

Once installed it will create a new menu item in *Window->AssetBundle Browser*.
