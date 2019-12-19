// Maksim Burtsev https://github.com/nim
// Licensed under the MIT license.

namespace Nim.Collections.Generic
{

    public class SegmentStack<T>
    {
        Segment Current;

        public SegmentStack()
        {
            Current = new Segment(null, 32, 0);
        }

        public SegmentStack(int capacity)
        {
            Current = new Segment(null, capacity, 0);
        }

        public int Count { get; private set; }

        public T Peek()
        {
            unchecked
            {
                if (Count == 0)
                {
                    return default;
                }

                var seg = Current;

                return seg.Values[Count - seg.Offset];
            }
        }

        // 
        public void Push(T value)
        {
            unchecked
            {
                var seg = Current;
                var count = Count++;
                var index = count - seg.Offset;

                // Write value
                if (index < seg.Length)
                {
                    seg.Values[index] = value;

                    return;
                }

                // Create new segmment
                var tmp = new Segment(seg, seg.Length * 2, count);

                Current = tmp;

                tmp.Values[0] = value;
            }
        }

        public T Pop()
        {
            unchecked
            {
                if (Count == 0)
                {
                    return default;
                }

                var seg = Current;
                var count = --Count;
                var index = count - seg.Offset;

                // Write value
                if (index < 0)
                {
                    seg = Current = Current.Back;

                    index = count - seg.Offset;
                }

                var val = seg.Values[index];

                seg.Values[index] = default;

                return val;
            }
        }

        sealed class Segment
        {
            public Segment Back;
            public T[] Values;
            public int Offset;
            public int Length;

            public Segment(Segment back, int size, int offset)
            {
                Back = back;
                Offset = offset;
                Values = new T[size];
                Length = size;
            }
        }
    }
}
