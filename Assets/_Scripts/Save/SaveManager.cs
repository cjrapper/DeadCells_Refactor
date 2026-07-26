using System.IO;
using DeadCells.Save;
using UnityEngine;

public class SaveManager: MonoBehaviour
{
    public static SaveManager Instance{get; private set;}

    void Awake()
    {
        if(Instance != null){Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void SaveGame(int slot)
    {
        SaveData data = new SaveData();
        foreach(var comp in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (comp is ISaveable s) s.OnSave(data);
        data.saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string json = JsonUtility.ToJson(data,true);//格式化打印
        File.WriteAllText(GetSlotPath(slot), json);
        Debug.Log($"Saved game slot: {slot}已保存");
    }

    public void LoadGame(int slot)
    {
        string path = GetSlotPath(slot);
        if (!File.Exists(path)) return;
        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        
        foreach(var comp in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if(comp is ISaveable s)s.OnLoad(data);
        Debug.Log($"[Save]槽位{slot}已加载");
    }

    public bool HasSave(int slot)
    {
        return File.Exists(GetSlotPath(slot));
    }

    public void DeleteSlot(int slot)
    {
        string path = GetSlotPath(slot);
        if (File.Exists(path)) File.Delete(path);
    }

    private string GetSlotPath(int slot)
    {
        string folder = Application.persistentDataPath + "/Saves";
        Directory.CreateDirectory(folder);
        return folder + $"/slot_{slot}.json";
    }
}