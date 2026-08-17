using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterSaveData
{
    public float health;
    public float maxHealth;
    public float coin;
    public Vector3 position;
}

[Serializable]
public class InventorySlotSaveData
{
    public int    slotIndex;
    public string itemId;
    public int    count;
}

[Serializable]
public class GameSaveData
{
    public string sceneName;
    public string savedAt;
    public CharacterSaveData character;
    public List<InventorySlotSaveData> inventory;
}
