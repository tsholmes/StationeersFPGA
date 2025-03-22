﻿using System;
using System.Collections.ObjectModel;
using Assets.Scripts.Objects;
using HarmonyLib;
using StationeersMods.Interface;
using UnityEngine;
using UnityEngine.Rendering;
namespace fpgamod
{
    [HarmonyPatch]
    public class PrefabPatch
    {
        public static ReadOnlyCollection<GameObject> prefabs { get; set; }
        [HarmonyPatch(typeof(Prefab), "LoadAll")]
        public static void Prefix()
        {
            try
            {
                Debug.Log("Prefab Patch started");
                foreach (var gameObject in prefabs)
                {
                    Thing thing = gameObject.GetComponent<Thing>();
                    // Additional patching goes here, like setting references to materials(colors) or tools from the game
                    if (thing != null)
                    {
                        Debug.Log(gameObject.name + " added to WorldManager");
                        WorldManager.Instance.SourcePrefabs.Add(thing);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                Debug.LogException(ex);
            }
        }
    }
}
