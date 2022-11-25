﻿using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace PSDUIImporter
{
    //arguments[]: color,font,fontSize,text,alignment
    public class TextImport : IImageImport
    {
        public void DrawImage(PSImage image, GameObject parent, GameObject ownObj = null)
        {
            UnityEngine.UI.Text myText;
            if (ownObj != null)
                myText = ownObj.AddMissingComponent<UnityEngine.UI.Text>();
            else
                myText = PSDImportUtility.LoadAndInstant<UnityEngine.UI.Text>(PSDImporterConst.ASSET_PATH_TEXT, image.name, parent);

            RectTransform rectTransform = myText.GetComponent<RectTransform>();
            rectTransform.SetAnchorMiddleCenter();

            //UnityEngine.UI.Text myText = PSDImportUtility.LoadAndInstant<Text>(PSDImporterConst.ASSET_PATH_TEXT, image.name, parent);
            //                        myText.color = image.arguments[0];
            //                        myText.font = image.arguments[1];
            Debug.Log("Label Color : " + image.arguments[0]);
            Debug.Log("fontSize : " + image.arguments[2]);

            Color color;
            if (UnityEngine.ColorUtility.TryParseHtmlString(("#" + image.arguments[0]), out color))
            {
                if (image.opacity > -1)
                {
                    color.a = image.opacity / 100f;
                    
                    Debug.Log("Opacity:" + color.a);
                }
                
                color.r = (float)Math.Pow((color.r ), 0.45) ;
                color.g = (float)Math.Pow((color.g ), 0.45) ;
                color.b = (float)Math.Pow((color.b ), 0.45) ;

                myText.color = color;
            }
            else
            {
                Debug.Log(image.arguments[0]);
            }

            float size;
            if (float.TryParse(image.arguments[2], out size))
            {
                myText.fontSize = (int) Math.Round(size);
            }

            myText.text = image.arguments[3];

            //设置字体,注意unity中的字体名需要和导出的xml中的一致
            string fontFolder;
            
            if(image.arguments[1].ToLower().Contains("static"))
            {
                fontFolder = PSDImporterConst.FONT_STATIC_FOLDER;
            }
            else
            {
                fontFolder = PSDImporterConst.FONT_FOLDER; 
            }
            string fontFullName = fontFolder + image.arguments[1] + PSDImporterConst.FONT_SUFIX;
            Debug.Log("font name ; " + fontFullName);
            var font = AssetDatabase.LoadAssetAtPath(fontFullName, typeof(Font)) as Font;
            if (font == null)
            {
                Debug.LogWarning("Load font failed : " + fontFullName);
            }
            else
            {
                myText.font = font;
            }
            //ps的size在unity里面太小，文本会显示不出来,暂时选择溢出
            myText.verticalOverflow = VerticalWrapMode.Overflow;
            myText.horizontalOverflow = HorizontalWrapMode.Overflow;
            //设置对齐
            if (image.arguments.Length >= 5)
                myText.alignment = ParseAlignmentPS2UGUI(image.arguments[4]);
            else
            {
                // 默认居中
                myText.alignment = TextAnchor.MiddleLeft;
            }

            // OutLine
            if (!string.IsNullOrEmpty(image.outline))
            {
                Debug.LogError(image.outline);

                var _temp = image.outline.Split('|');

                // 第一位颜色
                Color effectColor;

                Outline _outline = ownObj.AddMissingComponent<Outline>();

                if (UnityEngine.ColorUtility.TryParseHtmlString(("#" + _temp[0]), out effectColor))
                {
                    _outline.effectColor = effectColor;
                }
                // 第二位

                // 第三位

            }

            rectTransform.sizeDelta = new Vector2(image.size.width, image.size.height);
            rectTransform.anchoredPosition = new Vector2(image.position.x, image.position.y);
        }

        /// <summary>
        /// ps的对齐转换到ugui，暂时只做水平的对齐
        /// </summary>
        /// <param name="justification"></param>
        /// <returns></returns>
        public TextAnchor ParseAlignmentPS2UGUI(string justification)
        {
            var defaut = TextAnchor.MiddleCenter;
            if (string.IsNullOrEmpty(justification))
            {
                return defaut;
            }

            string[] temp = justification.Split('.');
            if (temp.Length != 2)
            {
                Debug.LogWarning("ps exported justification is error !");
                return defaut;
            }
            Justification justi = (Justification)System.Enum.Parse(typeof(Justification), temp[1]);
            int index = (int)justi;
            defaut = (TextAnchor)System.Enum.ToObject(typeof(TextAnchor), index);;

            return defaut;
        }

        //ps的对齐方式
        public enum Justification
        {       
            CENTERJUSTIFIED = 0,
            LEFTJUSTIFIED = 1,
            RIGHTJUSTIFIED = 2,          
            LEFT = 3,
            CENTER = 4,
            RIGHT = 5,
            FULLYJUSTIFIED = 6,
        }
    }
}
