using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes a GameObject go to Don't Destroy on Load and Caches.
/// </summary>
public class DontDestroyMe : MonoBehaviour
{
    public static readonly Dictionary<string, GameObject> Cache = new();

    public static GameObject GetNotDestroyedObject(string name) => Cache.ContainsKey(name) ? Cache[name] : null;

    public void Start()
    {
        Cache.Add(gameObject.name, gameObject);
        DontDestroyOnLoad(gameObject);
    }
}
