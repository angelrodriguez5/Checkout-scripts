using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Layers
{
    public static LayerMask Default = 1;
    public static LayerMask Ground = 1 << 6;
    public static LayerMask Shelf = 1 << 7;
    public static LayerMask Scenery = 1 << 8;
    public static LayerMask NavMeshIgnore = 1 << 9;
    public static LayerMask Player = 1 << 10;
    public static LayerMask NPC = 1 << 11;
    public static LayerMask Item = 1 << 12;
}
