using GameFramework.Localization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
//命名空间
namespace xxxxx
{
    public class JsonLocalizationHelper : DefaultLocalizationHelper
    {
        public class LocalizationData
        {
            public string key;
            public string value;
        }

        public override bool ParseData(ILocalizationManager localizationManager, string dictionaryString, object userData)
        {
            try
            {
                Dictionary<string,string> jsonDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionaryString);
                foreach (var kv in jsonDic)
                {
                    localizationManager.AddRawString(kv.Key, kv.Value);
                }
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"本地化 JSON 解析失败：{exception.Message}\n{exception.StackTrace}");
                return false;
            }

        }
    }

}
