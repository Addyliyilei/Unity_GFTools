
using GameFramework.Localization;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Language = GameFramework.Localization.Language;

namespace GFTools.LocalizationEditor
{
    [System.Serializable]
    public class LocalizationEntry
    {
        public string Key;
        public string Value;
        /// <summary>
        /// 本地化词条对象
        /// </summary>
        /// <param name="key"> 键 </param>
        /// <param name="value"> 值 </param>
        public LocalizationEntry(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public LocalizationEntry()
        {

        }
    }

    public enum FileType
    {
        Xml,
        Json,
    }
   

    public class LocalizationEditor : EditorWindow
    {
        public const float WindowWidth = 700;
        public const float WindowHeight = 720;
        /// <summary>
        /// 文件夹地址
        /// </summary>
        private string folderPath = "Assets/GameMain/Localization/";

        private string m_FilePath = "English";

        private FileType m_FileType = FileType.Xml;
        /// <summary>
        /// 语言类型列表
        /// </summary>
        private List<string> m_LanguageTypeList;
        private ReorderableList reorderableLanguageList;
        private string m_CurLanguage;
        public string CurLanguage
        {
            get => m_CurLanguage;
            set 
            {
                if(m_CurLanguage != value)
                {
                    m_CurLanguage = value;
                   // Debug.Log("Selected: " + value);
                }
            }
        }

        /// <summary>
        /// 允许拖拽排序
        /// </summary>
        public bool AllowDrag 
        { 
            get => EditorPrefs.GetBool("AllowDrag", false);
            set => EditorPrefs.SetBool("AllowDrag", value);
        
        }
        /// <summary>
        /// 关闭时保存
        /// </summary>
        public bool SaveWhenClosed 
        { 
            get => EditorPrefs.GetBool("SaveWhenClosed", true);
            set => EditorPrefs.SetBool("SaveWhenClosed", value);
        }
        public int SelectLanguageIndex 
        { 
            get
            {
                m_selectLanguageIndex = EditorPrefs.GetInt("SelectLanguageIndex", 0);
                return m_selectLanguageIndex;
            }

            set
            {
                m_selectLanguageIndex = value;
                EditorPrefs.SetInt("SelectLanguageIndex", m_selectLanguageIndex);
            }
        }
        /// <summary>
        /// 当前选择的语言索引
        /// </summary>
        private int m_selectLanguageIndex = 0;
        /// <summary>
        /// 键值对
        /// </summary>
        private Dictionary<string, string> m_KeyValuePairs;
        /// <summary>
        /// 键值对条目列表 便于修改
        /// </summary>
        private List<LocalizationEntry> m_CurEntryList = new List<LocalizationEntry>();
        /// <summary>
        /// 存储已添加语言的本地化条目
        /// </summary>
        private Dictionary<string, List<LocalizationEntry>> m_AllLangguageLocalitionEntry = new Dictionary<string, List<LocalizationEntry>>();
        private ReorderableList m_ReorderableEntryList;
     
        private GUIStyle _evenRowStyle;  //偶数行
        private GUIStyle _oddRowStyle;   //奇数行

        private bool m_SaveWhenClosed = true;
        private bool m_AllowDrag = false;

        private Vector2 m_EntryScrollPos;
        private Vector2 m_LanguageListScrollPos;

        [MenuItem("Game Framework/多语言编辑器")]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalizationEditor>("多语言编辑器");
            window.minSize = new Vector2(WindowWidth, WindowHeight); // 可选：设置最小尺寸
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            // 计算居中位置
            float posX = main.x + (main.width - WindowWidth) / 2;
            float posY = main.y + (main.height - WindowHeight) / 2;

            window.position = new Rect(posX, posY, WindowWidth, WindowHeight);
            string iconPath = LocalizationUtility.GetIconPath(window);

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            window.titleContent = new GUIContent(" 本地化编辑器", icon, "Localization Editor");
        }

        private void OnEnable()
        {
            LoadLanguageTypeList();
            for (int i = 0; i < m_LanguageTypeList.Count; i++)
            {
               var localEntry = LoadLocalizationEntry(m_LanguageTypeList[i]);
                if (localEntry != null && i == 0)
                {
                    m_CurEntryList = localEntry;
                }
             
            }
            InitReorderableEntryList();
           
        }

        private void OnDisable()
        {
            Debug.Log("关闭");
            if(SaveWhenClosed)
                SaveAll();
        }

        private void OnGUI()
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.green }
            };
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("多语言文件夹路径(?)", "eg:Assets/AAAGame/Localization/"), labelStyle, GUILayout.Width(200));
                folderPath = EditorGUILayout.TextField(folderPath);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("文件存储路径(?)", "eg:ChineseSimplified"), labelStyle, GUILayout.Width(200));
                m_FilePath = EditorGUILayout.TextField(m_FilePath);
                m_FileType = (FileType)EditorGUILayout.EnumPopup(m_FileType, GUILayout.Width(100));
            }
            EditorGUILayout.EndHorizontal();

            DrawLanguagePopup();
            EditorGUILayout.Space();
            DrawLocalizationEntry();
            EditorGUILayout.Space();
            DrawLanguageTypeList();

            DrawBottomButton();
        }

        private bool otherFoldout = false;
        /// <summary>
        /// 绘制底部按钮
        /// </summary>
        private void DrawBottomButton()
        {

            // 2. 占据剩余空间，将按钮推到底部
            GUILayout.FlexibleSpace();

            // 3. 固定在底部的保存按钮区域
            EditorGUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();

          
            if (GUILayout.Button("保存当前", GUILayout.Width(100), GUILayout.Height(30)))
            {
                SaveToFile();
            }
            if (GUILayout.Button("保存全部", GUILayout.Width(100), GUILayout.Height(30)))
            {
                SaveAll();
            }
            if (GUILayout.Button(new GUIContent("同步所有关键词(?)", "将当前关键词列表应用到所有语言文件"), GUILayout.Width(120), GUILayout.Height(30)))
            {
                SyncAllKeycodes();
            }

            GUILayout.FlexibleSpace();

 
            EditorGUILayout.EndHorizontal();

            otherFoldout = EditorGUILayout.Foldout(otherFoldout, new GUIContent("其他设置"));

            EditorGUILayout.BeginHorizontal("box");
            if(otherFoldout)
            {
                SaveWhenClosed = EditorGUILayout.Toggle(new GUIContent("关闭时保存："),SaveWhenClosed);
                AllowDrag = EditorGUILayout.Toggle(new GUIContent("允许拖拽排序："), AllowDrag);
                m_ReorderableEntryList.draggable = AllowDrag;
            }
         

            EditorGUILayout.EndHorizontal();
        }
        /// <summary>
        /// 绘制语言选择下拉框
        /// </summary>
        private void DrawLanguagePopup()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("选择语言：", EditorStyles.boldLabel);
            int newIndex = EditorGUILayout.Popup(SelectLanguageIndex, m_LanguageTypeList.ToArray(), GUILayout.Width(300f));

            if(newIndex !=  SelectLanguageIndex)
            {
                SelectLanguageIndex = newIndex;
                OnPopupLanguageChange(SelectLanguageIndex);
            }

            EditorGUILayout.EndHorizontal();
        }
        /// <summary>
        /// 绘制词条列表
        /// </summary>
        private void DrawLocalizationEntry()
        {
            if (m_ReorderableEntryList != null)
            {
                EditorGUILayout.LabelField("本地化词条列表", EditorStyles.boldLabel);

                m_EntryScrollPos = EditorGUILayout.BeginScrollView(m_EntryScrollPos, GUILayout.Height(380f));
                m_ReorderableEntryList.DoLayoutList();
                EditorGUILayout.EndScrollView();
            }
        }
        /// <summary>
        /// 绘制语言类型列表
        /// </summary>
        private void DrawLanguageTypeList()
        {
         
            if (reorderableLanguageList != null)
            {              
                m_LanguageListScrollPos = EditorGUILayout.BeginScrollView(m_LanguageListScrollPos, GUILayout.Height(140));
                reorderableLanguageList.DoLayoutList();
                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 加载指定语言的本地化词条
        /// </summary>
        /// <param name="language"></param>
        private List<LocalizationEntry> LoadLocalizationEntry(string language)
        {
            Dictionary<string,string> keyValuePairs = new Dictionary<string, string>();
            List<LocalizationEntry> localizationEntryList = new List<LocalizationEntry>();
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogWarning("路径为空，无法加载文件。");
                return null;
            }
            string fullPath = GetCurrentLanguageFilePath(language);
            if(!File.Exists(fullPath))
            {
                Debug.LogError(fullPath + " is not exists!");
            }

            localizationEntryList = ReadLocalizationEntryList(m_FileType, fullPath);

            m_AllLangguageLocalitionEntry.Add(language, localizationEntryList);

            return localizationEntryList;
        }

        /// <summary>
        /// 初始化可重排序的本地化词条列表
        /// </summary>
        private void InitReorderableEntryList()
        {
            m_ReorderableEntryList = new ReorderableList(m_CurEntryList, typeof(LocalizationEntry), true, true, true, true);
            m_ReorderableEntryList.drawHeaderCallback = (Rect rect) =>
            {
                float padding = 4f;
                float labelWidth = 40f;
                float keyWidth = (rect.width - labelWidth - padding * 3) * 0.4f;
                float valueWidth = (rect.width - labelWidth - padding * 3) * 0.6f;

                float x = rect.x + padding;
                float y = rect.y;

                GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(4, 4, 2, 2)
                };

                EditorGUI.LabelField(new Rect(x, y, labelWidth - padding, EditorGUIUtility.singleLineHeight), "序号",headerStyle);
                x += labelWidth;

                EditorGUI.LabelField(new Rect(x + padding, y, keyWidth, EditorGUIUtility.singleLineHeight), "Key", headerStyle);
                x += keyWidth + padding;

                EditorGUI.LabelField(new Rect(x + padding, y, valueWidth, EditorGUIUtility.singleLineHeight), "Value", headerStyle);
            };


            m_ReorderableEntryList.draggable = AllowDrag;
            m_ReorderableEntryList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var entry = m_CurEntryList[index];

                float padding = 4f;
                float labelWidth = 40f;
                float keyWidth = (rect.width - labelWidth - padding * 3) * 0.4f;
                float valueWidth = (rect.width - labelWidth - padding * 3) * 0.6f;

                float y = rect.y + 2;

                // 显示序号
                EditorGUI.LabelField(
                    new Rect(rect.x + padding, y, labelWidth - padding, EditorGUIUtility.singleLineHeight),
                    (index + 1).ToString(),
                    EditorStyles.miniButtonLeft
                );

                // 输入 Key
                entry.Key = EditorGUI.TextField(
                    new Rect(rect.x + labelWidth + padding, y, keyWidth, EditorGUIUtility.singleLineHeight),
                    entry.Key
                );

                // 输入 Value
                entry.Value = EditorGUI.TextField(
                    new Rect(rect.x + labelWidth + keyWidth + padding * 2, y, valueWidth, EditorGUIUtility.singleLineHeight),
                    entry.Value
                );
            };


            m_ReorderableEntryList.onAddCallback = OnAddEntryCallback;

            m_ReorderableEntryList.onRemoveCallback = OnRemoveCallBack;

        }

        /// <summary>
        /// 初始化样式
        /// </summary>
        private void InitLanguageLabelStyles()
        {
            if (_evenRowStyle == null)
            {
                _evenRowStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 12,
                    padding = new RectOffset(8, 8, 2, 2),
                };
                _evenRowStyle.normal.textColor = Color.white;
                _evenRowStyle.normal.background = LocalizationUtility.MakeTex(1, 1, new Color(0.3f, 0.3f, 0.3f));
            }

            if (_oddRowStyle == null)
            {
                _oddRowStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 12,
                    padding = new RectOffset(8, 8, 2, 2),
                };
                _oddRowStyle.normal.textColor = Color.white;
                _oddRowStyle.normal.background = LocalizationUtility.MakeTex(1, 1, new Color(0.2f, 0.2f, 0.2f));
            }
        }
        /// <summary>
        /// 添加本地化词条回调
        /// </summary>
        /// <param name="list"></param>
        private void OnAddEntryCallback(ReorderableList list)
        {
            for (int i = 0; i < m_AllLangguageLocalitionEntry.Count; i++)
            {
                string lang = m_LanguageTypeList[i];
                var entryList = m_AllLangguageLocalitionEntry[m_LanguageTypeList[i]];
                // 注册撤销点
                Undo.RecordObject(this, $"Add Entry to {lang}");
                entryList.Add(new LocalizationEntry("NewKey", "NewValue"));

                EditorUtility.SetDirty(this);
            }
        }
        /// <summary>
        /// 移除本地化词条回调
        /// </summary>
        /// <param name="list"></param>
        private void OnRemoveCallBack(ReorderableList list)
        {
            int index = m_CurEntryList.Count - 1;
            if (list.index >= 0 && list.index < m_CurEntryList.Count)
            {
                index = list.index;
            }

            for (int i = 0; i < m_AllLangguageLocalitionEntry.Count; i++)
            {
                string lang = m_LanguageTypeList[i];
                var entryList = m_AllLangguageLocalitionEntry[m_LanguageTypeList[i]];
                // 注册撤销点
                Undo.RecordObject(this, $"Remove Entry from {lang}");
                entryList.RemoveAt(index);
                EditorUtility.SetDirty(this);
            }

        }

        /// <summary>
        /// 读取语言类型列表、并初始化可重排序列表
        /// </summary>
        private void LoadLanguageTypeList()
        {
            if (m_LanguageTypeList == null)
                m_LanguageTypeList = new List<string>();

            m_LanguageTypeList = LocalizationUtility.GetFileNames(folderPath);
    
            reorderableLanguageList = new ReorderableList(m_LanguageTypeList, typeof(string), true, true, true, true);
            reorderableLanguageList.draggable = false;
            reorderableLanguageList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "语言类型列表",EditorStyles.boldLabel);
            };


            reorderableLanguageList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                InitLanguageLabelStyles(); // 确保样式已初始化

                var style = index % 2 == 0 ? _evenRowStyle : _oddRowStyle; ;
                EditorGUI.LabelField(
                     new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight),
                     m_LanguageTypeList[index],
                     style
                 );
            };

            reorderableLanguageList.onAddCallback = (ReorderableList list) =>
            {
                GenericMenu menu = new GenericMenu();
                foreach (var lang in System.Enum.GetValues(typeof(Language)))
                {
                    string langStr = lang.ToString();
                    if(!m_LanguageTypeList.Contains(langStr))
                    {
                        menu.AddItem(new GUIContent(langStr), false, () =>
                        {
                            m_LanguageTypeList.Add(langStr);
                            m_AllLangguageLocalitionEntry.Add(langStr, AddNewLocalizationEntryList());

                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent(langStr));
                    }
                }

                // 显示下拉菜单
                menu.ShowAsContext();

            };

            reorderableLanguageList.onRemoveCallback = (ReorderableList list) =>
            {
                if (list.index >= 0 && list.index < m_LanguageTypeList.Count)
                {
                    m_LanguageTypeList.RemoveAt(list.index);
                }
            };

        }
     
        /// <summary>
        /// 下拉框值修改事件
        /// </summary>
        /// <param name="index"></param>
        private void OnPopupLanguageChange(int index)
        {
            CurLanguage = m_LanguageTypeList[index];
            m_CurEntryList = m_AllLangguageLocalitionEntry[CurLanguage];
        }

        /// <summary>
        /// 同步
        /// </summary>
        private void SyncAllKeycodes()
        {
            
            foreach (var lang in m_LanguageTypeList)
            {
                
                if(lang != m_CurLanguage)
                {
                    int fixCount = 0;
                    var entryList = m_AllLangguageLocalitionEntry[lang];
                    for (int i = 0; i < entryList.Count; i++)
                    {
                        if(entryList[i].Key != m_CurEntryList[i].Key)
                        {
                            entryList[i].Key = m_CurEntryList[i].Key;
                            fixCount++;
                        }
                        
                    }
                    Debug.Log(lang + "：" + fixCount + "个已修改");
                }
               
            }

        }

        /// <summary>
        /// 保存当前本地化文件
        /// </summary>
        private void SaveToFile()
        {
            m_KeyValuePairs = new Dictionary<string, string>();

            foreach (var entry in m_CurEntryList)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    if (!m_KeyValuePairs.ContainsKey(entry.Key))
                        m_KeyValuePairs.Add(entry.Key, entry.Value);
                    else
                        Debug.LogWarning($"重复的 Key：{entry.Key}，已跳过。");
                }
            }
            WriteLocalizationEntryList(m_CurLanguage, m_CurEntryList);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 保存指定语言的本地化文件
        /// </summary>
        /// <param name="language"></param>
        private void SaveToFile(string language)
        {
            var localizationEntryList = m_AllLangguageLocalitionEntry[language];
            WriteLocalizationEntryList(language, localizationEntryList);
        }

        /// <summary>
        /// 全部保存
        /// </summary>
        private void SaveAll()
        {
            foreach (var lang in m_LanguageTypeList)
            {
                SaveToFile(lang);
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 添加新语言词条集
        /// </summary>
        /// <returns></returns>
        private List<LocalizationEntry> AddNewLocalizationEntryList()
        {
            var newList = new List<LocalizationEntry>();
            for (int i = 0; i < m_CurEntryList.Count; i++)
            {
                newList.Add(new LocalizationEntry(m_CurEntryList[i].Key, "New Key"));
            }
            return newList;
        }

        private string GetCurrentLanguageFilePath(string language)
        {
            if(string.IsNullOrEmpty(language))
            {
                language = m_LanguageTypeList[m_selectLanguageIndex];
            }
            return folderPath + language + LocalizationUtility.GetSuffixByFileType(m_FileType);
        }

        private List<LocalizationEntry> ReadLocalizationEntryList(FileType fileType,string filePath)
        {
            switch (fileType)
            {
                case FileType.Xml:
                    return LocalizationUtility.ReadXml(filePath);
                case FileType.Json:
                    return LocalizationUtility.ReadJson(filePath);
                default:
                    return new List<LocalizationEntry>();
            }
        }

        private void WriteLocalizationEntryList(string language, List<LocalizationEntry> localizationEntryList)
        {
            string fullPath = GetCurrentLanguageFilePath(language);

            switch (m_FileType)
            {
                case FileType.Xml:
                    LocalizationUtility.WriteXml(language, fullPath, localizationEntryList);
                    break;
                case FileType.Json:
                    LocalizationUtility.WriteJson(fullPath, localizationEntryList);
                    break;
                default:
                    break;
            }
        }
    }

}
