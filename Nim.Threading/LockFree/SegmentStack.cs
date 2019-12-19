using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Nim.Collections.LockFree
{
    [ComVisible(false)]
    [DebuggerDisplay("Count = {Count}")]
    public class SegmentStack<T> : IEnumerable<T>
    {
        //
        const int initial_segment_size = 32;

        //
        int count;
        //
        Segment First;
        //
        Segment Current;

        public SegmentStack()
        {
            Clear();
        }

        //
        public int Count => count;

        //
        public bool IsEmpty => count == 0;

        //
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool TryPeek(out T result)
        {
            while (true) unchecked
                {
                    var seg = Volatile.Read(ref Current);
                    var pos = Volatile.Read(ref count);

                    if (pos == 0)
                    {
                        result = default;

                        return false;
                    }

                    var index = pos - seg.Offset - 1;

                    if (index < 0 || index >= seg.Length)
                    {
                        continue;
                    }

                    var itm = seg.Values[index];

                    if (itm.Sync == 2)
                    {
                        result = itm.Value;

                        return true;
                    }
                }
        }

        //
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Push(T value)
        {
            var pos = Interlocked.Add(ref count, 1) - 1;
            var seg = Current;

            while (true) unchecked
                {
                    var index = pos - seg.Offset;
                    var len = seg.Length;

                    if (index < 0)
                    {
                        seg = seg.Back;
                    }
                    else if (index >= len)
                    {
                        var next = seg.Next;

                        while (next == null)
                        {
                            next = Volatile.Read(ref seg.Next);
                        }

                        seg = next;

                        if (index == len)
                        {
                            Current = seg;
                        }
                    }
                    else
                    {
                        while (Interlocked.CompareExchange(ref seg.Values[index].Sync, 1, 0) != 0)
                        {
                        }

                        if (index == 1)
                        {
                            if (seg.Next == null)
                            {
                                seg.Next = new Segment(seg, seg.Length * 2, seg.Offset + seg.Length);
                            }
                        }

                        seg.Values[index] = new Record(value, 2);

                        return;
                    }
                }
        }

        //
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool TryPop(out T result)
        {
            var pos = 0;
            var seg = Current;

            if (seg.Length > initial_segment_size)
            {
                pos = Interlocked.Add(ref count, -1);
            }
            else
            {
                var _count = count;

                while (true)
                {
                    if (_count == 0)
                    {
                        result = default;

                        return false;
                    }

                    var tmp = Interlocked.CompareExchange(ref count, _count - 1, _count);

                    if (tmp == _count)
                    {
                        pos = _count - 1;

                        break;
                    }

                    _count = tmp;
                }
            }

            while (true)
            {
                var index = pos - seg.Offset;

                if (index < 0)
                {
                    seg = seg.Back;

                    continue;
                }
                else if (index > seg.Length)
                {
                    seg = Volatile.Read(ref Current);

                    continue;
                }

                while (Interlocked.CompareExchange(ref seg.Values[index].Sync, 1, 2) != 2)
                {
                }

                if (index == 0 && seg.Back != null)
                {
                    seg.Next = null;

                    Current = seg.Back;
                }

                result = seg.Values[index].Value;

                seg.Values[index] = new Record();

                return true;
            }
        }

        // 
        public void Clear()
        {
            count = 0;
            Current = First = new Segment(null, initial_segment_size, 0);
        }

        // 
        [DebuggerDisplay("Sync = {Sync}, Value = {Value}")]
        struct Record
        {
            public Record(T value, int sync)
            {
                Value = value;
                Sync = sync;
            }

            public T Value;
            public int Sync;
        }

        // 
        [DebuggerDisplay("Offset = {Offset}, Length = {Length}")]
        sealed class Segment
        {
            // 
            public Segment Next;
            public Segment Back;
            public int Offset { get; }
            //
            public int Length { get; }
            //
            public Record[] Values { get; }

            public Segment(Segment above, int size, int offset)
            {
                Values = new Record[size];
                Offset = offset;
                Length = Values.Length;
                Back = above;

                if (above != null)
                {
                    above.Next = this;
                }
            }
        }

        #region ' IEnumerable<T> '

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class Enumerator : IEnumerator<T>
        {
            readonly SegmentStack<T> stack;
            int Position = 0;
            Segment Segment;

            public Enumerator(SegmentStack<T> stack)
            {
                this.stack = stack;

                Reset();
            }

            public bool MoveNext()
            {
                if (Position >= stack.count)
                {
                    return false;
                }

                var pos = Position++;
                var index = pos - Segment.Offset;

                if (index == Segment.Length)
                {
                    Segment = Segment.Next;

                    index = 0;
                }

                Current = Segment.Values[index].Value;

                return true;
            }

            public void Reset()
            {
                Position = 0;
                Segment = stack.First;
            }

            public T Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }

        #endregion
    }
}