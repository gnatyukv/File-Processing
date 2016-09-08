using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileSignature
{
    /// <summary>
    /// All items that implement this interface may be processed by Multiplexer<T> class
    /// </summary>
    interface IMultipleProducers
    {
        long Number { get; }
    }

    class NumberComparer<T> : IComparer<T> where T : IMultipleProducers
    {
        int IComparer<T>.Compare(T t1, T t2)
        {
            if (t1.Number > t2.Number)
                return 1;
            else if (t1.Number < t2.Number)
                return -1;
            else
                return 0;
        }
    }
}
