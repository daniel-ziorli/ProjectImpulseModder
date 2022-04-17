using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectImpulsePlayerSpawnPoint : MonoBehaviour {
    [Tooltip("Team Spawn Point determins if a spawn point is specific to a team. If false all player can spawn at this point.")]
    public bool teamSpawnPoint = false;
    [Tooltip("Team ID refers to which team can spawn at this point. This value must be above 0 and below 19.")]
    [Range(0,19)]
    public int teamId = 0;

}