//  PathAttributeDrawer.cs
//  http://kan-kikuchi.hatenablog.com/entry/PathAttribute
//
//  Created by kan.kikuchi on 2016.08.05.

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// PathAttribute��Inspector�łǂ��\������邩��ݒ肷��N���X
/// </summary>
[CustomPropertyDrawer(typeof(FilePathAttribute))]
public class FilePathAttributeDrawer : PropertyDrawer
{

    //=================================================================================
    //�X�V
    //=================================================================================

    //GUI���X�V����
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //D&D�o����GUI���쐬�A �h���b�v���ꂽ�I�u�W�F�N�g�̃��X�g���擾
        List<Object> dropObjectList = CreateDragAndDropGUI(position);

        var prop = showStringPath_() ?? showPathUitPath_();
        if (prop == default) return;

        //���ݐݒ肳��Ă���p�X��\��
        //GUI.Label(position, property.displayName + " : " + pathtext);
        var poslabel = position;
        poslabel.width = position.width * 0.3f;
        GUI.Label(poslabel, property.displayName);
        var postext = position;
        postext.x += position.width * 0.3f;
        postext.width = position.width - poslabel.width - 2;
        prop.stringValue = GUI.TextField(postext, prop.stringValue);
        return;


        SerializedProperty showStringPath_()
        {
            if (property.propertyType != SerializedPropertyType.String) return default;

            //�I�u�W�F�N�g���h���b�v���ꂽ��p�X��ݒ�
            if (dropObjectList.Count > 0)
            {
                property.stringValue =
                    AssetDatabase.GetAssetPath(dropObjectList[0]).Replace("Assets/", "");
            }

            return property;
        }

        SerializedProperty showPathUitPath_()
        {
            if (property.type != "PathUnit") return default;

            var prop = property.FindPropertyRelative("Value");

            //�I�u�W�F�N�g���h���b�v���ꂽ��p�X��ݒ�
            if (dropObjectList.Count > 0)
            {
                prop.stringValue =
                    AssetDatabase.GetAssetPath(dropObjectList[0]).Replace("Assets/", "");
            }

            return prop;
        }
    }

    //D&D��GUI���쐬
    private List<Object> CreateDragAndDropGUI(Rect rect)
    {
        List<Object> list = new List<Object>();

        //D&D�o����ꏊ��`��
        GUI.Box(rect, "");

        //�}�E�X�̈ʒu��D&D�͈̔͂ɂȂ���΃X���[
        if (!rect.Contains(Event.current.mousePosition))
        {
            return list;
        }

        //���݂̃C�x���g���擾
        EventType eventType = Event.current.type;

        //�h���b�O���h���b�v�ő��삪 �X�V���ꂽ�Ƃ� or ���s�����Ƃ�
        if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform)
        {
            //�J�[�\����+�̃A�C�R����\��
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            //�h���b�v���ꂽ�I�u�W�F�N�g�����X�g�ɓo�^
            if (eventType == EventType.DragPerform)
            {
                list = new List<Object>(DragAndDrop.objectReferences);

                //�h���b�O���󂯕t����(�h���b�O���ăJ�[�\���ɂ����t���Ă��I�u�W�F�N�g���߂�Ȃ��Ȃ�)
                DragAndDrop.AcceptDrag();
            }

            //�C�x���g���g�p�ς݂ɂ���
            Event.current.Use();
        }

        return list;
    }

}