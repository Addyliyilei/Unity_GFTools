using GameFramework.DataTable;
using Snake;
using System.Collections.Generic;
using UnityEngine;
using DataRowBase = UnityGameFramework.Runtime.DataRowBase;

public class NameProvider
{
    private List<int> _shuffledIds;
    private int _currentIndex = 0;

    public NameProvider()
    {

        if(GameEntry.Localization.Language == GameFramework.Localization.Language.English)
        {
            Init<DRNameDataEN>();
        }
        else 
        {
            Init<DRNameDataCN>();
        }

    }

    private void Init<T>()where T : DataRowBase
    {
       
        IDataTable<T> dt = GameEntry.DataTable.GetDataTable<T>();
        _shuffledIds = new List<int>();

        foreach (var row in dt)
        {
            _shuffledIds.Add(row.Id);
        }

        Shuffle(_shuffledIds);
        _currentIndex = 0;
    }







    public string GetRandomName()
    {
        if (_shuffledIds == null || _shuffledIds.Count == 0)
            return "KK";

        // 一轮用完，重新洗牌
        if (_currentIndex >= _shuffledIds.Count)
        {
            Shuffle(_shuffledIds);
            _currentIndex = 0;
        }

        int id = _shuffledIds[_currentIndex++];
        return GetNameById(id);
    }

    private void Shuffle(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    private T GetNameDataById<T>(int nameId) where T : DataRowBase
    {
        IDataTable<T> dtNameData = GameEntry.DataTable.GetDataTable<T>();
        T drNameData = dtNameData.GetDataRow(nameId);

        if (drNameData == null)
          return null;

        return drNameData;
    }

    public string GetNameById(int id)
    {
        if(GameEntry.Localization.Language == GameFramework.Localization.Language.English)
        {
            var drNameData = GetNameDataById<DRNameDataEN>(id);
            return drNameData != null ? drNameData.Name : "KK";
        }
        else 
        {
            var drNameData = GetNameDataById<DRNameDataCN>(id);
            return drNameData != null ? drNameData.Name : "KK";
        }
    }
}
