using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// This Postprocessor will look for all models with the file endings "fbx", "blend" or "dae" 
/// that are either inside a "Actors" or a "Props" folder and will apply certain import settings to them. 
/// By creating a ModelImporterSettingSO scriptable object and set its "isActiveSetting" field to true, the settings of the SO will be applied to the model import settings
/// 
/// written by Simon Eicher (fleshmobproductions@gmail.com, simon41@gmx.at), 2018
/// </summary>
public class ActorModelPostProcessor : AssetPostprocessor {

    private readonly string[] fileTypes = new string[] { "fbx", "blend", "dae" };
    private readonly string folderNameActors = "Actors";
    private readonly string folderNameProps = "Props";
    private readonly System.StringComparison folderNameComparisonSetting = System.StringComparison.InvariantCulture;

    private bool importMaterials = false;
    private bool generateColliders = false;
    private bool generateLightmapUVs = false;

    private bool stopDirectorySearchAtAssetsFolder = true;

    // Use this for initialization
    void OnPreprocessModel()
    {
        ModelImporter importer = assetImporter as ModelImporter;
        if (importer == null)
        {
            return;
        }
        string modelPath = assetPath;
        if (!HasAssetAppropriateType(modelPath))
        {
            return;
        }

        var directories = GetAllParentDirectories(modelPath);
        bool isProp = false;
        bool isActor = false;
        for (int i = 0; i < directories.Count; i++)
        {
            if (string.Equals(directories[i], folderNameActors, folderNameComparisonSetting)){
                isActor = true;
            }
            if (string.Equals(directories[i], folderNameProps, folderNameComparisonSetting))
            {
                isProp = true;
            }
        }
        if (isProp || isActor)
        {
            ApplyImporterSOSettings();

            ProcessModelDefault(importer);
            if (isProp)
            {
                ProcessPropSpecific(importer);
            } else
            {
                ProcessActorSpecific(importer);
            }
        }
    }

    private List<string> GetAllParentDirectories(string path)
    {
        List<string> directories = new List<string>();
        DirectoryInfo di = new DirectoryInfo(path);
        if (di != null)
        {
            GetAllParentDirectoriesRec(di.Parent, ref directories); //pass parent since the first directory info name will always be the model file name itself
        }
        return directories;
    }

    private void GetAllParentDirectoriesRec(DirectoryInfo prevInfo, ref List<string> directories)
    {
        if (prevInfo == null)
        {
            return;
        }
        directories.Add(prevInfo.Name);
        if (stopDirectorySearchAtAssetsFolder && string.Equals(prevInfo.Name, "Assets"))
        {
            return;
        }
        GetAllParentDirectoriesRec(prevInfo.Parent, ref directories);
    }

    //check for updated settings for every import
    private void ApplyImporterSOSettings()
    {
        var settingsList = ImporterExtensions.FindAssetsByType<ModelImporterSettingSO>();
        if (settingsList != null)
        {
            for (int i = 0; i < settingsList.Count; i++)
            {
                var setting = settingsList[i];
                if (setting.isActiveSetting)
                {
                    importMaterials = setting.ImportMaterials;
                    generateColliders = setting.GenerateColliders;
                    generateLightmapUVs = setting.GenerateLightmapUVs;
                    break;
                }
            }
        }
    }

    private bool HasAssetAppropriateType(string modelPath)
    {
        string lowerCasePath = modelPath.ToLower();
        for (int i = 0; i < fileTypes.Length; i++)
        {
            if (lowerCasePath.EndsWith(fileTypes[i]))
            {
                return true;
            }
        }
        return false;
    }

    private void ProcessModelDefault(ModelImporter importer)
    {
        importer.useFileScale = true;
        importer.importLights = false;
        importer.importCameras = false;
        importer.importMaterials = importMaterials;
        importer.generateSecondaryUV = generateLightmapUVs;
        importer.addCollider = generateColliders;
    }

    private void ProcessActorSpecific(ModelImporter importer)
    {
        importer.importBlendShapes = true;
        importer.importAnimation = true;
    }

    private void ProcessPropSpecific(ModelImporter importer)
    {
        importer.importBlendShapes = false;
        importer.importAnimation = false;
    }

}
