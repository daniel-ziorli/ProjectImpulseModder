#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.SceneManagement;
using System.Linq;
using System.IO;
using System;

public class ProjectImpulseMapExporter : EditorWindow {
    string mapName = "";
    string basePath = "";
    string exportPath = "";
    string customBasePath = "";
    string scenePath = "";
    bool showMapSettings = true;
    bool showSceneSettings = true;
    bool showExportSettings = true;
    bool showConfiguredGamemodes = true;

    bool openAfterExport;

    List<string> configuredGamemodes = new List<string>();
    Dictionary<string, bool> allGamemodes = new Dictionary<string, bool>();

    [MenuItem("Project Impulse/Map Exporter")]
    public static void ShowMapWindow() {
        GetWindow<ProjectImpulseMapExporter>("Map Exporter");
    }
    private void Awake() {
        scenePath = EditorSceneManager.GetActiveScene().path;
        mapName = EditorPrefs.GetString("MapName", "Your Map Name");
        exportPath = ""; //FormatPath(UnityEngine.Application.dataPath + "/Export/" + mapName);
        basePath = FormatPath(UnityEngine.Application.dataPath + "/Export");
        customBasePath = EditorPrefs.GetString("CustomBasePath", "");
        openAfterExport = EditorPrefs.GetBool("OpenAfterExport", false);

        if (allGamemodes.Count != 0)
            return;
        UnityEngine.Object[] gamemodeValidators = Resources.LoadAll("GamemodeValidators");
        foreach (UnityEngine.Object validatorObject in gamemodeValidators) {
            GameObject go = new GameObject(validatorObject.name);
            go.AddComponent(Type.GetType(validatorObject.name));
            Validator validator = go.GetComponent<Validator>();
            string gamemode = validator.GetGamemode();
            DestroyImmediate(go);
            if (gamemode != null && gamemode != "")
                allGamemodes[gamemode] = true;
        }
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
            if (!ValidateFeilds() || !ValidateScene())
                return;

            ExportMap();
        }
    }

    Rect buttonRect;
    private void MapSettings() {
        mapName = EditorGUILayout.TextField("Map Name: ", mapName);
        EditorPrefs.SetString("MapName", mapName);

        EditorGUIUtility.labelWidth = 200;

        if (showConfiguredGamemodes = EditorGUILayout.Foldout(showConfiguredGamemodes, "Configured Gamemodes")) {
            foreach (KeyValuePair<string, bool> g in allGamemodes.ToList())
                allGamemodes[g.Key] = EditorGUILayout.Toggle(g.Key, allGamemodes[g.Key]);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("All", GUILayout.Width(100)))
                SelectAllGamemodes();
            if (GUILayout.Button("None", GUILayout.Width(100)))
                DeselectAllGamemodes();
            GUILayout.EndHorizontal();
        }
    }

    private void SelectAllGamemodes() {
        foreach (KeyValuePair<string, bool> g in allGamemodes.ToList())
            allGamemodes[g.Key] = true;
    }

    private void DeselectAllGamemodes() {
        foreach (KeyValuePair<string, bool> g in allGamemodes.ToList())
            allGamemodes[g.Key] = false;
    }

    private void SceneSettings() {
        GUILayout.BeginHorizontal();
        scenePath = EditorSceneManager.GetActiveScene().path;
        GUILayout.Label("Scene Path: " + scenePath, EditorStyles.whiteLabel);
        GUILayout.EndHorizontal();
    }


    private void ExportSettings() {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Custom Export Path")) {
            customBasePath = EditorUtility.OpenFolderPanel("Set Custom Export Path", EditorPrefs.GetString("CustomBasePath", ""), "");
            EditorPrefs.SetString("CustomBasePath", customBasePath);
        }

        if (GUILayout.Button("Reset Export Path")) {
            customBasePath = "";
            basePath = FormatPath(UnityEngine.Application.dataPath + "/Export");
        }

        if (customBasePath == "")
            basePath = FormatPath(UnityEngine.Application.dataPath + "/Export");
        else
            basePath = FormatPath(customBasePath);

        exportPath = FormatPath(basePath + "/" + mapName);

        GUILayout.EndHorizontal();

        if (GUILayout.Button("Open Export Folder"))
            EditorUtility.RevealInFinder(basePath + "/");

        EditorGUIUtility.labelWidth = 190;
        openAfterExport = EditorGUILayout.Toggle("Open Export Folder On Complete", openAfterExport);
        EditorPrefs.SetBool("OpenAfterExport", openAfterExport);

        GUILayout.Space(10);
        GUILayout.Label("Export path: " + exportPath, EditorStyles.whiteLabel);
    }

    private bool ValidateFeilds() {
        if (basePath == null || basePath == "") {
            DisplayError("Error Invalid Export Path", "Please choose a valid export path or click 'Reset Export Path' to restore it to the default.");
            return false;
        }

        if (mapName.Contains("/") || mapName.Contains(@"\")) {
            DisplayError("Error Invalid Map Name", @"Map name can not contain characters '/' or '\' please remove these characters.");
            return false;
        }

        if (mapName == "") {
            DisplayError("Error Invalid Map Name", "Please enter a valid map name.");
            return false;
        }

        return true;
    }

    private bool ValidateScene() {
        configuredGamemodes = new List<string>();
        UnityEngine.Object[] gamemodeValidators = Resources.LoadAll("GamemodeValidators");
        foreach (UnityEngine.Object validatorObject in gamemodeValidators) {

            GameObject go = new GameObject(validatorObject.name);
            go.AddComponent(Type.GetType(validatorObject.name));
            Validator validator = go.GetComponent<Validator>();
            string gamemode = validator.GetGamemode();
            if (gamemode != "" && allGamemodes[gamemode] == false) {
                DestroyImmediate(go);
                continue;
            }

            validator.Validate();

            ValidationMessage? errorMessage = validator.GetErrorMessage();
            if (errorMessage != null) {
                DisplayError(errorMessage.Value.title, errorMessage.Value.message);
                DestroyImmediate(go);
                return false;
            }

            foreach (ValidationMessage message in validator.GetWarningMessages()) {
                if (!DisplayWarning(message.title, message.message)) {
                    DestroyImmediate(go);
                    return false;
                }
            }
            if (validator.gamemode != null && validator.gamemode != "")
                configuredGamemodes.Add(validator.gamemode);
            DestroyImmediate(go);
        }

        return true;
    }

    private void DisplayError(string title, string error) {
        EditorUtility.DisplayDialog(title, error, "Ok", "");
    }

    private bool DisplayWarning(string title, string warning) {
        return EditorUtility.DisplayDialog(title, warning, "Continue", "Cancel");
    }

    private void ExportMap() {
        AddScene(scenePath);
        if (!BuildAddressable())
            return;

        DeleteFolder(exportPath);
        CreateFolder(exportPath);

        CreateConfig(configuredGamemodes);

        var info = new DirectoryInfo(UnityEngine.Application.dataPath + "/Export");
        var fileInfo = info.GetFiles();
        foreach (var file in fileInfo)
            if (file.Name.Split(".").Length >= 2)
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

    private void CreateConfig(List<string> gamemodes) {
        string configContent = "\n//WARNING EDITING THIS FILE MAY RESULT IN ERRORS\n\n";
        configContent += "// name is the name of the map \n";
        configContent += "name=" + mapName + "\n";
        configContent += "// gamemodes is a list of gamemodes that this map is configured for\n";
        configContent += "gamemodes=";
        for (int i = 0; i < gamemodes.Count; i++)
            configContent += gamemodes[i] + (i == gamemodes.Count - 1 ? "\n" : ",");

        File.WriteAllText(FormatPath(UnityEngine.Application.dataPath + "/Export/" + mapName + "Config.cfg"), configContent);
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

    public static void DrawUILine(Color color, int thickness = 2, int padding = 10) {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;
        r.y += padding / 2;
        r.x -= 2;
        r.width += 6;
        EditorGUI.DrawRect(r, color);
    }
}
#endif