#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.SceneManagement;
using System.Linq;
using System.IO;

public class ProjectImpulseMapEditor : EditorWindow {
    string mapName = "";
    string exportPath = "";
    string customExportPath = "";
    string scenePath = "";
    bool showMapSettings = true;
    bool showSceneSettings = true;
    bool showExportSettings = true;
    bool openAfterExport;

    [MenuItem("Project Impulse/Map Exporter")]
    public static void ShowMapWindow() {
        GetWindow<ProjectImpulseMapEditor>("Map Exporter");
    }
    private void Awake() {
        scenePath = EditorSceneManager.GetActiveScene().path;
        mapName = EditorPrefs.GetString("MapName", "Your Map Name");
        exportPath = EditorPrefs.GetString("CustomExportPath", FormatPath(UnityEngine.Application.dataPath + "/Export/" + mapName));
        openAfterExport = EditorPrefs.GetBool("OpenAfterExport", false);
        customExportPath = exportPath;
    }

    private void OnGUI() {
        EditorGUIUtility.labelWidth = 80;
        GUILayout.Label("Project Impulse Map Exporter", EditorStyles.largeLabel);
        GUILayout.Space(10);

        if (showMapSettings = EditorGUILayout.Foldout(showMapSettings, "Map Settings")) MapSettings();
        DrawUILine(Color.grey);


        if (showSceneSettings = EditorGUILayout.Foldout(showSceneSettings, "Scene Settings")) SceneSettings();
        DrawUILine(Color.grey);

        if (showExportSettings = EditorGUILayout.Foldout(showExportSettings, "Export Settings")) ExportSettings();
        DrawUILine(Color.grey);

        if (GUILayout.Button("Export Map", GUILayout.Height(40))) {
            if (!ValidateFeilds())
                return;

            ExportMap();
        }
    }

    private void MapSettings() {
        mapName = EditorGUILayout.TextField("Map Name: ", mapName);
        EditorPrefs.SetString("MapName", mapName);
    }

    private void SceneSettings() {
        GUILayout.BeginHorizontal();
        scenePath = EditorGUILayout.TextField("Scene Path: ", scenePath);
        if (GUILayout.Button("Get Current Scene Path", GUILayout.Width(180)))
            scenePath = EditorSceneManager.GetActiveScene().path;
        GUILayout.EndHorizontal();
    }


    private void ExportSettings() {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Custom Export Path"))
            customExportPath = EditorUtility.OpenFolderPanel("Set Custom Export Path", exportPath, "");
        if (GUILayout.Button("Reset Export Path"))
            customExportPath = "";

        if (customExportPath == "")
            exportPath = FormatPath(UnityEngine.Application.dataPath + "/Export/" + mapName);
        else
            exportPath = FormatPath(customExportPath + "/" + mapName);
        EditorPrefs.SetString("CustomExportPath", exportPath);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Open Export Folder"))
            EditorUtility.RevealInFinder(exportPath);
        EditorGUIUtility.labelWidth = 190;
        openAfterExport = EditorGUILayout.Toggle("Open Export Folder On Complete", openAfterExport);
        EditorPrefs.SetBool("OpenAfterExport", openAfterExport);
        GUILayout.Space(10);
        GUILayout.Label("Export path: " + exportPath, EditorStyles.whiteLabel);
    }

    private bool ValidateFeilds() {
        if (exportPath == null || exportPath == "") {
            DisplayError("Error Invalid Export Path", "Please choose a valid export path or click 'Reset Export Path' to restore it to the default.");
            return false;
        }

        if (mapName.Contains("/") || mapName.Contains(@"\")) {
            DisplayError("Error Invalid Map Name", @"Map name can not contain characters '/' or '\' please remove these characters");
            return false;
        }

        if (mapName == "") {
            DisplayError("Error Invalid Map Name", "Please enter a valid map name");
            return false;
        }

        // if (!File.Exists(UnityEngine.Application.dataPath + scenePath)) {
        //     DisplayError("Error Invalid Scene Path", "Please supply a valid scene path by selecting the scene you wish to export, right clicking it and selecting 'Copy Path' or open the scene and click 'Get Current Scene Path'.");
        //     return false;
        // }

        return true;
    }

    private void DisplayError(string title, string error) {
        EditorUtility.DisplayDialog(title, error, "Ok", "");
    }

    public static void DrawUILine(Color color, int thickness = 2, int padding = 10) {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;
        r.y += padding / 2;
        r.x -= 2;
        r.width += 6;
        EditorGUI.DrawRect(r, color);
    }

    private void ExportMap() {
        AddScene(scenePath);
        bool success = BuildAddressable();
        if (!success)
            return;

        DeleteFolder(exportPath);
        CreateFolder(exportPath);

        CreateConfig();

        var info = new DirectoryInfo(UnityEngine.Application.dataPath + "/Export");
        var fileInfo = info.GetFiles();
        foreach (var file in fileInfo)
            File.Move(file.FullName, exportPath + "/" + file.Name);

        if (openAfterExport)
            EditorUtility.RevealInFinder(exportPath);
        AssetDatabase.Refresh();
    }

    private bool BuildAddressable() {
        AddressableAssetSettingsDefaultObject.Settings.profileSettings.SetValue(AddressableAssetSettingsDefaultObject.Settings.activeProfileId, "Local.LoadPath", "{UnityEngine.Application.dataPath}/Maps/" + FormatPath(mapName));
        AddressableAssetSettings.CleanPlayerContent();
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        bool success = string.IsNullOrEmpty(result.Error);
        return success;
    }

    private void CreateConfig() {
        string configContent = "\n//WARNING EDITING THIS FILE MAY RESULT IN ERRORS\n\n// name is the name of the map \nname=" + mapName + "\n// gamemodes is a list of gamemodes that this map is configured for\ngamemodes=Team Death Match,Free For All,Elimination\n";
        File.WriteAllText(FormatPath(UnityEngine.Application.dataPath + "/Export/" + mapName + "Config.cfg"), configContent);
    }

    private void DeleteFolder(string path) {
        if (!Directory.Exists(path))
            return;
        FileUtil.DeleteFileOrDirectory(path);
    }

    private void CreateFolder(string path) {
        Directory.CreateDirectory(path);
    }

    private string FormatPath(string path) {
        return path.Replace(" ", "").Replace(@"\", "/");
    }

    public void AddScene(string path) {
        var settings = AddressableAssetSettingsDefaultObject.Settings;

        if (!settings)
            return;

        var group = settings.FindGroup("Default Local Group");
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