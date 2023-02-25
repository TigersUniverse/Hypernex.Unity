using UnityEngine;

public static class ColorTools
{
    // This is ALL YOU HAD TO DO UNITY.
    public static Color RGBtoHSV(int r, int g, int b, int a = 255) => new(r / 255f, g / 255f, b / 255f, a / 255f);
}