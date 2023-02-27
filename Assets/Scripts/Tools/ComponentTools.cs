using UnityEngine;

public static class ComponentTools
{
    public static bool HasComponent<T>(GameObject gameObject) => gameObject.GetComponent(typeof(T)) != null;
}