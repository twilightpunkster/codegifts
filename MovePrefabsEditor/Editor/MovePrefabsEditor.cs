using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Linq;
using System.IO;

public class MovePrefabsEditor : EditorWindow
{
    string exportSubFolder = "MovedPrefabs";
    readonly string exportSubFolderDefault = "MovedPrefabs";

    [MenuItem("Window/Prefab Mover")]
    public static void ShowWindow()
    {
        MovePrefabsEditor window = (MovePrefabsEditor)EditorWindow.GetWindow(typeof(MovePrefabsEditor));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Export Settings", EditorStyles.boldLabel);
        exportSubFolder = EditorGUILayout.TextField("Output Path", exportSubFolder);
        if (GUI.Button(new Rect(10, 50, 100, 30), "Move Selection"))
        {
            SavePrefabs();
        }
    }

    void SavePrefabs()
    {
        var unfilteredSceneObjects = Selection.objects; 

        List<Object> foundPrefabs = new List<Object>();
        for (int i = 0; i < unfilteredSceneObjects.Length; i++)
        {
            var prefab = unfilteredSceneObjects[i];
            PrefabType prefType = PrefabUtility.GetPrefabType(prefab);
            if (prefType != PrefabType.None)
            {
                if (prefab != null)
                {
                    foundPrefabs.Add(prefab);
                }
            }
        }

        if (foundPrefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("Error",
                "There was no prefab asset in your selection.",
                "Ok");
            return;
        }
        
        if (string.IsNullOrEmpty(exportSubFolder))
        {
            exportSubFolder = exportSubFolderDefault;
        }
        string sanitizedSubFolder = SanitizePath_(exportSubFolder, '-');       
        if (EditorUtility.DisplayDialog("Are you sure?",
                        string.Format("Do you want to export the prefabs {0} with all dependencies to path {1}?", string.Join(", ", foundPrefabs.Select(x => x.name).ToArray()), exportSubFolder),
                        "Yes",
                        "No"))
        {
            MovePrefabs(foundPrefabs, sanitizedSubFolder);
        }
      
    }

    private void MovePrefabs(List<Object> foundPrefabs, string newFolderName)
    {
        newFolderName = newFolderName.TrimStart('/');
        string newFolderPath = Path.Combine("Assets/", newFolderName);
        List<Object> finalObjects = new List<Object>();
        Object[] roots = foundPrefabs.ToArray();

        finalObjects.AddRange(EditorUtility.CollectDependencies(roots));

        var allHierarchyObjects = EditorUtility.CollectDeepHierarchy(roots);

        foreach (var rootObj in roots)
        {
            AddIfNotInList(finalObjects, rootObj);
        }
        foreach (var item in allHierarchyObjects)
        {
            AddIfNotInList(finalObjects, item);
        }
        var childDependencies = EditorUtility.CollectDependencies(allHierarchyObjects);
        foreach (var item in childDependencies)
        {
            AddIfNotInList(finalObjects, item);
        }
        List<string> assetPaths = new List<string>(finalObjects.Count);
        string[] metaFilePaths = new string[finalObjects.Count];
        for (int i = 0; i < finalObjects.Count; i++)
        {
            string assetPath = AssetDatabase.GetAssetPath(finalObjects[i]);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AddIfNotInList(assetPaths, assetPath);
                var dependencies = AssetDatabase.GetDependencies(assetPath);
                foreach (string dep in dependencies)
                {
                    if (!string.IsNullOrEmpty(dep))
                    {
                        AddIfNotInList(assetPaths, dep);
                    } 
                }
            }
        }

        string[] moveSuccesses = new string[assetPaths.Count];
        string assetFolderPath = Application.dataPath;
        int startCharsToRemove = "Assets".Length;
        for (int i = 0; i < assetPaths.Count; i++)
        {
            string relativePath = assetPaths[i].Substring(startCharsToRemove);
            if (!relativePath.StartsWith("/"))
            {
                relativePath = "/" + relativePath;
            }
            int lastSlashIndex = relativePath.LastIndexOf('/');
            string relativeFolder = relativePath.Substring(0, lastSlashIndex);
            //Path.Combine - "If path2 contains an absolute path, this method returns path2." If the second element starts with a "/", it is treated as absolute path. 
            string newFullPath = Path.Combine(assetFolderPath, newFolderName) + relativeFolder;
            string newPath = newFolderPath + relativePath;
            string newFolder = newFolderPath + relativeFolder;
            CreateAssetFolder(newFolder);
            moveSuccesses[i] = AssetDatabase.MoveAsset(assetPaths[i], newPath); //returns An empty string if the asset has been successfully moved, otherwise an error message.
        }

        List<string> errorMessagesFinal = new List<string>();
        for (int i = 0; i < moveSuccesses.Length; i++)
        {
            if (!string.IsNullOrEmpty(moveSuccesses[i]))
            {
                errorMessagesFinal.Add(string.Format("Path: {0}, Error: ", assetPaths[i], moveSuccesses[i]));
            }
        }

        if (errorMessagesFinal.Count == 0)
        {
            EditorUtility.DisplayDialog("Success",
                        "Operation completed successfully",
                        "Yes");
        }
        else
        {
            EditorUtility.DisplayDialog("Completed with Problems",
                        string.Format("Following problems occured: '{0}'", string.Join("',    '", errorMessagesFinal.ToArray())),
                        "Ok");
        }
    }

    /// <summary>
    /// Will create a new folder in the project, including meta files. creating folders with the AssetDatabase is necessary to be able to move other assets into this folders
    /// </summary>
    /// <param name="path"></param>
    private void CreateAssetFolder(string path)
    {
        path = path.Replace("\\", "/");
        string[] pathParts = path.Split('/');
        for (int i = 1; i < pathParts.Length; i++)
        {
            string parentFolder = string.Join("/", pathParts.Take(i).ToArray());
            string newFolder = string.Join("/", pathParts.Take(i + 1).ToArray());
            if (!AssetDatabase.IsValidFolder(newFolder))
            {
                AssetDatabase.CreateFolder(parentFolder, pathParts[i]);
            }
        }
    }

    private void AddIfNotInList<T>(List<T> list, T objectToAdd)
    {
        if (!list.Contains(objectToAdd))
        {
            list.Add(objectToAdd);
        }
    }

    public static string SanitizePath_(string path, char replaceChar)
    {
        string dir = Path.GetDirectoryName(path);
        var invalids = System.IO.Path.GetInvalidFileNameChars();
        foreach (char c in invalids)
            dir = dir.Replace(c, replaceChar);

        string name = Path.GetFileName(path);
        foreach (char c in invalids)
            name = name.Replace(c, replaceChar);

        return dir + name;
    }

}
