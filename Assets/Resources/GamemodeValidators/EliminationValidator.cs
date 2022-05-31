using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EliminationValidator : Validator {

    public override string GetGamemode() {
        gamemode = "Elimination";
        return gamemode;
    }
    public override void Validate() {
        GetGamemode();
        Debug.Log(gamemode);

        ProjectImpulsePlayerSpawnPoint[] spawnPoints = GetPlayerSpawnPoints();

        bool defenderSpawnPoint = false;
        bool attackerSpawnPoint = false;
        foreach (ProjectImpulsePlayerSpawnPoint spawnPoint in spawnPoints) {

            if (spawnPoint.teamSpawnPoint && spawnPoint.teamId == 0)
                defenderSpawnPoint = true;
            else if (spawnPoint.teamSpawnPoint && spawnPoint.teamId == 1)
                attackerSpawnPoint = true;
        }

        if (!defenderSpawnPoint)
            AddErrorMessage("Error Missing Defender Spawn Point", gamemode + "\nMissing Defender Spawn Point. Add a spawn point to your scene and set Team Spawn Point to True. Then set the team index to 0.");

        if (!attackerSpawnPoint)
            AddErrorMessage("Error Missing Attacker Spawn Point", gamemode + "\nMissing Attacker Spawn Point. Add a spawn point to your scene and set Team Spawn Point to True. Then set the team index to 1.");

        if (spawnPoints.Length < 10)
            AddWarningMessage("Warning Not Enough Player Spawn Points", gamemode + "\nYou have less than 10 spawn points in your scene we recomend at least 10 but more is always better.");

        if (GetObjectSpawnPoints().Length == 0)
            AddWarningMessage("Warning No Object Spawn Points", gamemode + "\nYou have no object spawners in your scene. If you would like objects in your level go to the Project Impulse Mod Package > Prefabs and add a 'ObjectSpawnPoint' to your scene.");
    }
}
