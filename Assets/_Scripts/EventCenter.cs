using System;
using System.Collections.Generic;
using UnityEngine;

namespace AngryBirds.Core
{
    public class EventCenter : MonoBehaviour
{
    public static EventCenter Instance;

    private Dictionary<string, Delegate> eventDic = new();

    public enum EventType { PlayerHealthChange, PlayerStateChange, PlayerDead }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // ==================== 无参数 ====================

    public void AddListener(string eventName, Action action)
    {
        if (eventDic.TryGetValue(eventName, out var del))
        {
            eventDic[eventName] = Delegate.Combine(del, action);
        }
        else
        {
            eventDic.Add(eventName, action);
        }
    }

    public void RemoveListener(string eventName, Action action)
    {
        if (!eventDic.TryGetValue(eventName, out var del)) return;
        var result = Delegate.Remove(del, action);
        if (result == null) eventDic.Remove(eventName);
        else eventDic[eventName] = result;
    }

    public void Broadcast(string eventName)
    {
        if (eventDic.TryGetValue(eventName, out var del) && del is Action action)
            action.Invoke();
    }

    // ==================== 1 参数 ====================

    public void AddListener<T>(string eventName, Action<T> action)
    {
        if (eventDic.TryGetValue(eventName, out var del))
        {
            eventDic[eventName] = Delegate.Combine(del, action);
        }
        else
        {
            eventDic.Add(eventName, action);
        }
    }

    public void RemoveListener<T>(string eventName, Action<T> action)
    {
        if (!eventDic.TryGetValue(eventName, out var del)) return;
        var result = Delegate.Remove(del, action);
        if (result == null) eventDic.Remove(eventName);
        else eventDic[eventName] = result;
    }

    public void Broadcast<T>(string eventName, T arg)
    {
        if (eventDic.TryGetValue(eventName, out var del) && del is Action<T> action)
            action.Invoke(arg);
    }

    // ==================== 2 参数 ====================

    public void AddListener<T1, T2>(string eventName, Action<T1, T2> action)
    {
        if (eventDic.TryGetValue(eventName, out var del))
        {
            eventDic[eventName] = Delegate.Combine(del, action);
        }
        else
        {
            eventDic.Add(eventName, action);
        }
    }

    public void RemoveListener<T1, T2>(string eventName, Action<T1, T2> action)
    {
        if (!eventDic.TryGetValue(eventName, out var del)) return;
        var result = Delegate.Remove(del, action);
        if (result == null) eventDic.Remove(eventName);
        else eventDic[eventName] = result;
    }

    public void Broadcast<T1, T2>(string eventName, T1 arg1, T2 arg2)
    {
        if (eventDic.TryGetValue(eventName, out var del) && del is Action<T1, T2> action)
            action.Invoke(arg1, arg2);
    }

    // ==================== 调试 ====================

    public void Clear()
    {
        eventDic.Clear();
    }

    public int ListenerCount(string eventName)
    {
        if (eventDic.TryGetValue(eventName, out var del))
            return del.GetInvocationList().Length;
        return 0;
    }
}
}