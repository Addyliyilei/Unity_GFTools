using GameFramework;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityToolbarExtender;

namespace GFTools.LocalizationEditor
{
    [UnityEditor.InitializeOnLoad]
    public static class CustomToolBars
    {
        private static GUIContent switchSceneBtContent;

        //Toolbar栏工具箱下拉列表
        private static List<string> sceneAssetList;
   
        static CustomToolBars()
        {
            sceneAssetList = new List<string>();
            var curOpenSceneName = EditorSceneManager.GetActiveScene().name;
            switchSceneBtContent = EditorGUIUtility.TrTextContentWithIcon(string.IsNullOrEmpty(curOpenSceneName) ? "Switch Scene" : curOpenSceneName, "切换场景", "UnityLogo");
            EditorSceneManager.sceneOpened += OnSceneOpened;

            ToolbarCallback.OnToolbarGUILeft += OnToolbarGUILeft;
            ToolbarCallback.OnToolbarGUIRight += OnToolbarGUIRight;
        }


        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            switchSceneBtContent.text = scene.name;
        }
        private static void OnToolbarGUILeft()
        {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
            rect.x = (rect.width - 150); // 居中位置
            rect.width = 150;

            if (GUI.Button(rect, switchSceneBtContent, EditorStyles.toolbarPopup))
            {
                DrawSwithSceneDropdownMenus();
            }
        }
        private static void OnToolbarGUIRight()
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("多语言编辑器", EditorStyles.toolbarButton, GUILayout.MaxWidth(150)))
            {
                LocalizationEditor.ShowWindow();
            }
        }

        static void DrawSwithSceneDropdownMenus()
        {
            GenericMenu popMenu = new GenericMenu();
            popMenu.allowDuplicateNames = true;
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new string[] { ConstEditor.ScenePath });
            sceneAssetList.Clear();
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                sceneAssetList.Add(scenePath);
                string fileDir = System.IO.Path.GetDirectoryName(scenePath);
                bool isInRootDir = Utility.Path.GetRegularPath(ConstEditor.ScenePath).TrimEnd('/') == Utility.Path.GetRegularPath(fileDir).TrimEnd('/');
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                string displayName = sceneName;
                if (!isInRootDir)
                {
                    var sceneDir = System.IO.Path.GetRelativePath(ConstEditor.ScenePath, fileDir);
                    displayName = $"{sceneDir}/{sceneName}";
                }

                popMenu.AddItem(new GUIContent(displayName), false, menuIdx => { SwitchScene((int)menuIdx); }, i);
            }
            popMenu.ShowAsContext();
        }
        private static void SwitchScene(int menuIdx)
        {
            if (menuIdx >= 0 && menuIdx < sceneAssetList.Count)
            {
                var scenePath = sceneAssetList[menuIdx];
                var curScene = EditorSceneManager.GetActiveScene();
                if (curScene != null && curScene.isDirty)
                {
                    int opIndex = EditorUtility.DisplayDialogComplex("警告", $"当前场景{curScene.name}未保存,是否保存?", "保存", "取消", "不保存");
                    switch (opIndex)
                    {
                        case 0:
                            if (!EditorSceneManager.SaveOpenScenes())
                            {
                                return;
                            }
                            break;
                        case 1:
                            return;
                    }
                }
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }


    }

}