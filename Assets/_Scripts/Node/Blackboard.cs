using System.Collections.Generic;

public class Blackboard
{
    private Dictionary<string, object> data = new();

    public void Set(string key, object value)
    {
        data[key] = value;
    }

    public T Get<T>(string key) where T : class
    {
        data.TryGetValue(key, out var value);
        return value as T;
    }

    public void Remove(string key)
    {
        data.Remove(key);
    }
}
