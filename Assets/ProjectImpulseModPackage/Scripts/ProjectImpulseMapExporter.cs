#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.SceneManagement;
using System.Linq;
using System.IO;
using System;
using ModIO;
using ModIO.EditorCode;

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
    List<string> configuredPlatforms = new List<string> { "Windows", "Android" };


    private UserProfile user;
    private static bool isAwaitingServerResponse = false;
    private ScriptableModProfile profile;
    private EditableModfile buildProfile;


    [MenuItem("Project Impulse/Map Exporter")]
    public static void ShowMapWindow() {
        GetWindow<ProjectImpulseMapExporter>("Map Exporter");
    }
    private void Awake() {
        scenePath = EditorSceneManager.GetActiveScene().path;
        exportPath = "";
        basePath = FormatPath(UnityEngine.Application.persistentDataPath + "/Export");
        openAfterExport = EditorPrefs.GetBool("OpenAfterExport", false);
    }

    private void OnEnable() {
        buildProfile = new EditableModfile();
        buildProfile.version.value = "0.0.0";

        if (LocalUser.AuthenticationState == AuthenticationState.ValidToken) {
            ModManager.GetAuthenticatedUserProfile((userProfile) => {
                user = userProfile;
                Repaint();
            },
            null);
        }

        LoginWindow.userLoggedIn += OnUserLogin;
    }

    protected virtual void OnDisable() {
        LoginWindow.userLoggedIn -= OnUserLogin;
    }

    protected virtual void OnUserLogin(UserProfile userProfile) {
        OnDisable();
        OnEnable();
    }

    void LoadConfiguredGamemodes() {

        if (!profile)
            return;

        string[] tags = profile.editableModProfile.tags.value;
        configuredGamemodes = new List<string>();
        UnityEngine.Object[] gamemodeValidators = Resources.LoadAll("GamemodeValidators");
        foreach (UnityEngine.Object validatorObject in gamemodeValidators) {
            GameObject go = new GameObject(validatorObject.name);
            go.AddComponent(Type.GetType(validatorObject.name));
            Validator validator = go.GetComponent<Validator>();
            string gamemode = validator.GetGamemode();
            DestroyImmediate(go);
            if (gamemode == "" || gamemode == null)
                continue;

            foreach (string tag in tags) {
                if (tag == gamemode)
                    configuredGamemodes.Add(tag);
            }
        }
    }

    private void OnGUI() {
        LoadConfiguredGamemodes();
        EditorGUIUtility.labelWidth = 80;
        GUILayout.Label("Project Impulse Map Exporter", EditorStyles.largeLabel);
        GUILayout.Space(10);

        if (profile == null) {
            EditorGUILayout.HelpBox("Please select a mod profile as a the upload target.", MessageType.Info);
        } else if (profile.modId > 0) {
            EditorGUILayout.HelpBox(profile.editableModProfile.name.value + " will be updated as used as the upload target on the server.", MessageType.Info);
        } else {
            EditorGUILayout.HelpBox(profile.editableModProfile.name.value + " will be created as a new profile on the server.", MessageType.Info);
        }
        profile = EditorGUILayout.ObjectField("Mod Profile", profile, typeof(ScriptableModProfile), false) as ScriptableModProfile;
        if (profile)
            mapName = profile.editableModProfile.name.value;
        else
            mapName = "";

        using(new EditorGUI.DisabledScope(profile == null)) {
            if (GUILayout.Button("Add Thumbnail", GUILayout.Height(20))) {
                if (Camera.allCameras.Length == 0) {
                    GameObject cam = new GameObject("ThumbnailCamera");
                    cam.AddComponent<Camera>();
                }
                string screenshotPath = Screenshot();
                profile.editableModProfile.logoLocator.value.fileName = Path.GetFileName(screenshotPath);
                profile.editableModProfile.logoLocator.value.url = screenshotPath;
                AssetDatabase.Refresh();
            }
        }

        SceneSettings();
        DrawUILine(Color.grey);

        if (showExportSettings = EditorGUILayout.Foldout(showExportSettings, "Export Settings")) ExportSettings();
        DrawUILine(Color.grey);

        bool containsMapTag = false;
        if (profile) {
            containsMapTag = profile.editableModProfile.tags.value.Contains("Map");
        }
        if (profile == null)
            EditorGUILayout.HelpBox("Please select a mod profile before building.", MessageType.Warning);

        else if (configuredGamemodes.Count == 0)
            EditorGUILayout.HelpBox("One Gamemode must be selected in your mod profile. Goto yourmodprofile>Tags and under 'Gamemodes' set at least one to true", MessageType.Warning);

        bool canBuld = profile != null && configuredGamemodes.Count != 0;
        using (new EditorGUI.DisabledScope(!canBuld)) {
            if (GUILayout.Button("Build Windows", GUILayout.Height(40))) {
                if (!ValidateFeilds() || !ValidateScene())
                    return;

                CreateConfig();
                ExportWindows();

                if (openAfterExport)
                    EditorUtility.RevealInFinder(exportPath);
            }

            if (GUILayout.Button("Build Android", GUILayout.Height(40))) {
                if (!ValidateFeilds() || !ValidateScene())
                    return;

                CreateConfig();
                ExportAndroid();

                if (openAfterExport)
                    EditorUtility.RevealInFinder(exportPath);
            }
        }

        DrawUILine(Color.grey);

        string platformValidation = ValidatePlatforms();
        if (profile != null) {
            if (this.user == null)
                EditorGUILayout.HelpBox("Please sign into your mod.io account in order to publish", MessageType.Info);
            if (platformValidation != "")
                EditorGUILayout.HelpBox("You must build for " + platformValidation + " in order to publish.", MessageType.Warning);
            if (!containsMapTag)
                EditorGUILayout.HelpBox("The 'Map' tag must be set in your mod profile. Goto yourmodprofile>Tags and under 'Mod Type' set 'Map' to true.", MessageType.Warning);
        }

        LoginUI();

        using (new EditorGUI.DisabledScope(this.user == null || platformValidation != "" || !containsMapTag)) {
            if (GUILayout.Button("Upload Map", GUILayout.Height(40))) {
                UploadToServer();
            }
        }

    }

    protected virtual void Update() {
        if (user == null || LocalUser.Profile == null || LocalUser.Profile == null || user.id != LocalUser.Profile.id || user.username.Length == 0) {
            this.user = null;
            Repaint();
        }
    }

    private void LoginUI() {
        EditorGUILayout.BeginHorizontal();
        if (this.user == null) {
            EditorGUILayout.LabelField("Not logged in to mod.io");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Log In")) {
                LoginWindow.GetWindow<LoginWindow>("Login to mod.io");
            }
        } else {
            EditorGUILayout.LabelField("Logged in as:  " + this.user.username);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Log Out")) {
                EditorApplication.delayCall += () => {
                    if (EditorDialogs.ConfirmLogOut(this.user.username)) {
                        this.user = null;

                        LocalUser.instance = new LocalUser();
                        LocalUser.Save();

                        Repaint();
                    }
                };
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void SceneSettings() {
        GUILayout.BeginHorizontal();
        scenePath = EditorSceneManager.GetActiveScene().path;
        GUILayout.Label("Scene Path: " + scenePath, EditorStyles.label);
        GUILayout.EndHorizontal();
    }

    private void ExportSettings() {
        basePath = FormatPath(UnityEngine.Application.persistentDataPath + "/Mods");
        exportPath = FormatPath(basePath + "/" + mapName);

        if (GUILayout.Button("Open Export Folder"))
            EditorUtility.RevealInFinder(basePath + "/");

        EditorGUIUtility.labelWidth = 190;
        openAfterExport = EditorGUILayout.Toggle("Open Export Folder On Complete", openAfterExport);
        EditorPrefs.SetBool("OpenAfterExport", openAfterExport);

        GUILayout.Space(10);
        GUILayout.Label("Export path: " + exportPath, EditorStyles.label);
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
        foreach(Camera camera in Camera.allCameras) {
            camera.gameObject.SetActive(false);
        }

        LoadConfiguredGamemodes();
        UnityEngine.Object[] gamemodeValidators = Resources.LoadAll("GamemodeValidators");
        foreach (UnityEngine.Object validatorObject in gamemodeValidators) {

            GameObject go = new GameObject(validatorObject.name);
            go.AddComponent(Type.GetType(validatorObject.name));
            Validator validator = go.GetComponent<Validator>();
            string gamemode = validator.GetGamemode();
            if (gamemode != "" && !configuredGamemodes.Contains(gamemode)) {
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

    private string ValidatePlatforms() {
        if (!Directory.Exists(exportPath))
            return "Windows";

        string[] folders = Directory.GetDirectories(exportPath);
        if (folders.Length == 0)
            return "Windows";

        foreach (string platform in configuredPlatforms) {
            bool isPlatformBuilt = false;
            foreach (string folder in folders) {
                if (platform == "Windows" && Path.GetFileName(folder) == "StandaloneWindows64") {
                    isPlatformBuilt = true;
                    break;
                } else if (platform == "Android" && Path.GetFileName(folder) == "Android") {
                    isPlatformBuilt = true;
                    break;
                }
            }

            if (!isPlatformBuilt)
                return platform;
        }
        return "";
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
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
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
            "{UnityEngine.Application.persistentDataPath}/Mods/{LOCAL_FILE_NAME}/" + EditorUserBuildSettings.selectedStandaloneTarget
        );

        AddressableAssetSettingsDefaultObject.Settings.profileSettings.SetValue(
            AddressableAssetSettingsDefaultObject.Settings.activeProfileId,
            "Local.BuildPath",
            Application.persistentDataPath + "/Mods/" + FormatPath(mapName) + "/" + EditorUserBuildSettings.selectedStandaloneTarget
        );
        AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
    }

    private void CreateConfig() {

        if (!File.Exists(exportPath))
            CreateFolder(exportPath);

        string configContent = "\n//WARNING EDITING THIS FILE MAY RESULT IN ERRORS\n\n";
        configContent += "// type represents the type of mod \n";
        configContent += "type=Map\n";
        configContent += "// name is the name of the map \n";
        configContent += "name=" + mapName + "\n";
        configContent += "// gamemodes is a list of gamemodes that this map is configured for\n";
        configContent += "gamemodes=";

        LoadConfiguredGamemodes();

        for (int i = 0; i < configuredGamemodes.Count; i++)
            configContent += configuredGamemodes[i] + (i == configuredGamemodes.Count - 1 ? "\n" : ",");

        File.WriteAllText(FormatPath(exportPath + "/" + mapName + "Config.cfg"), configContent);
    }

    protected virtual void UploadToServer() {
        isAwaitingServerResponse = true;

        string profileFilePath = AssetDatabase.GetAssetPath(profile);

        Action<WebRequestError> onSubmissionFailed = (e) => {
            EditorUtility.DisplayDialog("Upload Failed",
                                        "Failed to update the mod profile on the server.\n"
                                        + e.displayMessage,
                                        "Close");

            isAwaitingServerResponse = false;
            Repaint();
        };

        if (profile.modId > 0) {
            ModManager.SubmitModChanges(profile.modId,
                                        profile.editableModProfile,
                                        (m) => ModProfileSubmissionSucceeded(m, profileFilePath),
                                        onSubmissionFailed);
        } else {
            ModManager.SubmitNewMod(profile.editableModProfile,
                                    (m) => ModProfileSubmissionSucceeded(m, profileFilePath),
                                    onSubmissionFailed);
        }
    }

    private void ModProfileSubmissionSucceeded(ModProfile updatedProfile,
                                                   string profileFilePath) {
        if (updatedProfile == null) {
            isAwaitingServerResponse = false;
            return;
        }


        // Update ScriptableModProfile
        profile.modId = updatedProfile.id;
        profile.editableModProfile = EditableModProfile.CreateFromProfile(updatedProfile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        // Upload Build
        if (Directory.Exists(exportPath)) {
            Action<WebRequestError> onSubmissionFailed = (e) => {
                EditorUtility.DisplayDialog("Upload Failed",
                                            "Failed to upload the mod build to the server.\n"
                                            + e.displayMessage,
                                            "Close");

                isAwaitingServerResponse = false;
                Repaint();

            };

            ModManager.UploadModBinaryDirectory(profile.modId,
                                                buildProfile,
                                                exportPath,
                                                true,
                                                mf => NotifySubmissionSucceeded(updatedProfile.name,
                                                                                updatedProfile.profileURL),
                                                onSubmissionFailed);

        } else {
            NotifySubmissionSucceeded(updatedProfile.name, updatedProfile.profileURL);
        }
    }

    private void NotifySubmissionSucceeded(string modName, string modProfileURL) {
        EditorUtility.DisplayDialog("Submission Successful",
                                    modName + " was successfully updated on the server."
                                    + "\nView the changes here: " + modProfileURL,
                                    "Close");
        isAwaitingServerResponse = false;
        Repaint();
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

    private string Screenshot() {
        var timestamp = System.DateTime.Now;
        var stampString = string.Format("_{0}-{1:00}-{2:00}_{3:00}-{4:00}-{5:00}", timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, timestamp.Second);
        string screenshotFolder = Application.dataPath + "/Thumbnails/" + mapName + "/";
        if (Directory.Exists(screenshotFolder))
            Directory.Delete(screenshotFolder, true);
        
        Directory.CreateDirectory(screenshotFolder);
        string screenshotPath = screenshotFolder + "/Screenshot" + stampString + ".png";

        RenderTexture screenTexture = new RenderTexture(1920, 1080, 16);
        Camera.allCameras[0].targetTexture = screenTexture;
        RenderTexture.active = screenTexture;
        Camera.allCameras[0].Render();
        Texture2D renderedTexture = new Texture2D(1920, 1080);
        renderedTexture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        RenderTexture.active = null;
        byte[] byteArray = renderedTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(screenshotPath, byteArray);

        AssetDatabase.Refresh();

        return screenshotPath;
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