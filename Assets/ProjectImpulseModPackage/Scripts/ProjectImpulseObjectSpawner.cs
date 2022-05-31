using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ProjectImpulseObjectSpawner : NetworkBehaviour {
    [Tooltip("Weapon Id refers to which weapons can be spawned\nPistol=0\nMac10=1\nShotgun=2\nBall=3\n")]
    public List<int> objectIds;
    [Tooltip("If set the spawner will spawn on round start. If not set the objectRespawnTime will have to be meet before a object is spawned.")]
    public bool spawnOnStart = true;
    [Tooltip("If set the spawner will spawn objects continuously. If not set the spawner will only spawn a new item if the previous item has left the spawn area.")]
    public bool continuousSpawning = false;
    [Tooltip("Amount of time in seconds it takes for the object to respawn. If <= 0 the object will not respawn throughout the round.")]
    public float objectRespawnTime = 30.0f;
    [Tooltip("Amount of time in seconds it takes for the object to despawn. Timer only counts down if the object is not moving and will be reset if the object begins to move again. If <= 0 the object will never despawn.")]
    public float objectDespawnTime = 30.0f;
}