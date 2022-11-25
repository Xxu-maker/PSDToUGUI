using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using SharpJson;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text;
using System.Linq;
public class PSD2UGUIEditor : EditorWindow
{
    private static GameObject root;
    private static string rootDir;
    private static List<UIFontAssets> fontAssets;
    private static StaticConfig mStaticConfig;
    public const string EXT_PUBLIC = "PUBLIC";
    public const string EXT_SLIDER = "SLIDER";
    /// <summary>
    /// 滚动视图pos
    /// </summary>
    Vector2 scrollPos = Vector2.zero;

    public Object[] folders = new Object[] { null };

    #region GUI
    void OnGUI()
    {
        //开始一个水平组,所有被渲染的控件，在这个组里一个接着一个被水平放置。该组必须调用EndHorizontal关闭。
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("批量导入所选择的文件夹", GUILayout.Height(30)))
        {
            folders = GerFolders();
        }

        if (GUILayout.Button("开始生成", GUILayout.Height(30)))
        {
            foreach (var folder in folders)
            {
                Generate(folder);
            }
        }
        //关闭水平组
        GUILayout.EndHorizontal();
        if (folders.Length > 0)
        {//开始滚动视图,scrollPos用于显示的滚动位置
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(Screen.width), GUILayout.Height(Screen.height - 200));
            foreach (var obj in folders)
            {
                EditorGUILayout.ObjectField(obj, typeof(Object), true);
            }
            //结束滚动视图
            GUILayout.EndScrollView();
        }
    }
    #endregion

    #region 相关方法

    private Object[] GerFolders()
    {
        Object[] mfolders = Selection.objects;
        
        return mfolders;
    }

    [MenuItem("Tools/PSD2UGUI")]
    public static void OpenWindow()
    {
        PSD2UGUIEditor window = (PSD2UGUIEditor)GetWindow(typeof(PSD2UGUIEditor),false,"PSD导入工具");
        window.Show();
    }
    public static void Generate(Object folder)
    {
        mStaticConfig = AssetDatabase.LoadAssetAtPath<StaticConfig>("Assets/StreamAssets/Config/StaticConfig.asset");
        fontAssets = mStaticConfig.fontAssets;
        //Object folder = Selection.activeObject;
        rootDir = AssetDatabase.GetAssetPath(folder);
        string jsonFile = getJsonData(rootDir);
        if (string.IsNullOrEmpty(jsonFile))
        {
            Debug.LogWarning("该文件夹不包含生成UI面板的json文件");
            return;
        }

        string json = File.ReadAllText(jsonFile);
        JsonDecoder decoder = new JsonDecoder();
        Dictionary<string, object> result = decoder.Decode(json) as Dictionary<string, object>;
        root = new GameObject(Path.GetFileName(rootDir));
        root.AddComponent<RectTransform>();
        root.transform.SetParent(getLastLayer(), false);
        root.transform.localScale = Vector3.one;
        root.transform.localPosition = Vector3.zero;
        root.transform.SetAsLastSibling();
        if (result.ContainsKey("children"))
        {
            parseChildren(result["children"], root);
        }
        root.layer =LayerMask.NameToLayer("UI");
        root = null;
        AssetDatabase.Refresh();
    }

    private static Transform getLastLayer()
    {
        GameObject canvas = GameObject.Find("UICanvas");
        if (canvas != null)
        {
            return canvas.transform;
        }

        return null;
    }

    private static string getJsonData(string path)
    {
        string[] jsons = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
        if (jsons.Length > 0)
        {
            return jsons[0];
        }

        return string.Empty;
    }

    /// 解析子对象列表
    private static void parseChildren(object children, GameObject parent = null)
    {
        List<object> nodes = children as List<object>;
        for (int i = nodes.Count - 1; i >= 0; --i)
        {
            parseNode(nodes[i], parent);
        }
    }

    private static void parseNode(object node, GameObject parent = null)
    {
        Dictionary<string, object> parse = node as Dictionary<string, object>;
        if (parse == null) return;
        GameObject child = null;
        switch (parse["type"].ToString())
        {
            case "LayerSet": // 容器
                child = parseLayerSet(parse, parent);
                break;
            case "ArtLayer": // 图片
                child = parseArtLayer(parse, parent);
                break;
            case "Label": // 文本
                child = parseLabel(parse, parent);
                break;
        }

        child.layer = LayerMask.NameToLayer("UI");
        List<object> children = parse["children"] as List<object>;
        if (children != null && children.Count > 0)
        {
            parseChildren(children, child);
        }
    }

    private static GameObject parseLayerSet(Dictionary<string, object> node, GameObject parent)
    {
        GameObject container = new GameObject(node["fileName"].ToString().Replace(" ", ""));
        var rectTrans = container.AddComponent<RectTransform>();
        var a=container.AddComponent<ContentSizeFitter>();
        a.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        a.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        container.transform.SetParent(parent.transform, false);
        //container.transform.position = root.transform.TransformPoint(new Vector3(float.Parse(node["x"].ToString()), float.Parse(node["y"].ToString()), 0));
        // container.transform.position = root.transform.TransformPoint(new Vector3(float.Parse(node["x"].ToString()),float.Parse(node["y"].ToString()),0));
        container.transform.position=Vector3.zero;
        container.transform.localScale = new Vector3(1, 1, 1);
        rectTrans.sizeDelta = new Vector2(float.Parse(node["width"].ToString()), float.Parse(node["height"].ToString()));
        
        float alpha = float.Parse(node["alpha"].ToString());
        if (alpha < 100f)
        {
            var group = container.AddComponent<CanvasGroup>();
            group.alpha = Mathf.Clamp01((float)alpha / 100f);
        }

        container.SetActive(node["visible"].ToString().Equals("1"));
        return container;
    }

    private static GameObject parseArtLayer(Dictionary<string, object> node, GameObject parent)
    {
        Image image = DefaultControls.CreateImage(new DefaultControls.Resources()).GetComponent<Image>();
        string name = node["name"].ToString().Replace(" ", "");
        //image.name = name/*.Split('_')[0]*/;
        image.name = node["fileName"].ToString().Replace(" ", "");
        string path = string.Empty;
        if (name.Contains(EXT_PUBLIC))
        {
            // 公共组件
            path = string.Format("{0}/{1}.png",mStaticConfig._PUBLIC_Path, image.name.TrimStart(' '),
                isPC ? "Game_PC" : "Game");
            if (!File.Exists(path))
            {
                Debug.LogWarningFormat("该对象引用的公共组件不存在：{0} 查找位置{1}", getParentPath(parent, image.name), path);
            }
        }
        else
        {
            var curname = node["fileName"].ToString().TrimStart(' ');
            path = string.Format("{0}/{1}.png", rootDir, curname);
            // TODO 如果是slider，则自动设置该图的border
            setspriteborder(path, name);
        }

        image.sprite = loasSprite(path);
        if (name.Contains(EXT_SLIDER))
        {
            image.type = Image.Type.Sliced;
        }

        var a=image.gameObject.AddComponent<ContentSizeFitter>();
        a.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        a.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        image.transform.SetParent(parent.transform);
        image.transform.position = root.transform.TransformPoint(new Vector3(float.Parse(node["x"].ToString()),
            float.Parse(node["y"].ToString()), 0));
        image.transform.localScale = new Vector3(1, 1, 1);
        
        image.rectTransform.sizeDelta =
            new Vector2(float.Parse(node["width"].ToString()), float.Parse(node["height"].ToString()));
        float alpha = float.Parse(node["alpha"].ToString());
        if (alpha < 100f)
        {
            Color c = image.color;
            c.a = Mathf.Clamp01((float)alpha / 100f);
            image.color = c;
        }

        image.gameObject.SetActive(node["visible"].ToString().Equals("1"));
        return image.gameObject;
    }

    private static GameObject parseLabel(Dictionary<string, object> node, GameObject parent)
    {
        Font FZY3JW = mStaticConfig._DefaultFont_1;
        Font character = mStaticConfig._DefaultFont_2;

        Text text = DefaultControls.CreateText(new DefaultControls.Resources()).GetComponent<Text>();
        var a=text.gameObject.AddComponent<ContentSizeFitter>();
        a.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        a.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        text.transform.SetParent(parent.transform);
        text.transform.position = root.transform.TransformPoint(new Vector3(float.Parse(node["x"].ToString()),
            float.Parse(node["y"].ToString()), 0));
        text.transform.localScale = new Vector3(1, 1, 1);
        text.rectTransform.sizeDelta =
            new Vector2(float.Parse(node["width"].ToString()), float.Parse(node["height"].ToString()));

        if (node.ContainsKey("text") && node["text"].ToString() != "undefined")
        {
            text.text = node["text"].ToString();
        }
        else
        {
            text.text = node["name"].ToString();
        }
        if (node["shadow"].ToString() != "")
        {
            var shadow = text.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = text.gameObject.AddComponent<Shadow>();
            }
            Color shadowColor = Color.white;
            ColorUtility.TryParseHtmlString("#" + node["shadow"].ToString(), out shadowColor);
            shadow.effectColor = shadowColor;
        }
        if (node["outline"].ToString() != "")
        {
            var outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }
            Color outlineColor = Color.white;
            ColorUtility.TryParseHtmlString("#" + node["outline"].ToString(), out outlineColor);
            outline.effectColor = outlineColor;
        }
        text.alignment = TextAnchor.MiddleCenter;
        text.font = GetMatchFont(node["font"].ToString());
        text.fontSize = Mathf.FloorToInt(float.Parse(node["size"].ToString()));
        Color c = Color.white;
        if (node.ContainsKey("color") && node["color"].ToString() != "undefined")
        {
            ColorUtility.TryParseHtmlString("#" + node["color"].ToString(), out c);
        }
        else
        {
            ColorUtility.TryParseHtmlString("#ff00cc", out c);
            Debug.LogWarning("字体颜色获取失败，请查看psd手动获取" + getParentPath(text.gameObject, text.name));
        }

        float alpha = float.Parse(node["alpha"].ToString());
        if (alpha < 100f)
        {
            c.a = Mathf.Clamp01((float)alpha / 100f);
        }

        text.color = c;
        text.gameObject.SetActive(node["visible"].ToString().Equals("1"));

        // adjust size
        Vector2 sizeDelta = text.rectTransform.sizeDelta;
        if (sizeDelta.x < text.preferredWidth)
        {
            sizeDelta.x = text.preferredWidth;
        }

        if (sizeDelta.y < text.preferredHeight)
        {
            sizeDelta.y = text.preferredHeight;
        }

        text.rectTransform.sizeDelta = sizeDelta;
        return text.gameObject;
    }

    /// 是否包含中文文本
    private static bool isChinese(string text)
    {
        if (!string.IsNullOrEmpty(text) && System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u4e00-\u9fa5]"))
        {
            return true;
        }

        return false;
    }

    private static string getParentPath(GameObject parent, string path)
    {
        if (parent != null)
        {
            path = path.Insert(0, parent.name + "/");
            if (parent.transform.parent != null)
            {
                return getParentPath(parent.transform.parent.gameObject, path);
            }
        }

        return path;
    }

    private static Sprite loasSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static bool isPC
    {
        get
        {
#if UNITY_EDITOR
            return Handles.GetMainGameViewSize().x > Handles.GetMainGameViewSize().y;
#else
            return Screen.width > Screen.height;
#endif
        }
    }

    private static void setspriteborder(string path, string imageName)
    {
#if UNITY_EDITOR
        if (imageName.Contains(EXT_SLIDER))
        {
            string[] images = imageName.Split('_');
            int w = 0;
            int h = 0;
            int left = 0;
            int top = 0;
            int right = 0;
            int bottom = 0;
            for (int i = 0; i < images.Length; ++i)
            {
                if (images[i].Contains(EXT_SLIDER))
                {
                    string[] args = images[i].Replace(EXT_SLIDER, "").Replace("(", "").Replace(")", "").Split(',');
                    if (args.Length == 3)
                    {
                        int.TryParse(args[0], out w);
                        int.TryParse(args[1], out h);
                        int.TryParse(args[2], out left);
                        int.TryParse(args[2], out top);
                        int.TryParse(args[2], out right);
                        int.TryParse(args[2], out bottom);
                    }
                    else if (args.Length == 6)
                    {
                        int.TryParse(args[0], out w);
                        int.TryParse(args[1], out h);
                        int.TryParse(args[2], out left);
                        int.TryParse(args[3], out top);
                        int.TryParse(args[4], out right);
                        int.TryParse(args[5], out bottom);
                    }

                    break;
                }
            }

            if (w != 0)
            {
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null)
                {
                    ti.spriteBorder = new Vector4(left, bottom, right, top);
                    AssetDatabase.ImportAsset(path);
                }
                else
                {
                    Debug.LogWarning("setspriteborder failed:" + path);
                }
            }
        }
#endif
    }

    /// <summary>
    /// 获取当前两个字体库名字匹配相似度
    /// 使用最接近的作为目标字体
    /// </summary>
    /// <param name="pFontName">json中存取的字体名字</param>
    /// <returns></returns>
    private static Font GetMatchFont(string pFontName)
    {
        //匹配字符数量
        int MatchCounter = 0;
        Font font = mStaticConfig._DefaultFont_1;
        foreach (var fontAssets in fontAssets)
        {
            var aWords = pFontName.ToCharArray();
            var bWords = fontAssets.name.ToCharArray();
            var tempCount = bWords.Count(x => aWords.Contains(x));
            if (MatchCounter <= tempCount)
            {
                font = fontAssets.font;
                MatchCounter = tempCount;
            }
        }
        return font;
    }
    #endregion
}