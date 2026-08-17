using System;
using System.Collections.Generic;

/// <summary>
/// 带优先级的无参事件 — 高优先级先执行
/// </summary>
public class PriorityEvent
{
    private readonly List<Entry> entries = new();
    private bool dirty;

    private struct Entry
    {
        public int priority;
        public Action handler;
    }

    public void Subscribe(Action handler, int priority = 0)
    {
        entries.Add(new Entry { priority = priority, handler = handler });
        dirty = true;
    }

    public void Unsubscribe(Action handler)
    {
        var entry = entries.Find(e => e.handler == handler);
        if (entry.handler != null)  // struct 默认值判断
            entries.Remove(entry);
    }

    public void Invoke()
    {
        if (entries.Count == 0) return;

        if (dirty)
        {
            entries.Sort((a, b) => b.priority.CompareTo(a.priority));
            dirty = false;
        }

     
        var snapshot = entries.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
            snapshot[i].handler?.Invoke();
    }

    public void Clear() => entries.Clear();
}

/// <summary>
/// 带优先级的单参事件 — 高优先级先执行
/// </summary>
public class PriorityEvent<T>
{
    private readonly List<Entry> entries = new();
    private bool dirty;

    private struct Entry
    {
        public int priority;
        public Action<T> handler;
    }

    public void Subscribe(Action<T> handler, int priority = 0)
    {
        entries.Add(new Entry { priority = priority, handler = handler });
        dirty = true;
    }

    public void Unsubscribe(Action<T> handler)
    {
        var entry = entries.Find(e => e.handler == handler);
        if (entry.handler != null)  
            entries.Remove(entry);
    }

    public void Invoke(T arg)
    {
        if (entries.Count == 0) return;

        if (dirty)
        {
            entries.Sort((a, b) => b.priority.CompareTo(a.priority));
            dirty = false;
        }

        var snapshot = entries.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
            snapshot[i].handler?.Invoke(arg);
    }

    public void Clear() => entries.Clear();
}
