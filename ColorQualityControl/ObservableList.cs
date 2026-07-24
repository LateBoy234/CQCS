using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorQualityControl
{
    public class ObservableList<T> : List<T>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public new void Add(T item)
        {
            base.Add(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, item));
        }

        public new void AddRange(IEnumerable<T> collection)
        {
            base.AddRange(collection);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, collection));
        }

        public new void Clear()
        {
            base.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
        }

        public new void Insert(int index, T item)
        {
            base.Insert(index, item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, new [] {item}, index));
        }

        public new void InsertRange(int index, IEnumerable<T> collection)
        {
            base.InsertRange(index, collection);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, collection, index));
        }

        public new void Remove(T item)
        {
            base.Remove(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, item));
        }

        public new void RemoveAll(Predicate<T> match)
        {
            List<T> machedItems = new List<T>();
            List<int> machedIndexes = new List<int>();
            for (int i = 0; i < Count; i++)
            {
                if (match(base[i]))
                {
                    machedIndexes.Add(i);
                    machedItems.Add(base[i]);
                }
            }

            for (int i = machedIndexes.Count - 1; i >= 0; i--)
            {
                RemoveAt(machedIndexes[i]);
            }
        }

        public new void RemoveAt(int index)
        {
            var item = base[index];
            base.RemoveAt(index);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, item));
        }

        public new void RemoveRange(int index, int count)
        {
            List<T> items = base.GetRange(index, count);
            base.RemoveRange(index, count);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, items, index));
        }

        public new void Sort()
        {
            base.Sort();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
        }

        public new void Sort(Comparison<T> comparison)
        {
            base.Sort(comparison);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
        }

        public new void Sort(IComparer<T> comparison)
        {
            base.Sort(comparison);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
        }

        public new T this[int index]
        {
            get { return base[index]; }
            set
            {
                var old = base[index];
                base[index] = value;
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace,  value, old, index));
            }
        }
    }
}
