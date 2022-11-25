using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StaticConfig.asset", menuName = "Config/StaticConfig")]
public class StaticConfig : ScriptableObject
{
    public List<UIFontAssets> fontAssets;
    public Font _DefaultFont_1;
    public Font _DefaultFont_2;
    public string _PUBLIC_Path;
}
[System.Serializable]
public class UIFontAssets
{
    public string name;
    public Font font;
}
