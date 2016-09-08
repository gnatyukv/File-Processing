using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

namespace FileSignature
{
    class SyncQueue<T> where T : IMultipleProducers
    {
        private readonly Queue<T> queue = new Queue<T>();
        // <TKey,TValue> - <Sequence number, Index of collection>
        private static readonly SortedDictionary<long, int> collectionsNumbers = new SortedDictionary<long, int>();
        private readonly int boundedCapacity;
        private bool isClosing;
        private bool isClosed;

        public bool IsClosed { get { lock (queue) return isClosed; } }

        public SyncQueue() { boundedCapacity = int.MaxValue; }
        public SyncQueue(int boundedCap) { boundedCapacity = boundedCap; }

        public void Enqueue(T item)
        {
            lock (queue)
            {
                while (queue.Count >= boundedCapacity)
                {
                    Monitor.Wait(queue);
                }
                queue.Enqueue(item);
                if (queue.Count == 1)
                {
                    Monitor.PulseAll(queue);
                }
            }
        }

        public bool TryDequeue(out T value)
        {
            lock (queue)
            {
                while (queue.Count == 0)
                {
                    if (isClosing)
                    {
                        isClosed = true;
                        value = default(T);
                        return false;
                    }
                    Monitor.Wait(queue);
                }
                value = queue.Dequeue();
                if (queue.Count == boundedCapacity - 1)
                {
                    Monitor.PulseAll(queue);
                }
                return true;
            }
        }

        public bool TryTake(out T value)
        {
            lock (queue)
            {
                if (queue.Count == 0)
                {
                    if (isClosing)
                    {
                        isClosed = true;
                    }
                    value = default(T);
                    return false;
                }
                value = queue.Dequeue();
                if (queue.Count == boundedCapacity - 1)
                {
                    Monitor.PulseAll(queue);
                }
                return true;
            }
        }

        public bool TryPeek(out T value)
        {
            lock (queue)
            {
                if (queue.Count == 0)
                {
                    value = default(T);
                    return false;
                }
                value = queue.Peek();
                return true;
            }
        }

        public static int TryTakeFromAny(SyncQueue<T>[] collections, out T item)
        {
            int optimalCollectionIndex;

            // Try determine the collection with the minimal item Number to take it from
            collectionsNumbers.Clear();
            T buff;
            for (int i = 0; i < collections.Length; i++)
            {
                if (collections[i].TryPeek(out buff))
                    if (!collectionsNumbers.ContainsKey(buff.Number))
                        collectionsNumbers.Add(buff.Number, i);
            }
            optimalCollectionIndex = collectionsNumbers.Count != 0 ? collectionsNumbers.First().Value : 0;

            while (true)
            {
                if (collections.Count(q => q.IsClosed) == collections.Length)
                {
                    item = default(T);
                    return -1;
                }
                for (int i = optimalCollectionIndex; i < collections.Length; i++)
                {
                    if (!collections[i].IsClosed && collections[i].TryTake(out item))
                    {
                        return i;
                    }
                }
            }
        }

        public void Close()
        {
            lock (queue)
            {
                isClosing = true;
                Monitor.PulseAll(queue);
            }
        }
    }
}
