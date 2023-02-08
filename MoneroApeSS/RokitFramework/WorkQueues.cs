using System;
using System.Collections.Generic;
using System.Text;

namespace RokitFramework
{
  public interface IWorkQueue<T>
  {
    List<T> GetItems();

    void Add(T item);

    void Remove(T item);

    bool IsQueued(T item);

    void Reset();

    T GetNext();

  }

  public abstract class WorkQueue<T> : IWorkQueue<T>
  {
    private List<T> _QueuedItems;

    public abstract object Synchronize { get; }


    protected WorkQueue()
    {
      _QueuedItems = new List<T>();
    }

    public List<T> GetItems()
    {
      lock (_QueuedItems)
      {
        List<T> Items = new List<T>(_QueuedItems);

        return Items;
      }
    }

    public void Add(T item)
    {
      lock (_QueuedItems)
      {
        _QueuedItems.Add(item);
      }
    }

    public void Remove(T item)
    {
      lock (_QueuedItems)
      {
        while (_QueuedItems.Contains(item))
        {
          if (!_QueuedItems.Remove(item))
          {
            break;
          }
        }
      }
    }

    public bool IsQueued(T item)
    {
      lock (_QueuedItems)
      {
        return _QueuedItems.Contains(item);
      }
    }

    public void Reset()
    {
      lock (_QueuedItems)
      {
        _QueuedItems.Clear();
      }
    }

    public T GetNext()
    {
      lock (_QueuedItems)
      {
        if (_QueuedItems.Count > 0)
        {
          return _QueuedItems[0];
        }
      }

      return default(T);
    }

    public T this[T item]
    {
      get
      {
        lock (_QueuedItems)
        {
          int i = _QueuedItems.IndexOf(item);
          if (i > -1)
          {
            return _QueuedItems[i];
          }
          else
          {
            return default(T);
          }
        }
      }
    }
  }
}
