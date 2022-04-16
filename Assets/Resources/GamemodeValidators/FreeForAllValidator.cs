using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeForAllValidator : Validator {
    public override string GetGamemode() {
        gamemode = "Free For All";
        return gamemode;
    }
    public override void Validate() {
        GetGamemode();
        Debug.Log(gamemode);
        if (GetPlayerSpawnPoints().Length < 20)
            AddWarningMessage("Warning Not Enough Player Spawn Points", gamemode + "\nYou have less than 20 spawn points in your scene we recomend at least 20 for Free For All.");

        if (GetWeaponSpawnPoints().Length == 0)
            AddWarningMessage("Warning No Weapon Spawn Points", gamemode + "\nYou have no weapon spawners in your scene. If you would like weapons in your level go to the Project Impulse Mod Package > Prefabs and add a 'WeaponSpawnPoint' to your scene.");
    }
}
