using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

// Stack based on solid array
namespace Nim.Collections.LockFree
{
    [ComVisible(false)]
    [DebuggerDisplay("Count = {Count}")]
    public class SolidStack<T> : IEnumerable<T>
    {
        //
        const int initial_size = 64;
        //
        const int possible_threads_number = 128;
        //
        const int friction_factor = 512;

        //
        int count;
        //
        int sync;

        //
        int Capacity;
        //
        Record[] Records;

        public SolidStack() : this(initial_size)
        {
        }

        public SolidStack(int capacity)
        {
            Capacity = (capacity / friction_factor + 1) * friction_factor;

            Clear();

            //var map = new HashSet<int>();

            //foreach (var itm in IndexTable)
            //{
            //    if (map.Contains(itm))
            //    {
            //        var bp = 0;
            //    }

            //    map.Add(itm);
            //}
        }

        //public LockFreeStack(int capacity)
        //{
        //    Capacity = capacity;
        //    Clear();
        //}

        //
        public int Count => count;

        //
        public bool IsEmpty => count == 0;

        //
        //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool TryPeek(out T result)
        {
            unchecked
            {
                var rec = Records;
                var pos = count - 1;

                if (pos < 0)
                {
                    result = default;
                    return false;
                }

                pos = GetIndex(pos);

                //  fast peek
                if (pos < rec.Length)
                {
                    var itm = rec[pos];

                    if (itm.Sync == 2)
                    {
                        result = itm.Value;

                        return true;
                    }
                }

                return TryPeekCore(out result);
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool TryPeekCore(out T result)
        {
            unchecked
            {
                Record[] rec;
                int pos;

                // slow peek
                while (true)
                {
                    do
                    {
                        pos = Volatile.Read(ref count) - 1;
                        rec = Volatile.Read(ref Records);

                        pos = GetIndex(pos);
                    }
                    while (pos >= rec.Length);

                    if (pos < 0)
                    {
                        result = default;
                        return false;
                    }

                    var itm = rec[pos];

                    if (itm.Sync == 2)
                    {
                        result = itm.Value;

                        return true;
                    }
                }
            }
        }

        //
        //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Push(T value)
        {
            unchecked
            {
                var rec = Records;
                var pos = Interlocked.Add(ref count, 1) - 1;

                pos = GetIndex(pos);

                // fast push
                if (pos < rec.Length)
                {
                    while (Interlocked.CompareExchange(ref rec[pos].Sync, 1, 0) != 0)
                    {
                        rec = Volatile.Read(ref Records);
                    }

                    rec[pos] = new Record(value, 2);

                    return;
                }

                PushCore(value, pos);
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        void PushCore(T value, int pos)
        {
            Record[] rec;

            unchecked
            {
                // spin thread 
                do
                {
                    rec = Volatile.Read(ref Records);
                }
                while (pos > rec.Length);

                // check resize array
                if (pos == rec.Length)
                {
                    // lock resize
                    while (Interlocked.CompareExchange(ref sync, 1, 0) != 0)
                    {
                        rec = Volatile.Read(ref Records);
                    }

                    var len = rec.Length;

                    if (pos == len)
                    {
                        var count = 0;
                        var j = 0;

                        // lock all possible threads
                        for (var i = 0; i < len && j < possible_threads_number; ++i)
                        {
                            var itm = GetIndex(len - i - 1);

                            count++;

                            while (true)
                            {
                                var sync = Volatile.Read(ref rec[itm].Sync);

                                // lock with data
                                if (sync == 2 && Interlocked.CompareExchange(ref rec[itm].Sync, 4, 2) == 2)
                                {
                                    j++;

                                    break;
                                }

                                // lock without data
                                if (sync == 0 && Interlocked.CompareExchange(ref rec[itm].Sync, 8, 0) == 0)
                                {
                                    break;
                                }
                            }
                        }

                        var tmp = new Record[rec.Length * 2];
                        Array.Copy(rec, tmp, rec.Length);

                        Volatile.Write(ref Records, tmp);

                        rec = tmp;

                        // unlock all threads
                        for (var i = 0; i < count; ++i)
                        {
                            var itm = GetIndex(len - i - 1);

                            if (rec[itm].Sync == 4)
                            {
                                rec[itm].Sync = 2;
                            }
                            else
                            {
                                rec[itm].Sync = 0;
                            }
                        }

                        // unlock resize
                        sync = 0;
                    }
                }

                // lock record
                while (Interlocked.CompareExchange(ref rec[pos].Sync, 1, 0) != 0)
                {
                    rec = Volatile.Read(ref Records);
                }

                rec[pos] = new Record(value, 2);
            }
        }

        public bool TryPop(out T result)
        {
            var rec = Records;

            if (count < 1)
            {
                result = default;

                return false;
            }

            int pos = Interlocked.Add(ref count, -1);

            if (pos < 0)
            {
                Interlocked.Add(ref count, 1);

                result = default;

                return false;
            }

            pos = GetIndex(pos);

            while (Interlocked.CompareExchange(ref rec[pos].Sync, 1, 2) != 2)
            {
                rec = Volatile.Read(ref Records);
            }

            result = rec[pos].Value;

            rec[pos] = new Record();

            return true;
        }

        //
        //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool TryPop2(out T result)
        {
            var rec = Records;
            int pos = 0;

            if (count > possible_threads_number)
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

            pos = GetIndex(pos);

            while (Interlocked.CompareExchange(ref rec[pos].Sync, 1, 2) != 2)
            {
                rec = Volatile.Read(ref Records);
            }

            result = rec[pos].Value;

            rec[pos] = new Record();

            return true;
        }

        //[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetIndex(int number)
        {
            unchecked
            {
                var base_num = (number / friction_factor) * friction_factor;

                return base_num + IndexTable[number - base_num];
            }
        }

        // 
        public void Clear()
        {
            count = 0;
            Records = new Record[Capacity];
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
            int Position = 0;
            readonly SolidStack<T> stack;

            public Enumerator(SolidStack<T> stack)
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

                var pos = GetIndex(Position++);
                var sync = 1;

                while (sync != 0 || sync != 2)
                {
                    if (pos < stack.Records.Length)
                    {
                        sync = Volatile.Read(ref stack.Records[pos].Sync);
                    }
                }

                if (sync == 2)
                {
                    Current = stack.Records[pos].Value;

                    return true;
                }

                return false;
            }

            public void Reset()
            {
                Position = 0;
            }

            public T Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }

        #endregion

        static readonly int[] IndexTable =
        {
            000, 016, 032, 048, 192, 208, 224, 240, 064, 080, 096, 112, 128, 144, 160, 176, 
            256, 272, 288, 304, 448, 464, 480, 496, 320, 336, 352, 368, 384, 400, 416, 432, 
            001, 017, 033, 049, 193, 209, 225, 241, 065, 081, 097, 113, 129, 145, 161, 177, 
            257, 273, 289, 305, 449, 465, 481, 497, 321, 337, 353, 369, 385, 401, 417, 433, 
            002, 018, 034, 050, 194, 210, 226, 242, 066, 082, 098, 114, 130, 146, 162, 178, 
            258, 274, 290, 306, 450, 466, 482, 498, 322, 338, 354, 370, 386, 402, 418, 434, 
            003, 019, 035, 051, 195, 211, 227, 243, 067, 083, 099, 115, 131, 147, 163, 179, 
            259, 275, 291, 307, 451, 467, 483, 499, 323, 339, 355, 371, 387, 403, 419, 435, 
            004, 020, 036, 052, 196, 212, 228, 244, 068, 084, 100, 116, 132, 148, 164, 180, 
            260, 276, 292, 308, 452, 468, 484, 500, 324, 340, 356, 372, 388, 404, 420, 436, 
            005, 021, 037, 053, 197, 213, 229, 245, 069, 085, 101, 117, 133, 149, 165, 181, 
            261, 277, 293, 309, 453, 469, 485, 501, 325, 341, 357, 373, 389, 405, 421, 437, 
            006, 022, 038, 054, 198, 214, 230, 246, 070, 086, 102, 118, 134, 150, 166, 182, 
            262, 278, 294, 310, 454, 470, 486, 502, 326, 342, 358, 374, 390, 406, 422, 438, 
            007, 023, 039, 055, 199, 215, 231, 247, 071, 087, 103, 119, 135, 151, 167, 183, 
            263, 279, 295, 311, 455, 471, 487, 503, 327, 343, 359, 375, 391, 407, 423, 439, 
            008, 024, 040, 056, 200, 216, 232, 248, 072, 088, 104, 120, 136, 152, 168, 184, 
            264, 280, 296, 312, 456, 472, 488, 504, 328, 344, 360, 376, 392, 408, 424, 440, 
            009, 025, 041, 057, 201, 217, 233, 249, 073, 089, 105, 121, 137, 153, 169, 185, 
            265, 281, 297, 313, 457, 473, 489, 505, 329, 345, 361, 377, 393, 409, 425, 441, 
            010, 026, 042, 058, 202, 218, 234, 250, 074, 090, 106, 122, 138, 154, 170, 186, 
            266, 282, 298, 314, 458, 474, 490, 506, 330, 346, 362, 378, 394, 410, 426, 442, 
            011, 027, 043, 059, 203, 219, 235, 251, 075, 091, 107, 123, 139, 155, 171, 187, 
            267, 283, 299, 315, 459, 475, 491, 507, 331, 347, 363, 379, 395, 411, 427, 443, 
            012, 028, 044, 060, 204, 220, 236, 252, 076, 092, 108, 124, 140, 156, 172, 188, 
            268, 284, 300, 316, 460, 476, 492, 508, 332, 348, 364, 380, 396, 412, 428, 444, 
            013, 029, 045, 061, 205, 221, 237, 253, 077, 093, 109, 125, 141, 157, 173, 189, 
            269, 285, 301, 317, 461, 477, 493, 509, 333, 349, 365, 381, 397, 413, 429, 445, 
            014, 030, 046, 062, 206, 222, 238, 254, 078, 094, 110, 126, 142, 158, 174, 190, 
            270, 286, 302, 318, 462, 478, 494, 510, 334, 350, 366, 382, 398, 414, 430, 446, 
            015, 031, 047, 063, 207, 223, 239, 255, 079, 095, 111, 127, 143, 159, 175, 191, 
            271, 287, 303, 319, 463, 479, 495, 511, 335, 351, 367, 383, 399, 415, 431, 447, 
        };
    }
}