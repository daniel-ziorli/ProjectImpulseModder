using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamDeathMatchValidator : Validator {

    public override string GetGamemode() {
        gamemode = "Team Death Match";
        return gamemode;
    }
    public override void Validate() {
        GetGamemode();
        Debug.Log(gamemode);
        if (GetPlayerSpawnPoints().Length < 10)
            AddWarningMessage("Warning Not Enough Player Spawn Points", gamemode + "\nYou have less than 10 spawn points in your scene we recomend at least 10 but more is always better.");

        if (GetObjectSpawnPoints().Length == 0)
            AddWarningMessage("Warning No Object Spawn Points", gamemode + "\nYou have no object spawners in your scene. If you would like objects in your level go to the Project Impulse Mod Package > Prefabs and add a 'ObjectSpawnPoint' to your scene.");
    }
}
