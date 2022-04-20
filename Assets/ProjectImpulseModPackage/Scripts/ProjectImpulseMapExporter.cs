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
using System;
using Unity.EditorCoroutines.Editor;
using System.Collections;
using UnityEditor.Compilation;

public class ProjectImpulseMapExporter : EditorWindow {
    string mapName = "";
    string basePath = "";
    string exportPath = "";
    string scenePath = "";
    bool showMapSettings = true;
    bool showSceneSettings = true;
    bool showExportSettings = true;
    bool showConfiguredGamemodes = true;
    bool showPlatforms = true;


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
        basePath = FormatPath(UnityEngine.Application.persistentDataPath + "/Export");
        openAfterExport = EditorPrefs.GetBool("OpenAfterExport", false);
    }

    void LoadGamemodes() {
        Dictionary<string, bool> temp = null;
        if (allGamemodes != null)
            temp = new Dictionary<string, bool>(allGamemodes);
        allGamemodes = new Dictionary<string, bool>();
        UnityEngine.Object[] gamemodeValidators = Resources.LoadAll("GamemodeValidators");
        foreach (UnityEngine.Object validatorObject in gamemodeValidators) {
            GameObject go = new GameObject(validatorObject.name);
            go.AddComponent(Type.GetType(validatorObject.name));
            Validator validator = go.GetComponent<Validator>();
            string gamemode = validator.GetGamemode();
            DestroyImmediate(go);
            if (gamemode == "" || gamemode == null)
                continue;
            if (temp != null && temp.Keys.Contains(gamemode))
                allGamemodes[gamemode] = temp[gamemode];
            else
                allGamemodes[gamemode] = true;
        }
    }

    private void OnGUI() {
        LoadGamemodes();

        EditorGUIUtility.labelWidth = 80;
        GUILayout.Label("Project Impulse Map Exporter", EditorStyles.largeLabel);
        GUILayout.Space(10);

        if (showMapSettings = EditorGUILayout.Foldout(showMapSettings, "Map Settings")) MapSettings();
        DrawUILine(Color.grey);


        if (showSceneSettings = EditorGUILayout.Foldout(showSceneSettings, "Scene Settings")) SceneSettings();
        DrawUILine(Color.grey);

        if (showExportSettings = EditorGUILayout.Foldout(showExportSettings, "Export Settings")) ExportSettings();
        DrawUILine(Color.grey);

        if (GUILayout.Button("Build Windows", GUILayout.Height(40))) {

            if (!ValidateFeilds() || !ValidateScene())
                return;

            CreateConfig(configuredGamemodes);
            ExportWindows();

            if (openAfterExport)
                EditorUtility.RevealInFinder(exportPath);
        }

        if (GUILayout.Button("Build Android", GUILayout.Height(40))) {

            if (!ValidateFeilds() || !ValidateScene())
                return;

            CreateConfig(configuredGamemodes);
            ExportAndroid();

            if (openAfterExport)
                EditorUtility.RevealInFinder(exportPath);
        }
    }

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
        basePath = FormatPath(UnityEngine.Application.persistentDataPath + "/Maps");
        exportPath = FormatPath(basePath + "/" + mapName);

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

    void ExportWindows() {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows);
        EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.StandaloneWindows64;
        BuildAddressable();
    }

    void ExportMac() {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
        EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.StandaloneOSX;
        BuildAddressable();
    }

    void ExportLinux() {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
        EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.StandaloneOSX;
        BuildAddressable();
    }

    void ExportAndroid() {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.Android;
        BuildAddressable();
    }

    private void BuildAddressable(object obj = null) {
        var group = AddressableAssetSettingsDefaultObject.Settings.FindGroup("Default Local Group");
        var guid = AssetDatabase.AssetPathToGUID(scenePath);
        if (group == null || guid == null)
            return;

        foreach (AddressableAssetEntry entry in group.entries.ToList())
            group.RemoveAssetEntry(entry);

        var e = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, group, false, false);
        var entriesAdded = new List<AddressableAssetEntry> { e };
        e.SetLabel("Map", true, true, false);

        group.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, false, true);
        AddressableAssetSettingsDefaultObject.Settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true, false);

        AddressableAssetSettingsDefaultObject.Settings.profileSettings.SetValue(
            AddressableAssetSettingsDefaultObject.Settings.activeProfileId,
            "Local.LoadPath",
            "{UnityEngine.Application.persistentDataPath}/Maps/" + FormatPath(mapName) + "/" + EditorUserBuildSettings.selectedStandaloneTarget
        );

        AddressableAssetSettingsDefaultObject.Settings.profileSettings.SetValue(
            AddressableAssetSettingsDefaultObject.Settings.activeProfileId,
            "Local.BuildPath",
            Application.persistentDataPath + "/Maps/" + FormatPath(mapName) + "/" + EditorUserBuildSettings.selectedStandaloneTarget
        );
        AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
    }

    private void CreateConfig(List<string> gamemodes) {
        if (!File.Exists(exportPath))
            CreateFolder(exportPath);

        string configContent = "\n//WARNING EDITING THIS FILE MAY RESULT IN ERRORS\n\n";
        configContent += "// name is the name of the map \n";
        configContent += "name=" + mapName + "\n";
        configContent += "// gamemodes is a list of gamemodes that this map is configured for\n";
        configContent += "gamemodes=";
        for (int i = 0; i < gamemodes.Count; i++)
            configContent += gamemodes[i] + (i == gamemodes.Count - 1 ? "\n" : ",");

        File.WriteAllText(FormatPath(exportPath + "/" + mapName + "Config.cfg"), configContent);
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