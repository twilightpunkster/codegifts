using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "ModelImporterSetting", menuName = "Importer/ModelImporterSettingSO")]
public class ModelImporterSettingSO : ScriptableObject {

    [SerializeField]
    public bool isActiveSetting = false;
    [SerializeField]
    protected bool importMaterials = false;
    [SerializeField]
    protected bool generateColliders = false;
    [SerializeField]
    protected bool generateLightmapUVs = false;

    public bool ImportMaterials { get { return importMaterials; } }
    public bool GenerateColliders { get { return generateColliders; } }
    public bool GenerateLightmapUVs { get { return generateLightmapUVs; } }

    //will get called every time the asset changes
    //if one assets gets set to active, disable others, be careful to not run into an endless loop here
    private void OnValidate()
    {
        if (isActiveSetting)
        {
            List<ModelImporterSettingSO> allSettings = ImporterExtensions.FindAssetsByType<ModelImporterSettingSO>();
            for (int i = 0; i < allSettings.Count; i++)
            {
                if (allSettings[i] != this)
                {
                    allSettings[i].isActiveSetting = false;
                }
            }
        }
    }
}
