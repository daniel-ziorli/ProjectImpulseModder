using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ProjectImpulseObjectSpawner : NetworkBehaviour {
    [Tooltip("If set the spawner will spawn on round start. If not set the objectRespawnTime will have to be meet before a object is spawned.")]
    [SerializeField] private bool spawnOnStart = true;
    [Tooltip("If set the spawner will spawn objects continuously. If not set the spawner will only spawn a new item if the previous item has left the spawn area.")]
    [SerializeField] private bool continuousSpawning = false;
    [Tooltip("Amount of time in seconds it takes for the object to respawn. If <= 0 the object will not respawn throughout the round.")]
    [SerializeField] private float objectRespawnTime = 30.0f;
    [Tooltip("Amount of time in seconds it takes for the object to despawn. Timer only counts down if the object is not moving and will be reset if the object begins to move again. If <= 0 the object will never despawn.")]
    [SerializeField] private float objectDespawnTime = 30.0f;
    private float respawnTimer = 0;
    [Tooltip("Weapon Id refers to which weapons can be spawned\nPistol=0\nMac10=1\nShotgun=2\n")]
    [SerializeField] private List<int> spawnableObjectIds;
}
