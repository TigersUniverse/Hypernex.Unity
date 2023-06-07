using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Makes a GameObject go to Don't Destroy on Load and Caches.
/// </summary>
public class DontDestroyMe : MonoBehaviour
{
    public static readonly Dictionary<string, GameObject> Cache = new();

    public static GameObject GetNotDestroyedObject(string name) => Cache.ContainsKey(name) ? Cache[name] : null;

    private bool IsPresent => GetNotDestroyedObject(gameObject.name);

    public void Register()
    {
        if (!IsPresent)
        {
            Cache.Add(gameObject.name, gameObject);
            DontDestroyOnLoad(gameObject);
        }
    }

    public void Start() => Register();

    public void MoveToScene(Scene s)
    {
        Cache.Remove(gameObject.name);
        SceneManager.MoveGameObjectToScene(gameObject, s);
    }

    public GameObject Clone()
    {
        GameObject g = Instantiate(gameObject);
        Destroy(g.GetComponent<DontDestroyMe>());
        return g;
    }
}
