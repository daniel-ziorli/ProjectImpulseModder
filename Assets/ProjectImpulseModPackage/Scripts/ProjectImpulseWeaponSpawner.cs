using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using MEC;

[Serializable]
public class WeaponData {
    public string weaponName;
    public bool spawnWeapon;
    public int weaponIndex;
}

public class ProjectImpulseWeaponSpawner : NetworkBehaviour {
    [SerializeField]
    public List<WeaponData> weapons;
    public List<GameObject> weaponPrefabs = new List<GameObject>();

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
