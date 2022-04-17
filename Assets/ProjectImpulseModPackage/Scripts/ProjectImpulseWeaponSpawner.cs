using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ProjectImpulseWeaponSpawner : NetworkBehaviour {

    [Tooltip("Weapon Id refers to which weapons can be spawned\nPistol=2\nMac10=3\nShotgun=4")]
    public List<int> weaponIds;
    private List<GameObject> weaponPrefabs = new List<GameObject>();

    public bool spawnOnRoundStart = true;
    public float weaponRespawnTime = 30;
    public float weaponDespawnTime = 10f;
    private float respawnTimer = 0;
    private bool canSpawn = true;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private GameObject newestWeaponSpawn;
    private bool isInitialized = false;

    
}
