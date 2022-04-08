#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using System.Linq;
using System.IO;

public class ProjectImpulseMapEditor : EditorWindow {
    string mapName = "";
    bool customExportPath = false;
    string exportPath = "";
    string scenePath = "";
    private static AddressableAssetSettings settings;

    [MenuItem("Project Impulse/Map Exporter")]
    public static void ShowMapWindow() {
        GetWindow<ProjectImpulseMapEditor>("Map Exporter");
    }

    private void OnGUI() {
        GUILayout.Label("Project Impulse Map Exporter", EditorStyles.largeLabel);
        mapName = EditorGUILayout.TextField("Map Name: ", mapName);
        scenePath = EditorGUILayout.TextField("Scene Path: ", scenePath);

        GUILayout.Label("Export Path: " + exportPath);
        customExportPath = EditorGUILayout.Toggle("Use Custom Export Path", customExportPath);
        if (customExportPath)
            exportPath = EditorGUILayout.TextField("Export Path: ", exportPath);
        else
            exportPath = UnityEngine.Application.dataPath + "/Export/" + mapName;

        if (GUILayout.Button("Export Map"))
            ExportMap();
    }

    private void ExportMap() {
        AddScene(scenePath);
        bool success = BuildAddressable();
        if(!success)
            return;

        DeleteFolder(exportPath);
        CreateFolder(exportPath);

        CreateConfig();
        
        var info = new DirectoryInfo(UnityEngine.Application.dataPath + "/Export");
        var fileInfo = info.GetFiles();
        foreach (var file in fileInfo)
            File.Move(file.FullName, exportPath + "/" + file.Name);

        AssetDatabase.Refresh();
    }

    private bool BuildAddressable() {
        AddressableAssetSettingsDefaultObject.Settings.profileSettings.SetValue(AddressableAssetSettingsDefaultObject.Settings.activeProfileId, "Local.LoadPath", "{UnityEngine.Application.dataPath}/Maps/" + mapName);
        AddressableAssetSettings.CleanPlayerContent();
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        bool success = string.IsNullOrEmpty(result.Error);
        return success;
    }

    private void CreateConfig() {
        string configContent = "\n//WARNING EDITING THIS FILE MAY RESULT IN ERRORS\n\n// name is the name of the map \nname=" + mapName + "\n// gamemodes is a list of gamemodes that this map is configured for\ngamemodes=Team Death Match,Free For All,Elimination\n";
        File.WriteAllText(UnityEngine.Application.dataPath + "/Export/" + mapName + "Config.cfg", configContent);
    }

    private void DeleteFolder(string path) {
        if (!Directory.Exists(path))
            return;
        string[] files = Directory.GetFiles(path);
        foreach(string file in files)
            File.Delete(file);

        Directory.Delete(path);
    }

    private void CreateFolder(string path) {
        Directory.CreateDirectory(path);
    }

    public void AddScene(string path) {
        var settings = AddressableAssetSettingsDefaultObject.Settings;

        if (!settings)
            return;

        var group = settings.FindGroup("Default Local Group");

        //group.GetSchema<BundledAssetGroupSchema>().LoadPath.SetVariableByName(AddressableAssetSettingsDefaultObject.Settings, "{UnityEngine.Application.dataPath}/Maps/" + mapName);
        var guid = AssetDatabase.AssetPathToGUID(path);
        if (group == null || guid == null)
            return;

        foreach (AddressableAssetEntry entry in group.entries.ToList())
            group.RemoveAssetEntry(entry);

        var e = settings.CreateOrMoveEntry(guid, group, false, false);
        var entriesAdded = new List<AddressableAssetEntry> { e };
        e.SetLabel("Map", true, true, false);

        group.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, false, true);
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true, false);
    }
}
#endif