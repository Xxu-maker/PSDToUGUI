﻿using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace PSDUIImporter
{
    //只有一半的图片的生成类，需要组合拼接
    public class HalfSpriteImport : IImageImport
    {
        public void DrawImage(PSImage image, GameObject parent, GameObject ownObj = null)
        {
            RectTransform halfRectTrans = parent.GetComponent<RectTransform>();

            PSDImportUtility.SetAnchorMiddleCenter(halfRectTrans);
            halfRectTrans.sizeDelta = new Vector2(image.size.width, image.size.height);
            halfRectTrans.anchoredPosition = new Vector2(image.position.x, image.position.y);

            UnityEngine.UI.Image leftOrUpSprite;
            if (ownObj != null)
                leftOrUpSprite = ownObj.AddMissingComponent<UnityEngine.UI.Image>();
            else
                leftOrUpSprite = PSDImportUtility.LoadAndInstant<UnityEngine.UI.Image>(PSDImporterConst.ASSET_PATH_IMAGE, image.name, halfRectTrans.gameObject);

            //string assetPath = "";
            //if (image.imageSource == ImageSource.Common || image.imageSource == ImageSource.Custom)
            //{
            //    assetPath = PSDImportUtility.baseDirectory + image.name + PSDImporterConst.PNG_SUFFIX;
            //}
            //else
            //{
            //    assetPath = PSDImporterConst.Globle_BASE_FOLDER + image.name.Replace(".", "/") + PSDImporterConst.PNG_SUFFIX;
            //}

            //Sprite sprite = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite)) as Sprite;
            Sprite sprite = image.LoadAssetAtPath<Sprite>() as Sprite;
//             if (sprite == null)
//             {
//                 Debug.Log("loading asset at path: " + assetPath);
//             }

            leftOrUpSprite.sprite = sprite;
            RectTransform lOrURectTrans = leftOrUpSprite.GetComponent<RectTransform>();
            lOrURectTrans.anchoredPosition = new Vector2(image.position.x, image.position.y);

            //添加镜像组件
            var mirror = lOrURectTrans.gameObject.AddComponent<UGUI.Effects.Mirror>();
            if (image.imageType == ImageType.BottomHalfImage)
            {
                mirror.mirrorType = UGUI.Effects.Mirror.MirrorType.Vertical;
            }
            else if (image.imageType == ImageType.LeftHalfImage)
            {
                mirror.mirrorType = UGUI.Effects.Mirror.MirrorType.Horizontal;                
            }
            else if (image.imageType == ImageType.QuarterImage)
            {
                mirror.mirrorType = UGUI.Effects.Mirror.MirrorType.Quarter;
            }
            mirror.SetNativeSize();
            
        }
    }
}
