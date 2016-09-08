using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileSignature
{
    class Multiplexer<T> where T : IMultipleProducers
    {
        private readonly SortedSet<T> lookAheadBuffer = new SortedSet<T>(new NumberComparer<T>());
        private long sequenceNumber = 1;

        public bool MonitorProducers(SyncQueue<T>[] input, out T item)
        {
            T buff;
            // -1 is a non-index value which denotes that all queues are closed to enqueue
            while (SyncQueue<T>.TryTakeFromAny(input, out buff) != -1)
            {
                if (buff.Number == sequenceNumber)
                {
                    item = buff;
                    sequenceNumber++;
                    return true;
                }
                else if (lookAheadBuffer.Any(b => b.Number == sequenceNumber))
                {
                    lookAheadBuffer.Add(buff);
                    item = lookAheadBuffer.First();
                    lookAheadBuffer.RemoveWhere(b => b.Number == sequenceNumber);
                    sequenceNumber++;
                    return true;
                }
                else
                {
                    lookAheadBuffer.Add(buff);
                }
            }
            // lookAheadBuffer may still containing items while they arrived out of order
            while (lookAheadBuffer.Count() != 0)
            {
                // must be always true in normal operation
                if (lookAheadBuffer.Any(b => b.Number == sequenceNumber))
                {
                    item = lookAheadBuffer.First();
                    lookAheadBuffer.RemoveWhere(b => b.Number == sequenceNumber);
                    sequenceNumber++;
                    return true;
                }
                else
                {
                    item = default(T);
                    return false;
                }
            }
            item = default(T);
            return false;
        }
    }
}
