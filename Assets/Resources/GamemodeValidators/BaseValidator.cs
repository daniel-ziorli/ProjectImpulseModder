using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BaseValidator : Validator {

    public override string GetGamemode() {
        gamemode = "";
        return gamemode;
    }
    public override void Validate() {
        gamemode = null;

        foreach (Camera camera in Camera.allCameras) {
            DestroyImmediate(camera.gameObject);
        }

        foreach (NetworkIdentity networkIdentity in Resources.FindObjectsOfTypeAll<NetworkIdentity>()) {
            networkIdentity.serverOnly = true;
        }

        if (!Object.FindObjectOfType<ProjectImpulseDeathZone>())
            AddErrorMessage("Error No Death Zone In Scene", "Please go to the project impulse mod package, under the prefab section add the 'Death Zone' prefab to your scene.");

        if (GetPlayerSpawnPoints().Length == 0)
            AddErrorMessage("Error No Player Spawn Points", "Please go to the project impulse mod package, under the prefab section add the 'Player Spawn Point' prefab to your scene. We recomend at least 10 but more is better.");
    }
}
