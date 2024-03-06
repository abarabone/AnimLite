//  PathAttributeDrawer.cs
//  http://kan-kikuchi.hatenablog.com/entry/PathAttribute
//
//  Created by kan.kikuchi on 2016.08.05.

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// PathAttributeがInspectorでどう表示されるかを設定するクラス
/// </summary>
[CustomPropertyDrawer(typeof(FilePathAttribute))]
public class FilePathAttributeDrawer : PropertyDrawer
{

    //=================================================================================
    //更新
    //=================================================================================

    //GUIを更新する
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //string以外に設定されている場合はスルー
        if (property.propertyType != SerializedPropertyType.String)
        {
            return;
        }

        //D&D出来るGUIを作成、 ドロップされたオブジェクトのリストを取得
        List<Object> dropObjectList = CreateDragAndDropGUI(position);

        //オブジェクトがドロップされたらパスを設定
        if (dropObjectList.Count > 0)
        {
            property.stringValue = AssetDatabase.GetAssetPath(dropObjectList[0]);
        }

        //現在設定されているパスを表示
        GUI.Label(position, property.displayName + " : " + property.stringValue);
    }

    //D&DのGUIを作成
    private List<Object> CreateDragAndDropGUI(Rect rect)
    {
        List<Object> list = new List<Object>();

        //D&D出来る場所を描画
        GUI.Box(rect, "");

        //マウスの位置がD&Dの範囲になければスルー
        if (!rect.Contains(Event.current.mousePosition))
        {
            return list;
        }

        //現在のイベントを取得
        EventType eventType = Event.current.type;

        //ドラッグ＆ドロップで操作が 更新されたとき or 実行したとき
        if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform)
        {
            //カーソルに+のアイコンを表示
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            //ドロップされたオブジェクトをリストに登録
            if (eventType == EventType.DragPerform)
            {
                list = new List<Object>(DragAndDrop.objectReferences);

                //ドラッグを受け付ける(ドラッグしてカーソルにくっ付いてたオブジェクトが戻らなくなる)
                DragAndDrop.AcceptDrag();
            }

            //イベントを使用済みにする
            Event.current.Use();
        }

        return list;
    }

}