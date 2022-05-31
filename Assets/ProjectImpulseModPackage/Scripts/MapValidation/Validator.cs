using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ValidationMessage {
    public string title;
    public string message;
}

public abstract class Validator : MonoBehaviour {
    public List<ValidationMessage> warningMessages = new List<ValidationMessage>();
    public ValidationMessage errorMessage;
    bool error = false;
    public string gamemode;

    private void Awake() {
        errorMessage = new ValidationMessage();
        errorMessage.title = "";
        errorMessage.message = "";
    }

    public abstract void Validate();
    public abstract string GetGamemode();

    public List<ValidationMessage> GetWarningMessages() {
        return warningMessages;
    }

    public void AddWarningMessage(string title, string message) {
        ValidationMessage validationMessage = new ValidationMessage();
        validationMessage.title = title;
        validationMessage.message = message;
        warningMessages.Add(validationMessage);
    }

    public void AddErrorMessage(string title, string message) {
        ValidationMessage validationMessage = new ValidationMessage();
        validationMessage.title = title;
        validationMessage.message = message;
        errorMessage = validationMessage;
        error = true;
    }

    public ValidationMessage? GetErrorMessage() {
        if (error)
            return errorMessage;
        return null;
    }

    public ProjectImpulsePlayerSpawnPoint[] GetPlayerSpawnPoints() {
        return FindObjectsOfType<ProjectImpulsePlayerSpawnPoint>();
    }

    public ProjectImpulseObjectSpawner[] GetObjectSpawnPoints() {
        return FindObjectsOfType<ProjectImpulseObjectSpawner>();
    }

}
