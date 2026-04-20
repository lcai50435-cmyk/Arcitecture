using System;
using UnityEngine;

[Serializable]
public class WeaponOptionData
{
    public WeaponType weaponType;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
}
