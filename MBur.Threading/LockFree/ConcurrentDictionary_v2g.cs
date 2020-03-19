 // Maksim Burtsev https://github.com/MBurtsev
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace MBur.Collections.LockFree/*_v2g*/
{
    /// <summary>
    /// Represents a thread-safe collection of keys and values.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    /// All public and protected members of <see cref="ConcurrentDictionary{TKey,TValue}"/> are thread-safe and may be used
    /// concurrently from multiple threads.
    /// </remarks>
    [DebuggerTypeProxy(typeof(IDictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public class ConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue> where TKey : notnull
    {
        // Lock-Free hash table implementation. CAS operations are used only
        // to sync buckets, which allows threads working with different buckets
        // to have less friction. Wait-Free count is also used, which allowed to
        // abandon Interlocked.Add. All public methods are thread-safe. The
        // enumerator does not lock the hash table while reading.
        // See for more information https://www.linkedin.com/pulse/lock-free-hash-table-maksim-burtsev/

        // The default value that is used if the user has not specified a capacity.
        private const int DEFAULT_CAPACITY = 127;
        // The multiplier is used when expanding the array.
        private const int GROW_MULTIPLIER = 3;
        // The default array size of counts
        private const int COUNTS_SIZE = 16;
        // The size for first segments
        private const int CYCLE_BUFFER_SEGMENT_SIZE = 128;
        // The number of operations through which the page will be allowed to be used again.
        private const int WRITER_DELAY = 4096;//256
        // The thread Id
        [ThreadStatic] private static int t_id;
        // All current data is collected here.
        private HashTableData _data;
        // Initial table size
        private readonly int _capacity;
        // To compare keys
        private readonly IEqualityComparer<TKey> _keysComparer;
        // To compare values
        private readonly IEqualityComparer<TValue> _valuesComparer;

        #region ' Public Interface '

        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the default concurrency level, has the default initial capacity, and
        /// uses the default comparer for the key type.
        /// </summary>
        public ConcurrentDictionary()
            : this(DEFAULT_CAPACITY, EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level and capacity, and uses the default
        /// comparer for the key type.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// can contain.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"> <paramref name="capacity"/> is less than
        /// 0.</exception>
        public ConcurrentDictionary(int capacity)
            : this(capacity, EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level and capacity, and uses the specified
        /// <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="comparer"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public ConcurrentDictionary(IEqualityComparer<TKey> comparer)
            : this(DEFAULT_CAPACITY, comparer, EqualityComparer<TValue>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level, has the specified initial capacity, and
        /// uses the specified <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// can contain.</param>
        /// <param name="comparer">The <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="capacity"/> is less than 0.
        /// </exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="comparer"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public ConcurrentDictionary(int capacity, IEqualityComparer<TKey> comparer)
            : this(capacity, comparer, EqualityComparer<TValue>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that contains elements copied from the specified <see
        /// cref="System.Collections.Generic.IEnumerable{T}"/>, has the default concurrency
        /// level, has the default initial capacity, and uses the default comparer for the key type.
        /// </summary>
        /// <param name="collection">The <see
        /// cref="System.Collections.Generic.IEnumerable{T}"/> whose elements are copied to
        /// the new
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/>.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="collection"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentException"><paramref name="collection"/> contains one or more
        /// duplicate keys.</exception>
        public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
            : this(collection, EqualityComparer<TKey>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that contains elements copied from the specified <see
        /// cref="System.Collections.IEnumerable"/>, has the default concurrency level, has the default
        /// initial capacity, and uses the specified
        /// <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="collection">The <see
        /// cref="System.Collections.Generic.IEnumerable{T}"/> whose elements are copied to
        /// the new
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/>.</param>
        /// <param name="comparer">The <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="collection"/> is a null reference
        /// (Nothing in Visual Basic). -or-
        /// <paramref name="comparer"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : this(DEFAULT_CAPACITY, comparer, EqualityComparer<TValue>.Default)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            InitializeFromCollection(collection);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level, has the specified initial capacity, and
        /// uses the specified <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// can contain.</param>
        /// <param name="keysComparer">The <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        /// <param name="valuesComparer">The <see cref="System.Collections.Generic.IEqualityComparer{TValue}"/>
        /// implementation to use when comparing values.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="capacity"/> is less than 0.
        /// </exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="keysComparer"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="valuesComparer"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public ConcurrentDictionary(int capacity, IEqualityComparer<TKey> keysComparer, IEqualityComparer<TValue> valuesComparer)
        {
            if (valuesComparer == null)
            {
                throw new ArgumentNullException(nameof(valuesComparer));
            }

            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ConcurrentDictionary_CapacityMustNotBeNegative);
            }

            // save capacity value for Clear() method
            _capacity       = capacity;
            _keysComparer   = keysComparer ?? EqualityComparer<TKey>.Default;
            _valuesComparer = valuesComparer;
            _data           = new HashTableData(capacity, COUNTS_SIZE);
        }

        #region ' Deprecated '

        // This constructors are deprecated because concurrencyLevel not existed in current realization
        // ------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level and capacity, and uses the default
        /// comparer for the key type.
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/> concurrently.</param>
        /// <param name="capacity">The initial number of elements that the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// can contain.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="concurrencyLevel"/> is
        /// less than 1.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"> <paramref name="capacity"/> is less than
        /// 0.</exception>
        [Obsolete("This constructor will soon be deprecated. Use 'ConcurrentDictionary<TKey,TValue>(int capacity)' instead")]
        public ConcurrentDictionary(int concurrencyLevel, int capacity)
            : this(capacity)
        {
            if (concurrencyLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrencyLevel), SR.ConcurrentDictionary_ConcurrencyLevelMustBePositive);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level, has the specified initial capacity, and
        /// uses the specified <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/> concurrently.</param>
        /// <param name="capacity">The initial number of elements that the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// can contain.</param>
        /// <param name="comparer">The <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="concurrencyLevel"/> is less than 1. -or-
        /// <paramref name="capacity"/> is less than 0.
        /// </exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="comparer"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        [Obsolete("This constructor will soon be deprecated. Use 'ConcurrentDictionary<TKey,TValue>(int capacity, IEqualityComparer<TKey> comparer)' instead")]
        public ConcurrentDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer)
            : this(capacity, comparer)
        {
            if (concurrencyLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrencyLevel), SR.ConcurrentDictionary_ConcurrencyLevelMustBePositive);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that contains elements copied from the specified <see cref="System.Collections.IEnumerable"/>,
        /// has the specified concurrency level, has the specified initial capacity, and uses the specified
        /// <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/> concurrently.</param>
        /// <param name="collection">The <see cref="System.Collections.Generic.IEnumerable{T}"/> whose elements are copied to the new
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/>.</param>
        /// <param name="comparer">The <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/> implementation to use
        /// when comparing keys.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="collection"/> is a null reference (Nothing in Visual Basic).
        /// -or-
        /// <paramref name="comparer"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="concurrencyLevel"/> is less than 1.
        /// </exception>
        /// <exception cref="System.ArgumentException"><paramref name="collection"/> contains one or more duplicate keys.</exception>
        [Obsolete("This constructor will soon be deprecated. Use 'ConcurrentDictionary<TKey,TValue>(IEnumerable<KeyValuePair<TKey, TValue>> collection, int capacity)' instead")]
        public ConcurrentDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : this(collection, comparer)
        {
            if (concurrencyLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrencyLevel), SR.ConcurrentDictionary_ConcurrencyLevelMustBePositive);
            }
        }

        #endregion

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <value>The number of key/value paris contained in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</value>
        /// <remarks>Count has snapshot semantics and represents the number of items in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// at the moment when Count was accessed.</remarks>
        public int Count
        {
            get
            {
                var count = 0L;

                unchecked
                {
                    var counts = _data.Counts;

                    for (var i = 0; i < counts.Length; ++i)
                    {
#if TARGET_32BIT
                        // The Read method is unnecessary on 64-bit systems, because 64-bit
                        // read operations are already atomic. On 32-bit systems, 64-bit read
                        // operations are not atomic unless performed using Read.
                        // https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.read?view=netframework-4.8
                        count += Interlocked.Read(ref counts[i].Count);
#else
                        count += counts[i].Count;
#endif
                    }
                }

                return (int)count;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="ConcurrentDictionary{TKey,TValue}"/> is empty.
        /// </summary>
        /// <value>true if the <see cref="ConcurrentDictionary{TKey,TValue}"/> is empty; otherwise,
        /// false.</value>
        public bool IsEmpty
        {
            get
            {
                var count = 0L;

                unchecked
                {
                    var counts = _data.Counts;

                    for (var i = 0; i < counts.Length; ++i)
                    {
#if TARGET_32BIT
                        count += Interlocked.Read(ref counts[i].Count);
#else
                        count += counts[i].Count;
#endif
                    }
                }

                return count == 0L;
            }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <value>The value associated with the specified key. If the specified key is not found, a get
        /// operation throws a
        /// <see cref="System.Collections.Generic.KeyNotFoundException"/>, and a set operation creates a new
        /// element with the specified key.</value>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The property is retrieved and
        /// <paramref name="key"/>
        /// does not exist in the collection.</exception>
        public TValue this[TKey key]
        {
            get
            {
                if (!TryGetValue(key, out var value))
                {
                    ThrowKeyNotFoundException(key);
                }

                return value;
            }
            set
            {
                AddOrUpdate(key, value, value);
            }
        }

        /// <summary>
        /// Determines whether the <see cref="ConcurrentDictionary{TKey,TValue}"/> contains the specified
        /// key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="ConcurrentDictionary{TKey,TValue}"/>.</param>
        /// <returns>true if the <see cref="ConcurrentDictionary{TKey,TValue}"/> contains an element with
        /// the specified key; otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public bool ContainsKey(TKey key)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            unchecked
            {
                var data      = _data;
                var comp      = _keysComparer;
                var frame     = data.Frame;
                var hash      = comp.GetHashCode(key) & 0x7fffffff;
                var index     = hash % frame.HashMaster;
                var sync      = frame.SyncTable[index];

                if ((sync & (int)RecordStatus.Grown) != 0)
                {
                    frame = Volatile.Read(ref data.GrowingFrame);
                    index = hash % frame.HashMaster;
                    sync  = frame.SyncTable[index];
                }

                ref var link  = ref frame.Links[index];
                ref var buck  = ref data.Cabinets[link.Id].Buckets[link.Positon];

                if (
                        (sync & (int)RecordStatus.HasValue) != 0
                                    &&
                        comp.Equals(key, buck.Key)
                   )
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key from the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, <paramref name="value"/> contains the object from
        /// the
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/> with the specified key or the default value of
        /// <typeparamref name="TValue"/>, if the operation failed.</param>
        /// <returns>true if the key was found in the <see cref="ConcurrentDictionary{TKey,TValue}"/>;
        /// otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            // Each change occurs in a new area of memory. The operability of this 
            // function is provided by the time-lag effect. When a thread loses a 
            // time quantum when switching tasks, its expectation of a new quantum 
            // is much less than the time when a page can be reused for recording.

            unchecked
            {
                var data  = _data;
                var comp  = _keysComparer;
                var frame = data.Frame;
                var hash  = comp.GetHashCode(key) & 0x7fffffff;
                var index = hash % frame.HashMaster;
                var sync  = frame.SyncTable[index];

                if ((sync & (int)RecordStatus.Grown) != 0)
                {
                    frame = Volatile.Read(ref data.GrowingFrame);
                    index = hash % frame.HashMaster;
                    sync  = frame.SyncTable[index];
                }

                ref var link  = ref frame.Links[index];
                ref var buck  = ref data.Cabinets[link.Id].Buckets[link.Positon];

                // check exist
                if (
                        (sync & (int)RecordStatus.HasValue) != 0
                                    &&
                        comp.Equals(key, buck.Key)
                   )
                {
                    value = buck.Value;

                    return true;
                }

                // not exist
                value = default;

                return false;
            }
        }

        /// <summary>
        /// Attempts to add the specified key and value to the <see cref="ConcurrentDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be a null reference (Nothing
        /// in Visual Basic) for reference types.</param>
        /// <returns>true if the key/value pair was added to the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// successfully; otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// contains too many elements.</exception>
        public bool TryAdd(TKey key, TValue value)
        {
            // TODO Remove here. To pass EtwTests.
            //if (CDSCollectionETWBCLProvider.Log.IsEnabled())
            //{
            //    CDSCollectionETWBCLProvider.Log.ConcurrentDictionary_AcquiringAllLocks(3);
            //}

            if (key == null)
            {
                ThrowKeyNullException();
            }

            unchecked
            {
                var data  = _data;
                var frame = data.Frame;
                var comp  = _keysComparer;
                var hash  = comp.GetHashCode(key) & 0x7fffffff;
                var cabn  = GetCabinet(data);

                PreparePage(cabn);

                // search empty space
                while (true)
                {
                    var syncs = frame.SyncTable;
                    var index = hash % frame.HashMaster;
                    var sync  = syncs[index];

                    if ((sync & (int)RecordStatus.Grown) != 0)
                    {
                        frame = Volatile.Read(ref data.GrowingFrame);
                        syncs = frame.SyncTable;
                        index = hash % frame.HashMaster;
                        sync  = syncs[index];
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.HasValue)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    if ((sync & (int)RecordStatus.HasValue) == 0)
                    {
                        // try to get lock
                        if (Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Adding, sync) == sync)
                        {
                            try
                            {
                                var page = cabn.ReadyPage;

                                cabn.Buckets[page].Key   = key;
                                cabn.Buckets[page].Value = value;
                                cabn.ReadyPage           = -1;

                                frame.Links[index] = new Link { Id = cabn.Id, Positon = page };

                                syncs[index] = (int)RecordStatus.HasValue;

                                IncrementCount(data);

                                return true;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    // growing
                    else
                    {
                        ref var link = ref frame.Links[index];
                        ref var buck = ref data.Cabinets[link.Id].Buckets[link.Positon];

                        // check exist
                        if (comp.Equals(key, buck.Key))
                        {
                            return false;
                        }

                        GrowTable(data);
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>Removes a key and value from the dictionary.</summary>
        /// <param name="item">The <see cref="KeyValuePair{TKey,TValue}"/> representing the key and value to remove.</param>
        /// <returns>
        /// true if the key and value represented by <paramref name="item"/> are successfully
        /// found and removed; otherwise, false.
        /// </returns>
        /// <remarks>
        /// Both the specifed key and value must match the entry in the dictionary for it to be removed.
        /// The key is compared using the dictionary's comparer (or the default comparer for <typeparamref name="TKey"/>
        /// if no comparer was provided to the dictionary when it was constructed).  The value is compared using the
        /// default comparer for <typeparamref name="TValue"/>.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <see cref="KeyValuePair{TKey, TValue}.Key"/> property of <paramref name="item"/> is a null reference.
        /// </exception>
        public bool TryRemove(KeyValuePair<TKey, TValue> item)
        {
            var val = item.Value;
            var key = item.Key;

            if (key is null)
            {
                throw new ArgumentNullException(nameof(item), SR.ConcurrentDictionary_ItemKeyIsNull);
            }

            unchecked
            {
                var data  = _data;
                var frame = data.Frame;
                var comp  = _keysComparer;
                var hash  = comp.GetHashCode(key) & 0x7fffffff;

                while (true)
                {
                    var syncs = frame.SyncTable;
                    var index = hash % frame.HashMaster;
                    var sync  = syncs[index];

                    if ((sync & (int)RecordStatus.Grown) != 0)
                    {
                        frame = Volatile.Read(ref data.GrowingFrame);
                        syncs = frame.SyncTable;
                        index = hash % frame.HashMaster;
                        sync  = syncs[index];
                    }

                    // return if empty
                    if (sync == (int)RecordStatus.Empty)
                    {
                        return false;
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.HasValue)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    ref var link = ref frame.Links[index];
                    ref var buck = ref data.Cabinets[link.Id].Buckets[link.Positon];

                    // check
                    if (!comp.Equals(key, buck.Key) || !_valuesComparer.Equals(val, buck.Value))
                    {
                        return false;
                    }

                    // try to get lock
                    if (Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Removing, sync) == sync)
                    {
                        try
                        {
                            RemoveLink(data, link, GetCabinet(data).Id);

                            syncs[index] = (int)RecordStatus.Empty;

                            DecrementCount(data);

                            return true;
                        }
                        catch
                        {
                            syncs[index] = sync;

                            throw;
                        }
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>
        /// Attempts to remove and return the the value with the specified key from the
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove and return.</param>
        /// <param name="value">When this method returns, <paramref name="value"/> contains the object removed from the
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/> or the default value of <typeparamref
        /// name="TValue"/>
        /// if the operation failed.</param>
        /// <returns>true if an object was removed successfully; otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public bool TryRemove(TKey key, out TValue value)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            unchecked
            {
                var data  = _data;
                var frame = data.Frame;
                var comp  = _keysComparer;
                var hash  = comp.GetHashCode(key) & 0x7fffffff;

                while (true)
                {
                    var syncs = frame.SyncTable;
                    var index = hash % frame.HashMaster;
                    var sync  = syncs[index];

                    if ((sync & (int)RecordStatus.Grown) != 0)
                    {
                        frame = Volatile.Read(ref data.GrowingFrame);
                        syncs = frame.SyncTable;
                        index = hash % frame.HashMaster;
                        sync  = syncs[index];
                    }

                    // return if empty
                    if (sync == (int)RecordStatus.Empty)
                    {
                        value = default;

                        return false;
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.HasValue)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    ref var link = ref frame.Links[index];
                    ref var buck = ref data.Cabinets[link.Id].Buckets[link.Positon];

                    // check
                    if (!comp.Equals(key, buck.Key))
                    {
                        value = default;

                        return false;
                    }

                    // try to get lock
                    if (Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Removing, sync) == sync)
                    {
                        try
                        {
                            value = buck.Value;

                            RemoveLink(data, link, GetCabinet(data).Id);

                            syncs[index] = (int)RecordStatus.Empty;

                            DecrementCount(data);

                            return true;
                        }
                        catch
                        {
                            syncs[index] = sync;

                            throw;
                        }
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>
        /// Compares the existing value for the specified key with a specified value, and if they're equal,
        /// updates the key with a third value.
        /// </summary>
        /// <param name="key">The key whose value is compared with <paramref name="comparisonValue"/> and
        /// possibly replaced.</param>
        /// <param name="newValue">The value that replaces the value of the element with <paramref
        /// name="key"/> if the comparison results in equality.</param>
        /// <param name="comparisonValue">The value that is compared to the value of the element with
        /// <paramref name="key"/>.</param>
        /// <returns>true if the value with <paramref name="key"/> was equal to <paramref
        /// name="comparisonValue"/> and replaced with <paramref name="newValue"/>; otherwise,
        /// false.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null
        /// reference.</exception>
        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            unchecked
            {
                var data  = _data;
                var frame = data.Frame;
                var comp  = _keysComparer;
                var hash  = comp.GetHashCode(key) & 0x7fffffff;
                var cabn  = GetCabinet(data);

                PreparePage(cabn);

                while (true)
                {
                    var syncs = frame.SyncTable;
                    var index = hash % frame.HashMaster;
                    var sync  = syncs[index];
                    
                    if ((sync & (int)RecordStatus.Grown) != 0)
                    {
                        frame = Volatile.Read(ref data.GrowingFrame);
                        syncs = frame.SyncTable;
                        index = hash % frame.HashMaster;
                        sync  = syncs[index];
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.HasValue)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    var link = frame.Links[index];
                    ref var buck = ref data.Cabinets[link.Id].Buckets[link.Positon];

                    // check exist
                    if (
                            (sync & (int)RecordStatus.HasValue) == 0 
                                            || 
                            !comp.Equals(key, buck.Key)
                                            ||
                            !_valuesComparer.Equals(buck.Value, comparisonValue)
                       )
                    {
                        return false;
                    }

                    // try to get lock
                    if (Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync) == sync)
                    {
                        try
                        {
                            var page = cabn.ReadyPage;

                            cabn.Buckets[page].Key   = key;
                            cabn.Buckets[page].Value = newValue;
                            cabn.ReadyPage           = -1;

                            frame.Links[index] = new Link { Id = cabn.Id, Positon = page };

                            RemoveLink(data, link, cabn.Id);
                        }
                        finally
                        {
                            syncs[index] = sync;
                        }
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// if the key does not already exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">the value to be added, if the key does not already exist</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The value for the key.  This will be either the existing value for the key if the
        /// key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
        public TValue GetOrAdd(TKey key, TValue value)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            unchecked
            {
                var data  = _data;
                var frame = data.Frame;
                var comp  = _keysComparer;
                var hash  = comp.GetHashCode(key) & 0x7fffffff;
                var cabn  = GetCabinet(data);

                PreparePage(cabn);

                // search empty space
                while (true)
                {
                    var syncs = frame.SyncTable;
                    var index = hash % frame.HashMaster;
                    var sync  = syncs[index];

                    if ((sync & (int)RecordStatus.Grown) != 0)
                    {
                        frame = Volatile.Read(ref data.GrowingFrame);
                        syncs = frame.SyncTable;
                        index = hash % frame.HashMaster;
                        sync  = syncs[index];
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.HasValue)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    if ((sync & (int)RecordStatus.HasValue) == 0)
                    {
                        // try to get lock
                        if (Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Adding, sync) == sync)
                        {
                            try
                            {
                                var page = cabn.ReadyPage;

                                cabn.Buckets[page].Key   = key;
                                cabn.Buckets[page].Value = value;
                                cabn.ReadyPage           = -1;

                                frame.Links[index] = new Link { Id = cabn.Id, Positon = page };

                                syncs[index] = (int)RecordStatus.HasValue;

                                IncrementCount(data);

                                return value;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    // growing
                    else
                    {
                        ref var link = ref frame.Links[index];
                        ref var buck = ref data.Cabinets[link.Id].Buckets[link.Positon];

                        // check exist
                        if (comp.Equals(key, buck.Key))
                        {
                            return buck.Value;
                        }

                        GrowTable(data);
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// if the key does not already exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="valueFactory">The function used to generate a value for the key</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="valueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The value for the key.  This will be either the existing value for the key if the
        /// key is already in the dictionary, or the new value for the key as returned by valueFactory
        /// if the key was not in the dictionary.</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            if (valueFactory == null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            unchecked
            {
                var data  = _data;
                var frame = data.Frame;
                var comp  = _keysComparer;
                var hash  = comp.GetHashCode(key) & 0x7fffffff;
                var cabn  = GetCabinet(data);

                PreparePage(cabn);

                // search empty space
                while (true)
                {
                    var syncs = frame.SyncTable;
                    var index = hash % frame.HashMaster;
                    var sync  = syncs[index];

                    if ((sync & (int)RecordStatus.Grown) != 0)
                    {
                        frame = Volatile.Read(ref data.GrowingFrame);
                        syncs = frame.SyncTable;
                        index = hash % frame.HashMaster;
                        sync  = syncs[index];
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.HasValue)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    if ((sync & (int)RecordStatus.HasValue) == 0)
                    {
                        // try to get lock
                        if (Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Adding, sync) == sync)
                        {
                            try
                            {
                                var value = valueFactory(key);
                                var page  = cabn.ReadyPage;

                                cabn.Buckets[page].Key   = key;
                                cabn.Buckets[page].Value = value;
                                cabn.ReadyPage           = -1;

                                frame.Links[index] = new Link { Id = cabn.Id, Positon = page };

                                syncs[index] = (int)RecordStatus.HasValue;

                                IncrementCount(data);

                                return value;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    // growing
                    else
                    {
                        ref var link = ref frame.Links[index];
                        ref var buck = ref data.Cabinets[link.Id].Buckets[link.Positon];

                        // check exist
                        if (comp.Equals(key, buck.Key))
                        {
                            return buck.Value;
                        }

                        GrowTable(data);
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// if the key does not already exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="valueFactory">The function used to generate a value for the key</param>
        /// <param name="factoryArgument">An argument value to pass into <paramref name="valueFactory"/>.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="valueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The value for the key.  This will be either the existing value for the key if the
        /// key is already in the dictionary, or the new value for the key as returned by valueFactory
        /// if the key was not in the dictionary.</returns>
        public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            if (valueFactory == null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            unchecked
            {
                var data  = _data;
                var frame = data.Frame;
                var comp  = _keysComparer;
                var hash  = comp.GetHashCode(key) & 0x7fffffff;
                var cabn  = GetCabinet(data);

                PreparePage(cabn);

                // search empty space
                while (true)
                {
                    var syncs = frame.SyncTable;
                    var index = hash % frame.HashMaster;
                    var sync  = syncs[index];

                    if ((sync & (int)RecordStatus.Grown) != 0)
                    {
                        frame = Volatile.Read(ref data.GrowingFrame);
                        syncs = frame.SyncTable;
                        index = hash % frame.HashMaster;
                        sync  = syncs[index];
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.HasValue)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    if ((sync & (int)RecordStatus.HasValue) == 0)
                    {
                        // try to get lock
                        if (Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Adding, sync) == sync)
                        {
                            try
                            {
                                var value = valueFactory(key, factoryArgument);
                                var page  = cabn.ReadyPage;

                                cabn.Buckets[page].Key   = key;
                                cabn.Buckets[page].Value = value;
                                cabn.ReadyPage           = -1;

                                frame.Links[index] = new Link { Id = cabn.Id, Positon = page };

                                syncs[index] = (int)RecordStatus.HasValue;

                                IncrementCount(data);

                                return value;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    // growing
                    else
                    {
                        ref var link = ref frame.Links[index];
                        ref var buck = ref data.Cabinets[link.Id].Buckets[link.Positon];

                        // check exist
                        if (comp.Equals(key, buck.Key))
                        {
                            return buck.Value;
                        }

                        GrowTable(data);
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key does not already
        /// exist, or updates a key/value pair in the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key
        /// already exists.
        /// </summary>
        /// <param name="key">The key to be added or whose value should be updated</param>
        /// <param name="addValueFactory">The function used to generate a value for an absent key</param>
        /// <param name="updateValueFactory">The function used to generate a new value for an existing key
        /// based on the key's existing value</param>
        /// <param name="factoryArgument">An argument to pass into <paramref name="addValueFactory"/> and <paramref name="updateValueFactory"/>.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="addValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="updateValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The new value for the key.  This will be either be the result of addValueFactory (if the key was
        /// absent) or the result of updateValueFactory (if the key was present).</returns>
        public TValue AddOrUpdate<TArg>(TKey key, Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            if (addValueFactory == null)
            {
                throw new ArgumentNullException(nameof(addValueFactory));
            }

            if (updateValueFactory == null)
            {
                throw new ArgumentNullException(nameof(updateValueFactory));
            }

            unchecked
            {
                var data  = _data;
                var frame = data.Frame;
                var comp  = _keysComparer;
                var hash  = comp.GetHashCode(key) & 0x7fffffff;
                var cabn  = GetCabinet(data);

                PreparePage(cabn);

                // search empty space
                while (true)
                {
                    var syncs = frame.SyncTable;
                    var index = hash % frame.HashMaster;
                    var sync  = syncs[index];

                    if ((sync & (int)RecordStatus.Grown) != 0)
                    {
                        frame = Volatile.Read(ref data.GrowingFrame);
                        syncs = frame.SyncTable;
                        index = hash % frame.HashMaster;
                        sync  = syncs[index];
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.HasValue)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    if ((sync & (int)RecordStatus.HasValue) == 0)
                    {
                        // try to get lock
                        if (Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Adding, sync) == sync)
                        {
                            try
                            {
                                var value = addValueFactory(key, factoryArgument);
                                var page  = cabn.ReadyPage;

                                cabn.Buckets[page].Key   = key;
                                cabn.Buckets[page].Value = value;
                                cabn.ReadyPage           = -1;

                                frame.Links[index] = new Link { Id = cabn.Id, Positon = page };

                                syncs[index] = (int)RecordStatus.HasValue;

                                IncrementCount(data);

                                return value;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    // growing
                    else
                    {
                        var link = frame.Links[index];
                        ref var buck = ref data.Cabinets[link.Id].Buckets[link.Positon];

                        // check exist
                        if (comp.Equals(key, buck.Key))
                        {
                            if (Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync) == sync)
                            {
                                try
                                {
                                    var value = updateValueFactory(key, buck.Value, factoryArgument);
                                    var page  = cabn.ReadyPage;

                                    cabn.Buckets[page].Key   = key;
                                    cabn.Buckets[page].Value = value;
                                    cabn.ReadyPage           = -1;

                                    frame.Links[index] = new Link { Id = cabn.Id, Positon = page };

                                    RemoveLink(data, link, cabn.Id);

                                    return value;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }

                        GrowTable(data);
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key does not already
        /// exist, or updates a key/value pair in the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key
        /// already exists.
        /// </summary>
        /// <param name="key">The key to be added or whose value should be updated</param>
        /// <param name="addValueFactory">The function used to generate a value for an absent key</param>
        /// <param name="updateValueFactory">The function used to generate a new value for an existing key
        /// based on the key's existing value</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="addValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="updateValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The new value for the key.  This will be either be the result of addValueFactory (if the key was
        /// absent) or the result of updateValueFactory (if the key was present).</returns>
        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            if (addValueFactory == null)
            {
                throw new ArgumentNullException(nameof(addValueFactory));
            }

            if (updateValueFactory == null)
            {
                throw new ArgumentNullException(nameof(updateValueFactory));
            }

            unchecked
            {
                var data  = _data;
                var frame = data.Frame;
                var comp  = _keysComparer;
                var hash  = comp.GetHashCode(key) & 0x7fffffff;
                var cabn  = GetCabinet(data);

                PreparePage(cabn);

                // search empty space
                while (true)
                {
                    var syncs = frame.SyncTable;
                    var index = hash % frame.HashMaster;
                    var sync  = syncs[index];

                    if ((sync & (int)RecordStatus.Grown) != 0)
                    {
                        frame = Volatile.Read(ref data.GrowingFrame);
                        syncs = frame.SyncTable;
                        index = hash % frame.HashMaster;
                        sync  = syncs[index];
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.HasValue)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    if ((sync & (int)RecordStatus.HasValue) == 0)
                    {
                        // try to get lock
                        if (Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Adding, sync) == sync)
                        {
                            try
                            {
                                var value = addValueFactory(key);
                                var page  = cabn.ReadyPage;

                                cabn.Buckets[page].Key   = key;
                                cabn.Buckets[page].Value = value;
                                cabn.ReadyPage           = -1;

                                frame.Links[index] = new Link { Id = cabn.Id, Positon = page };

                                syncs[index] = (int)RecordStatus.HasValue;

                                IncrementCount(data);

                                return value;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    // growing
                    else
                    {
                        var link = frame.Links[index];
                        ref var buck = ref data.Cabinets[link.Id].Buckets[link.Positon];

                        // check exist
                        if (comp.Equals(key, buck.Key))
                        {
                            if (Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync) == sync)
                            {
                                try
                                {
                                    var value = updateValueFactory(key, buck.Value);
                                    var page  = cabn.ReadyPage;

                                    cabn.Buckets[page].Key   = key;
                                    cabn.Buckets[page].Value = value;
                                    cabn.ReadyPage           = -1;

                                    frame.Links[index] = new Link { Id = cabn.Id, Positon = page };

                                    RemoveLink(data, link, cabn.Id);

                                    return value;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }

                        GrowTable(data);
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key does not already
        /// exist, or updates a key/value pair in the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key
        /// already exists.
        /// </summary>
        /// <param name="key">The key to be added or whose value should be updated</param>
        /// <param name="addValue">The value to be added for an absent key</param>
        /// <param name="updateValueFactory">The function used to generate a new value for an existing key based on
        /// the key's existing value</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="updateValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The new value for the key.  This will be either be the result of addValueFactory (if the key was
        /// absent) or the result of updateValueFactory (if the key was present).</returns>
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            if (updateValueFactory == null)
            {
                throw new ArgumentNullException(nameof(updateValueFactory));
            }

            unchecked
            {
                var data  = _data;
                var frame = data.Frame;
                var comp  = _keysComparer;
                var hash  = comp.GetHashCode(key) & 0x7fffffff;
                var cabn  = GetCabinet(data);

                PreparePage(cabn);

                // search empty space
                while (true)
                {
                    var syncs = frame.SyncTable;
                    var index = hash % frame.HashMaster;
                    var sync  = syncs[index];

                    if ((sync & (int)RecordStatus.Grown) != 0)
                    {
                        frame = Volatile.Read(ref data.GrowingFrame);
                        syncs = frame.SyncTable;
                        index = hash % frame.HashMaster;
                        sync  = syncs[index];
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.HasValue)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    if ((sync & (int)RecordStatus.HasValue) == 0)
                    {
                        // try to get lock
                        if (Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Adding, sync) == sync)
                        {
                            try
                            {
                                var page  = cabn.ReadyPage;

                                cabn.Buckets[page].Key   = key;
                                cabn.Buckets[page].Value = addValue;
                                cabn.ReadyPage           = -1;

                                frame.Links[index] = new Link { Id = cabn.Id, Positon = page };

                                syncs[index] = (int)RecordStatus.HasValue;

                                IncrementCount(data);

                                return addValue;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    // growing
                    else
                    {
                        var link = frame.Links[index];
                        ref var buck = ref data.Cabinets[link.Id].Buckets[link.Positon];

                        // check exist
                        if (comp.Equals(key, buck.Key))
                        {
                            if (Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync) == sync)
                            {
                                try
                                {
                                    var value = updateValueFactory(key, buck.Value);
                                    var page  = cabn.ReadyPage;

                                    cabn.Buckets[page].Key   = key;
                                    cabn.Buckets[page].Value = value;
                                    cabn.ReadyPage           = -1;

                                    frame.Links[index] = new Link { Id = cabn.Id, Positon = page };

                                    RemoveLink(data, link, cabn.Id);

                                    return value;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }

                        GrowTable(data);
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key does not already
        /// exist, or updates a key/value pair in the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key
        /// already exists.
        /// </summary>
        /// <param name="key">The key to be added or whose value should be updated</param>
        /// <param name="addValue">The value to be added for an absent key</param>
        /// <param name="updateValue">The new value for an existing key</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The new value for the key.  This will be either be the result of addValueFactory (if the key was
        /// absent) or the result of updateValueFactory (if the key was present).</returns>
        public TValue AddOrUpdate(TKey key, TValue addValue, TValue updateValue)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            var data  = _data;
            var frame = data.Frame;
            var comp  = _keysComparer;
            var hash  = comp.GetHashCode(key) & 0x7fffffff;
            var cabn  = GetCabinet(data);

            PreparePage(cabn);

            unchecked
            {
                // search empty space
                while (true)
                {
                    var syncs = frame.SyncTable;
                    var index = hash % frame.HashMaster;
                    var sync  = syncs[index];

                    if ((sync & (int)RecordStatus.Grown) != 0)
                    {
                        frame = Volatile.Read(ref data.GrowingFrame);
                        syncs = frame.SyncTable;
                        index = hash % frame.HashMaster;
                        sync  = syncs[index];
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.HasValue)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    // adding mode
                    if ((sync & (int)RecordStatus.HasValue) == 0)
                    {
                        if (Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Adding, sync) == sync)
                        {
                            try
                            {
                                var page = cabn.ReadyPage;

                                cabn.Buckets[page].Key   = key;
                                cabn.Buckets[page].Value = addValue;
                                cabn.ReadyPage           = -1;

                                frame.Links[index] = new Link { Id = cabn.Id, Positon = page };

                                syncs[index] = (int)RecordStatus.HasValue;

                                IncrementCount(data);

                                return addValue;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    else
                    {
                        var link = frame.Links[index];
                        ref var buck = ref data.Cabinets[link.Id].Buckets[link.Positon];

                        // update mode
                        if (comp.Equals(key, buck.Key))
                        {
                            // try to get lock
                            if (Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync) == sync)
                            {
                                try
                                {
                                    var page = cabn.ReadyPage;

                                    cabn.Buckets[page].Key   = key;
                                    cabn.Buckets[page].Value = updateValue;
                                    cabn.ReadyPage           = -1;

                                    frame.Links[index] = new Link { Id = cabn.Id, Positon = page };
                                    //Volatile.Write(ref frame.Links[index].Int64View, ((long)page << 32) + cabn.Id);

                                    RemoveLink(data, link, cabn.Id);

                                    return updateValue;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }

                        GrowTable(data);
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>Returns an enumerator that iterates through the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</summary>
        /// <returns>An enumerator for the <see cref="ConcurrentDictionary{TKey,TValue}"/>.</returns>
        /// <remarks>
        /// The enumerator returned from the dictionary is safe to use concurrently with
        /// reads and writes to the dictionary, however it does not represent a moment-in-time snapshot
        /// of the dictionary.  The contents exposed through the enumerator may contain modifications
        /// made to the dictionary after <see cref="GetEnumerator"/> was called.
        /// </remarks>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Copies the key and value pairs stored in the <see cref="ConcurrentDictionary{TKey,TValue}"/> to a
        /// new array.
        /// </summary>
        /// <returns>A new array containing a snapshot of key and value pairs copied from the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</returns>
        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            var index = 0;
            var arr = new KeyValuePair<TKey, TValue>[Count];

            // reads all values
            foreach (var current in this)
            {
                // grow array
                if (index == arr.Length)
                {
                    var len = Count;

                    if (len < arr.Length)
                    {
                        len = arr.Length + 10_000;
                    }
                    else
                    {
                        len += 10_000;
                    }

                    var tmp = new KeyValuePair<TKey, TValue>[len];

                    Array.Copy(arr, tmp, arr.Length);

                    arr = tmp;
                }

                arr[index++] = current;
            }

            // trim array
            if (index != arr.Length)
            {
                var tmp = new KeyValuePair<TKey, TValue>[index];

                Array.Copy(arr, tmp, index);

                arr = tmp;
            }

            return arr;
        }

        /// <summary>
        /// Removes all keys and values from the <see cref="ConcurrentDictionary{TKey,TValue}"/>.
        /// </summary>
        public void Clear()
        {
            var tmp = new HashTableData(_capacity, COUNTS_SIZE);

            // counts initialization
            for (var i = 0; i < tmp.Counts.Length; ++i)
            {
                tmp.Counts[i] = new ConcurrentDictionaryCounter();
            }

            // write empty data
            _data = tmp;
        }

        #endregion

        #region ' Private Area '

        // Expand table function
        private void GrowTable(HashTableData data)
        {
            if (data.SyncGrowing > 0)
            {
                return;
            }

            // get growing lock
            if (Interlocked.CompareExchange(ref data.SyncGrowing, 1, 0) != 0)
            {
                return;
            }

            unchecked
            {
                var cur_frame   = data.Frame;
                var mul         = GROW_MULTIPLIER;
                var new_master  = cur_frame.HashMaster;
                var cabinet     = GetCabinet(data);
                var comp        = _keysComparer;

                if (new_master == int.MaxValue)
                {
                    data.SyncGrowing = 0;

                    throw new OverflowException();
                }

                var size = cur_frame.HashMaster * (long)mul + 1;

                if (size > int.MaxValue)
                {
                    size = int.MaxValue;
                }

                new_master = (int)size;

                var new_frame = new HashTableDataFrame(new_master);

                Volatile.Write(ref data.GrowingFrame, new_frame);

                for (var i = 0; i < cur_frame.HashMaster; ++i)
                {
                    var sync = Volatile.Read(ref cur_frame.SyncTable[i]);

                    // set a bucket lock
                    if (
                            sync <= (int)RecordStatus.HasValue
                                            &&
                            Interlocked.CompareExchange(ref cur_frame.SyncTable[i], sync | (int)RecordStatus.Growing, sync) == sync
                       )
                    {
                        if ((sync & (int)RecordStatus.HasValue) != 0)
                        {
                            ref var link = ref cur_frame.Links[i];
                            ref var buck = ref data.Cabinets[link.Id].Buckets[link.Positon];

                            PreparePage(cabinet);

                            var page = cabinet.ReadyPage;

                            cabinet.Buckets[page].Key   = buck.Key;
                            cabinet.Buckets[page].Value = buck.Value;

                            var index = (comp.GetHashCode(buck.Key) & 0x7fffffff) % new_frame.HashMaster;

                            new_frame.SyncTable[index] = (int)RecordStatus.HasValue;
                            new_frame.Links[index]     = new Link { Id = cabinet.Id, Positon = page };

                            cabinet.ReadyPage = -1;

                            RemoveLink(data, link, cabinet.Id);
                        }

                        cur_frame.SyncTable[i] = (int)RecordStatus.Grown;
                    }
                    // wait another thread complate oparation
                    else
                    {
                        i--; 
                    }
                }

                // write new data frame
                data.Frame = new_frame;

                // unlock growing
                data.SyncGrowing = 0;

                PreparePage(cabinet);
            }
        }

        // Constructor initialization
        private void InitializeFromCollection(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            foreach (KeyValuePair<TKey, TValue> pair in collection)
            {
                if (pair.Key == null)
                {
                    ThrowKeyNullException();
                }

                if (!TryAdd(pair.Key, pair.Value))
                {
                    throw new ArgumentException(SR.ConcurrentDictionary_SourceContainsDuplicateKeys);
                }
            }
        }

        // Increaze Count
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IncrementCount(HashTableData data)
        {
            // A situation may arise when some threads only add, while others only delete.
            // Then a reasonable question may arise whether this will lead to overflow of
            // a variable of type long. To answer this question, it is necessary to make
            // calculations. As you know, long is 2 ^ 63. According to tests, the rate of
            // addition is about 30 million operations per second. Let's calculate how many
            // years it will take to overflow this variable.
            // years = 9223372036854775807 / (30_000_000 * 60 * 60 * 24 * 365) = 9740.
            // Almost 10 thousand years. In addition, in the application that uses the
            // hash table, there is another business logic that reduces the number of
            // operations per second. In general, I am sure that overflow can be considered
            // impossible. If in the future the core performance will increase significantly,
            // then it will be necessary to switch to using 128-bit variables.

            var id = t_id;
            var counts = data.Counts;

            if (id == 0 || id >= counts.Length)
            {
                id = t_id = Thread.CurrentThread.ManagedThreadId;

                if (id >= counts.Length)
                {
                    counts = CountsResize(data);
                }
            }

            counts[id].Count++;
        }

        // Decrement Count
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecrementCount(HashTableData data)
        {
            var id = t_id;
            var counts = data.Counts;

            if (id == 0 || id >= counts.Length)
            {
                id = t_id = Thread.CurrentThread.ManagedThreadId;

                if (id >= counts.Length)
                {
                    counts = CountsResize(data);
                }
            }

            counts[id].Count--;
        }

        // Function to increase the size of the array counts
        private ConcurrentDictionaryCounter[] CountsResize(HashTableData data)
        {
            var id = t_id;

            while (Interlocked.CompareExchange(ref data.SyncCounts, 1, 0) != 0)
            {
                while (Volatile.Read(ref data.SyncCounts) != 0)
                {
                    Thread.Yield();
                }

                var counts = Volatile.Read(ref data.Counts);

                if (id < counts.Length)
                {
                    return counts;
                }
            }

            var len = data.Counts.Length;

            if (id > len)
            {
                len = id;
            }

            var tmp_counts = new ConcurrentDictionaryCounter[len * 2];

            Array.Copy(data.Counts, tmp_counts, data.Counts.Length);

            // fill with empty counts
            for (var i = data.Counts.Length; i < tmp_counts.Length; ++i)
            {
                tmp_counts[i] = new ConcurrentDictionaryCounter();
            }

            // write new counts link
            data.Counts = tmp_counts;

            // unlock counts
            data.SyncCounts = 0;

            return tmp_counts;
        }

        // 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Cabinet GetCabinet(HashTableData data)
        {
            var id = t_id;
            var cabinets = data.Cabinets;

            if (id == 0 || id >= cabinets.Length)
            {
                id = t_id = Thread.CurrentThread.ManagedThreadId;

                if (id >= cabinets.Length)
                {
                    cabinets = ResizeCabinets(data);
                }
            }

            return cabinets[id];
        }

        // 
        private Cabinet[] ResizeCabinets(HashTableData data)
        {
            var id = t_id;

            while (Interlocked.CompareExchange(ref data.SyncCabinets, 1, 0) != 0)
            {
                while (Volatile.Read(ref data.SyncCabinets) != 0)
                {
                    Thread.Yield();
                }

                var сabinets = Volatile.Read(ref data.Cabinets);

                if (id < сabinets.Length)
                {
                    return сabinets;
                }
            }

            var len = data.Cabinets.Length;

            if (id > len)
            {
                len = id;
            }

            var tmp_Cabinets = new Cabinet[len * 2];

            Array.Copy(data.Cabinets, tmp_Cabinets, data.Cabinets.Length);

            // fill with empty Cabinets
            for (var i = data.Cabinets.Length; i < tmp_Cabinets.Length; ++i)
            {
                tmp_Cabinets[i] = new Cabinet(i);
            }

            // write new Cabinets link
            data.Cabinets = tmp_Cabinets;

            // unlock Cabinets
            data.SyncCabinets = 0;

            return tmp_Cabinets;
        }
        
        // 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PreparePage(Cabinet cabinet)
        {
            if (cabinet.ReadyPage < 0)
            {
                // return not used elememnt
                if (cabinet.Positon < cabinet.Buckets.Length - WRITER_DELAY)
                {
                    cabinet.ReadyPage = cabinet.Positon++;

                    return;
                }

                var guest = cabinet.Current ?? cabinet.GuestsList;

                // return removed element
                if (guest != null)
                {
                    var seg = guest.MessageBuffer.Reader;

                    if (seg.ReaderPosition != seg.WriterPosition)
                    {
                        cabinet.ReadyPage = seg.Messages[seg.ReaderPosition];

                        if (seg.ReaderPosition < seg.Messages.Length - 1)
                        {
                            seg.ReaderPosition++;
                        }
                        else
                        {
                            seg.ReaderPosition = 0;
                        }

                        return;
                    }
                    else
                    {
                        var cur_guest = guest;

                        while (true)
                        {
                            seg = cur_guest.MessageBuffer.Reader;

                            var cur_seg = seg;

                            while (true)
                            {
                                // validation reader position
                                if (cur_seg.ReaderPosition != cur_seg.WriterPosition)
                                {
                                    cabinet.ReadyPage = cur_seg.Messages[cur_seg.ReaderPosition];

                                    // set position to next element for reading
                                    if (cur_seg.ReaderPosition < cur_seg.Messages.Length - 1)
                                    {
                                        cur_seg.ReaderPosition++;
                                    }
                                    else
                                    {
                                        cur_seg.ReaderPosition = 0;
                                    }

                                    // save current guest
                                    cabinet.Current = cur_guest;

                                    // save current segment
                                    cur_guest.MessageBuffer.Reader = cur_seg;

                                    return;
                                }

                                // swap to next segment
                                cur_seg = cur_seg.Next;

                                if (cur_seg == null)
                                {
                                    cur_seg = cur_guest.MessageBuffer.Root;
                                }

                                if (cur_seg == seg)
                                {
                                    break;
                                }
                            }

                            // swap to next guest
                            cur_guest = cur_guest.Next;

                            if (cur_guest == null)
                            {
                                cur_guest = cabinet.GuestsList;
                            }

                            if (cur_guest == guest)
                            {
                                break;
                            }
                        }
                    }
                }

                // grow buckets
                if (cabinet.Buckets.Length == int.MaxValue)
                {
                    throw new OverflowException();
                }

                var len = cabinet.Buckets.Length * 2d;

                if (len > int.MaxValue)
                {
                    len = int.MaxValue;
                }

                var tmp = new Bucket[(int)len];

                Array.Copy(cabinet.Buckets, tmp, cabinet.Buckets.Length);

                cabinet.Buckets   = tmp;
                cabinet.ReadyPage = cabinet.Positon++;
            }
        }

        // 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveLink(HashTableData data, Link link, int guest_id)
        {
            if (link.Id == 0)
            {
                return;
            }

            Guest guest;

            var owner = data.Cabinets[link.Id];
            var table = owner.GuestsTable;

            // grow guests table
            if (guest_id >= table.Length)
            {
                try
                {
                    while (Interlocked.CompareExchange(ref owner.Sync, guest_id, 0) != 0)
                    {
                    }

                    table = Volatile.Read(ref owner.GuestsTable);

                    if (guest_id >= table.Length)
                    {
                        var len = (guest_id / 4 + 1) * 4;
                        var tmp = new Guest[len];

                        Array.Copy(table, tmp, table.Length);

                        owner.GuestsTable = table = tmp;
                    }
                }
                finally
                {
                    owner.Sync = 0;
                }
            }

            // setup new guest
            if (table[guest_id] == null)
            {
                guest = new Guest(guest_id);

                try
                {
                    while (Interlocked.CompareExchange(ref owner.Sync, guest_id, 0) != 0)
                    {
                    }

                    guest.Next = Volatile.Read(ref owner.GuestsList);

                    owner.GuestsList = guest;

                    table[guest_id] = guest;

                }
                finally
                {
                    owner.Sync = 0;
                }
            }

            guest = table[guest_id];

            // write to delay buffer
            if (guest.DelayPosition == WRITER_DELAY)
            {
                guest.DelayPosition = 0;
                guest.DelayBufferReady = true;
            }

            if (!guest.DelayBufferReady)
            {
                guest.DelayBuffer[guest.DelayPosition++] = link.Positon;

                return;
            }

            var pos = guest.DelayBuffer[guest.DelayPosition];

            guest.DelayBuffer[guest.DelayPosition++] = link.Positon;

            var seg = guest.MessageBuffer.Writer;

            // write message
            if (seg.WriterPosition != seg.ReaderPosition - 1)
            {
                if (seg.WriterPosition == seg.Messages.Length - 1)
                {
                    if (seg.ReaderPosition > 0)
                    {
                        seg.Messages[seg.WriterPosition] = pos;
                        seg.WriterPosition = 0;

                        return;
                    }
                }
                else
                {
                    seg.Messages[seg.WriterPosition++] = pos;

                    return;
                }
            }

            var cur  = seg.Next;
            var last = guest.MessageBuffer.Root;

            // search for segment with free cells
            while (true)
            {
                if (cur == null)
                {
                    cur = guest.MessageBuffer.Root;
                }

                if (cur == seg)
                {
                    break;
                }

                if (cur.WriterPosition != cur.ReaderPosition - 1)
                {
                    if (cur.WriterPosition == cur.Messages.Length - 1)
                    {
                        if (cur.ReaderPosition > 0)
                        {
                            cur.Messages[cur.WriterPosition] = pos;
                            cur.WriterPosition = 0;

                            guest.MessageBuffer.Writer = cur;

                            return;
                        }
                    }
                    else
                    {
                        cur.Messages[cur.WriterPosition++] = pos;

                        guest.MessageBuffer.Writer = cur;

                        return;
                    }
                }

                if (cur.Next == null)
                {
                    last = cur;
                }

                cur = cur.Next;
            }

            // create new segment
            var new_seg = new ConcurrentDictionaryCycleBufferSegment(last.Messages.Length * 2);

            new_seg.WriterPosition = 1;
            new_seg.Messages[0]    = pos;

            last.Next = new_seg;

            guest.MessageBuffer.Writer = new_seg;

            // Remove readed segments
            // It makes no reason to keep smaller segments. This reduces the
            // number of iterations for the read thread.
            ref var node = ref guest.MessageBuffer.Root;

            while (node != null)
            {
                if (node == guest.MessageBuffer.Root && node.Next == null)
                {
                    break;
                }

                if (node.ReaderPosition == node.WriterPosition)
                {
                    node = node.Next;
                }
                else
                {
                    node = ref node.Next;
                }
            }
        }

        #endregion

        #region ' IEnumerable Members '

        /// <summary>Returns an enumerator that iterates through the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</summary>
        /// <returns>An enumerator for the <see cref="ConcurrentDictionary{TKey,TValue}"/>.</returns>
        /// <remarks>
        /// The enumerator returned from the dictionary is safe to use concurrently with
        /// reads and writes to the dictionary, however it does not represent a moment-in-time snapshot
        /// of the dictionary.  The contents exposed through the enumerator may contain modifications
        /// made to the dictionary after <see cref="GetEnumerator"/> was called.
        /// </remarks>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>Returns an enumerator that iterates through the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</summary>
        /// <returns>An enumerator for the <see cref="ConcurrentDictionary{TKey,TValue}"/>.</returns>
        /// <remarks>
        /// The enumerator returned from the dictionary is safe to use concurrently with
        /// reads and writes to the dictionary, however it does not represent a moment-in-time snapshot
        /// of the dictionary.  The contents exposed through the enumerator may contain modifications
        /// made to the dictionary after <see cref="GetEnumerator"/> was called.
        /// </remarks>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region ' IDictionary Members '

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The object to use as the key.</param>
        /// <param name="value">The object to use as the value.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <exception cref="System.ArgumentException">
        /// <paramref name="key"/> is of a type that is not assignable to the key type <typeparamref
        /// name="TKey"/> of the <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>. -or-
        /// <paramref name="value"/> is of a type that is not assignable to <typeparamref name="TValue"/>,
        /// the type of values in the <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// -or- A value with the same key already exists in the <see
        /// cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </exception>
        void IDictionary.Add(object key, object? value)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            if (!(key is TKey))
            {
                throw new ArgumentException(SR.ConcurrentDictionary_TypeOfKeyIncorrect);
            }

            ThrowIfInvalidObjectValue(value);

            ((IDictionary<TKey, TValue>)this).Add((TKey)key, (TValue)value!);
        }

        /// <summary>
        /// Gets whether the <see cref="System.Collections.Generic.IDictionary{TKey,TValue}"/> contains an
        /// element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see
        /// cref="System.Collections.Generic.IDictionary{TKey,TValue}"/>.</param>
        /// <returns>true if the <see cref="System.Collections.Generic.IDictionary{TKey,TValue}"/> contains
        /// an element with the specified key; otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException"> <paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        bool IDictionary.Contains(object key)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            return (key is TKey _key) && ContainsKey(_key);
        }

        /// <summary>Provides an <see cref="System.Collections.IDictionaryEnumerator"/> for the
        /// <see cref="System.Collections.IDictionary"/>.</summary>
        /// <returns>An <see cref="System.Collections.IDictionaryEnumerator"/> for the <see
        /// cref="System.Collections.Generic.IDictionary{TKey,TValue}"/>.</returns>
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new DictionaryEnumerator(this);
        }

        /// <summary>
        /// Gets a value indicating whether the <see
        /// cref="System.Collections.IDictionary"/> has a fixed size.
        /// </summary>
        /// <value>true if the <see cref="System.Collections.IDictionary"/> has a
        /// fixed size; otherwise, false. For <see
        /// cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>, this property always
        /// returns false.</value>
        bool IDictionary.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see
        /// cref="System.Collections.IDictionary"/> is read-only.
        /// </summary>
        /// <value>true if the <see cref="System.Collections.IDictionary"/> is
        /// read-only; otherwise, false. For <see
        /// cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>, this property always
        /// returns false.</value>
        bool IDictionary.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see
        /// cref="System.Collections.IDictionary"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        void IDictionary.Remove(object key)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            if (key is TKey _key)
            {
                TryRemove(_key, out TValue throwAwayValue);
            }
        }

        /// <summary>
        /// Gets a collection containing the keys in the <see
        /// cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="System.Collections.Generic.ICollection{TKey}"/> containing the keys in the
        /// <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.</value>
        public ICollection<TKey> Keys
        {
            get
            {
                return GetKeys();
            }
        }

        /// <summary>
        /// Gets a collection containing the values in the <see
        /// cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="System.Collections.Generic.ICollection{TValue}"/> containing the values in
        /// the
        /// <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.</value>
        public ICollection<TValue> Values
        {
            get
            {
                return GetValues();
            }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <value>The value associated with the specified key, or a null reference (Nothing in Visual Basic)
        /// if <paramref name="key"/> is not in the dictionary or <paramref name="key"/> is of a type that is
        /// not assignable to the key type <typeparamref name="TKey"/> of the <see
        /// cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>.</value>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentException">
        /// A value is being assigned, and <paramref name="key"/> is of a type that is not assignable to the
        /// key type <typeparamref name="TKey"/> of the <see
        /// cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>. -or- A value is being
        /// assigned, and <paramref name="key"/> is of a type that is not assignable to the value type
        /// <typeparamref name="TValue"/> of the <see
        /// cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>
        /// </exception>
        object? IDictionary.this[object key]
        {
            get
            {
                if (key == null)
                {
                    ThrowKeyNullException();
                }

                if (key is TKey _key && TryGetValue(_key, out TValue value))
                {
                    return value;
                }

                return null;
            }
            set
            {
                if (key == null)
                {
                    ThrowKeyNullException();
                }

                if (!(key is TKey))
                {
                    throw new ArgumentException(SR.ConcurrentDictionary_TypeOfKeyIncorrect);
                }

                ThrowIfInvalidObjectValue(value);

                this[(TKey)key] = (TValue)value;
            }
        }

        #endregion

        #region ' IDictionary<TKey,TValue> members '

        /// <summary>
        /// Adds the specified key and value to the <see
        /// cref="System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <exception cref="System.ArgumentException">
        /// An element with the same key already exists in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</exception>
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            if (!TryAdd(key, value))
            {
                throw new ArgumentException(SR.ConcurrentDictionary_KeyAlreadyExisted);
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see
        /// cref="System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>true if the element is successfully remove; otherwise false. This method also returns
        /// false if
        /// <paramref name="key"/> was not found in the original <see
        /// cref="System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            return TryRemove(key, out TValue throwAwayValue);
        }

        /// <summary>
        /// Gets an <see cref="System.Collections.Generic.IEnumerable{TKey}"/> containing the keys of
        /// the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="System.Collections.Generic.IEnumerable{TKey}"/> containing the keys of
        /// the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.</value>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            get
            {
                return GetKeys();
            }
        }

        /// <summary>
        /// Gets an <see cref="System.Collections.Generic.IEnumerable{TValue}"/> containing the values
        /// in the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="System.Collections.Generic.IEnumerable{TValue}"/> containing the
        /// values in the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.</value>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            get
            {
                return GetValues();
            }
        }

        #endregion

        #region ' ICollection Members '

        /// <summary>
        /// Copies the elements of the <see cref="System.Collections.ICollection"/> to an array, starting
        /// at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from
        /// the <see cref="System.Collections.ICollection"/>. The array must have zero-based
        /// indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="array"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// 0.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="index"/> is equal to or greater than
        /// the length of the <paramref name="array"/>. -or- The number of elements in the source <see
        /// cref="System.Collections.ICollection"/>
        /// is greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>.</exception>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ConcurrentDictionary_IndexIsNegative);
            }

            var offset = 0;
            var len = array.Length - index;

            // +++++++++++++++++++++++++++++++++
            //
            // The following condition has been set to pass some tests. However,
            // it is not entirely successful. Let's look at a two-thread scenario.
            //
            // var dict = new ConcurrentDictionary<int, int>();
            //
            // thread #1: dict.TryAdd(1, -1);
            // thread #1: var count = dict.Count;
            // thread #1: var array = new int[count];
            // thread #2: dict.TryAdd(2, -2);
            // thread #1: dict.CopyTo(array, 0);
            //
            // Such a scenario is guaranteed to lead to an exception that makes no sense.
            // I recommend commented-out condition instead.

            var count = Count;

            if (array.Length - count < index)
            {
                throw new ArgumentException(SR.ConcurrentDictionary_ArrayNotLargeEnough);
            }

            //if (index >= array.Length)
            //{
            //    throw new ArgumentException(SR.ConcurrentDictionary_ArrayNotLargeEnough);
            //}

            // +++++++++++++++++++++++++++++++++


            // To be consistent with the behavior of ICollection.CopyTo() in Dictionary<TKey,TValue>,
            // we recognize three types of target arrays:
            //    - an array of KeyValuePair<TKey, TValue> structs
            //    - an array of DictionaryEntry structs
            //    - an array of objects

            if (array is KeyValuePair<TKey, TValue>[] pairs)
            {
                foreach (var current in this)
                {
                    if (offset == len)
                    {
                        break;
                    }

                    pairs[index + offset++] = current;
                }

                return;
            }

            if (array is DictionaryEntry[] entries)
            {
                foreach (var current in this)
                {
                    if (offset == len)
                    {
                        break;
                    }

                    entries[index + offset++] = new DictionaryEntry { Key = current.Key, Value = current.Value };
                }

                return;
            }

            if (array is object[] objects)
            {
                foreach (var current in this)
                {
                    if (offset == len)
                    {
                        break;
                    }

                    objects[index + offset++] = current;
                }

                return;
            }

            if (array is TKey[] keys)
            {
                foreach (var current in this)
                {
                    if (offset == len)
                    {
                        break;
                    }

                    keys[index + offset++] = current.Key;
                }

                return;
            }

            if (array is TValue[] values)
            {
                foreach (var current in this)
                {
                    if (offset == len)
                    {
                        break;
                    }

                    values[index + offset++] = current.Value;
                }

                return;
            }

            throw new ArgumentException(SR.ConcurrentDictionary_ArrayIncorrectType, nameof(array));
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="System.Collections.ICollection"/> is
        /// synchronized with the SyncRoot.
        /// </summary>
        /// <value>true if access to the <see cref="System.Collections.ICollection"/> is synchronized
        /// (thread safe); otherwise, false. For <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>, this property always
        /// returns false.</value>
        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see
        /// cref="System.Collections.ICollection"/>. This property is not supported.
        /// </summary>
        /// <exception cref="System.NotSupportedException">The SyncRoot property is not supported.</exception>
        object ICollection.SyncRoot
        {
            get
            {
                throw new NotSupportedException(SR.ConcurrentCollection_SyncRoot_NotSupported);
            }
        }

        /// <summary>
        /// Gets an <see cref="System.Collections.ICollection"/> containing the keys of the <see
        /// cref="System.Collections.IDictionary"/>.
        /// </summary>
        /// <value>An <see cref="System.Collections.ICollection"/> containing the keys of the <see
        /// cref="System.Collections.IDictionary"/>.</value>
        ICollection IDictionary.Keys
        {
            get
            {
                return GetKeys();
            }
        }

        /// <summary>
        /// Gets an <see cref="System.Collections.ICollection"/> containing the values in the <see
        /// cref="System.Collections.IDictionary"/>.
        /// </summary>
        /// <value>An <see cref="System.Collections.ICollection"/> containing the values in the <see
        /// cref="System.Collections.IDictionary"/>.</value>
        ICollection IDictionary.Values
        {
            get
            {
                return GetValues();
            }
        }

        #endregion

        #region ' ICollection<KeyValuePair<TKey,TValue>> Members '

        /// <summary>
        /// Adds the specified value to the <see cref="System.Collections.Generic.ICollection{TValue}"/>
        /// with the specified key.
        /// </summary>
        /// <param name="keyValuePair">The <see cref="System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// structure representing the key and value to add to the <see
        /// cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="keyValuePair"/> of <paramref
        /// name="keyValuePair"/> is null.</exception>
        /// <exception cref="System.OverflowException">The <see
        /// cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>
        /// contains too many elements.</exception>
        /// <exception cref="System.ArgumentException">An element with the same key already exists in the
        /// <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/></exception>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            ((IDictionary<TKey, TValue>)this).Add(keyValuePair.Key, keyValuePair.Value);
        }

        /// <summary>
        /// Determines whether the <see cref="System.Collections.Generic.ICollection{T}"/>
        /// contains a specific key and value.
        /// </summary>
        /// <param name="keyValuePair">The <see cref="System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// structure to locate in the <see
        /// cref="System.Collections.Generic.ICollection{TValue}"/>.</param>
        /// <returns>true if the <paramref name="keyValuePair"/> is found in the <see
        /// cref="System.Collections.Generic.ICollection{T}"/>; otherwise, false.</returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            if (TryGetValue(keyValuePair.Key, out TValue value))
            {
                return _valuesComparer.Equals(value, keyValuePair.Value);
            }

            return false;
        }

        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only.
        /// </summary>
        /// <value>true if the <see cref="System.Collections.Generic.ICollection{T}"/> is
        /// read-only; otherwise, false. For <see
        /// cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>, this property always returns
        /// false.</value>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Removes a key and value from the dictionary.
        /// </summary>
        /// <param name="keyValuePair">The <see
        /// cref="System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// structure representing the key and value to remove from the <see
        /// cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.</param>
        /// <returns>true if the key and value represented by <paramref name="keyValuePair"/> is successfully
        /// found and removed; otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException">The Key property of <paramref
        /// name="keyValuePair"/> is a null reference (Nothing in Visual Basic).</exception>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            return TryRemove(keyValuePair);
        }

        /// <summary>
        /// Copies the elements of the <see cref="System.Collections.Generic.ICollection{T}"/> to an array of
        /// type <see cref="System.Collections.Generic.KeyValuePair{TKey,TValue}"/>, starting at the
        /// specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional array of type <see
        /// cref="System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// that is the destination of the <see
        /// cref="System.Collections.Generic.KeyValuePair{TKey,TValue}"/> elements copied from the <see
        /// cref="System.Collections.ICollection"/>. The array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="array"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// 0.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="index"/> is equal to or greater than
        /// the length of the <paramref name="array"/>. -or- The number of elements in the source <see
        /// cref="System.Collections.ICollection"/>
        /// is greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>.</exception>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        #endregion

        #region ' Structures '

        // Reflects the current status of each basket.
        // Warning: When changing, carefully look at the use in more><less conditions.
        private enum RecordStatus
        {
            Empty     = 00,
            HasValue  = 01,
            Adding    = 02,
            Removing  = 04,
            Updating  = 08,
            Growing   = 16,
            Grown     = 32
        }

        /// <summary>
        /// In a hashtable, there is a need to replace multiple references and some
        /// data atomically.Since in .Net the link changes atomically, this wrapper
        /// class exists for these purposes.
        /// </summary>
        private sealed class HashTableData
        {
            public HashTableData(int hashMaster, int threads)
            {
                Frame        = new HashTableDataFrame(hashMaster);
                Counts       = new ConcurrentDictionaryCounter[threads];
                Cabinets     = new Cabinet[threads];
                GrowingFrame = null;

                for (var i = 0; i < threads; ++i)
                {
                    Cabinets[i] = new Cabinet(i);
                    Counts[i]   = new ConcurrentDictionaryCounter();
                }
            }

            public HashTableData(int hashMaster) : this(hashMaster, 0)
            {
            }

            // To synchronize threads when growing table
            public int SyncGrowing;

            // To synchronize threads when expanding the counters array
            public int SyncCounts;

            // To synchronize threads when expanding the cabinets array
            public int SyncCabinets;

            // Current data frame
            public HashTableDataFrame Frame;

            // Datа frame in the process of growth.
            public HashTableDataFrame GrowingFrame;

            // Array of counters
            public ConcurrentDictionaryCounter[] Counts;

            // 
            public Cabinet[] Cabinets;
        }

        /// <summary>
        /// A data frame is always created when a table grows.
        /// </summary>
        private sealed class HashTableDataFrame
        {
            public HashTableDataFrame(int hashMaster)
            {
                HashMaster = hashMaster;
                SyncTable  = new int[hashMaster];
                Links      = new Link[hashMaster];
            }

            // Divider for hash function
            public readonly int HashMaster;

            // State array. Used to synchronize threads. See RecordStatus
            public readonly int[] SyncTable;

            // 
            public readonly Link[] Links;
        }

        // 
        private sealed class Cabinet
        {
            public Cabinet(int id)
            {
                Id          = id;
                ReadyPage   = -1;
                Positon     = 0;
                GuestsTable = new Guest[0];
                Buckets     = new Bucket[WRITER_DELAY * 4];
           }

            // owner id
            public readonly int Id;
            // Prepared page for a new record
            public int ReadyPage;
            // linked list of guests (convenient for the owner)
            public Guest GuestsList;
            // by id table of guests (convenient for the guest)
            public Guest[] GuestsTable;
            // current guest the owner is working with
            public Guest Current;
            // for synchronization threads during initialization
            public int Sync;
            // current position in Buckets array
            public int Positon;
            // Array of buckets
            public Bucket[] Buckets;
        }

        // 
        private sealed class Guest
        {
            public Guest(int id)
            {
                Id               = id;
                Next             = null;
                MessageBuffer    = new ConcurrentDictionaryCycleBuffer(CYCLE_BUFFER_SEGMENT_SIZE);
                DelayBuffer      = new int[WRITER_DELAY];
                DelayBufferReady = false;
            }

            // guest id
            public int Id;
            // next guest
            public Guest Next;
            // cycle buffer
            public ConcurrentDictionaryCycleBuffer MessageBuffer;
            //
            public bool DelayBufferReady;
            //
            public int DelayPosition;
            // 
            public int[] DelayBuffer;
        }

        //
        [DebuggerDisplay("{Key}/{Value}")]
        private struct Bucket
        {
            public TKey     Key;
            public TValue   Value;
        }

        /// <summary>
        /// A private class that implements the ability to enumerate.
        /// </summary>
        private class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            // owner dictionary
            private readonly ConcurrentDictionary<TKey, TValue> _dictionary;
            // bucket index
            private int _index;

            // constructor
            public Enumerator(ConcurrentDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;

                Reset();
            }

            // get next value
            public bool MoveNext()
            {
                while (true)
                {
                    var data  = Volatile.Read(ref _dictionary._data);
                    var frame = data.Frame;

                    if (_index >= frame.HashMaster)
                    {
                        return false;
                    }

                    var sync = frame.SyncTable[_index];

                    // skip if empty
                    if (sync == (int)RecordStatus.Empty)
                    {
                        _index++;

                        continue;
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.HasValue)
                    {
                        continue;
                    }

                    ref var link  = ref frame.Links[_index];
                    ref var buck  = ref data.Cabinets[link.Id].Buckets[link.Positon];

                    Current = new KeyValuePair<TKey, TValue>(buck.Key, buck.Value);

                    _index++;

                    return true;
                }
            }

            // reset for reuse
            public void Reset()
            {
                _index = 0;
                Current = default;
            }

            // current value
            public KeyValuePair<TKey, TValue> Current { get; private set; }

            // current value
            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            // free up resources
            public void Dispose()
            {
            }
        }

        /// <summary>
        /// A private class to represent enumeration over the dictionary that implements the
        /// IDictionaryEnumerator interface.
        /// </summary>
        private class DictionaryEnumerator : IDictionaryEnumerator
        {
            // Enumerator over the dictionary.
            private readonly IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

            internal DictionaryEnumerator(ConcurrentDictionary<TKey, TValue> dictionary)
            {
                _enumerator = dictionary.GetEnumerator();
            }

            public DictionaryEntry Entry
            {
                get
                {
                    return new DictionaryEntry(_enumerator.Current.Key, _enumerator.Current.Value);
                }
            }

            public object Key
            {
                get
                {
                    return _enumerator.Current.Key;
                }
            }

            public object? Value
            {
                get
                {
                    return _enumerator.Current.Value;
                }
            }

            public object Current
            {
                get
                {
                    return Entry;
                }
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Reset();
            }
        }

        #endregion

        #region ' Helpers '

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        private ReadOnlyCollection<TKey> GetKeys()
        {
            var keys = new List<TKey>(Count);

            foreach (var current in this)
            {
                keys.Add(current.Key);
            }

            return new ReadOnlyCollection<TKey>(keys);
        }

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
        private ReadOnlyCollection<TValue> GetValues()
        {
            var vals = new List<TValue>(Count);

            foreach (var current in this)
            {
                vals.Add(current.Value);
            }

            return new ReadOnlyCollection<TValue>(vals);
        }

        // These exception throwing sites have been extracted into their own NoInlining methods
        // as these are uncommonly needed and when inlined are observed to prevent the inlining
        // of important methods like TryGetValue and ContainsKey.
        [DoesNotReturn]
        private static void ThrowKeyNotFoundException(object key)
        {
            throw new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key.ToString()));
        }

        //
        [DoesNotReturn]
        private static void ThrowKeyNullException()
        {
            throw new ArgumentNullException("key");
        }

        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowIfInvalidObjectValue(object? value)
        {
            if (value != null)
            {
                if (!(value is TValue))
                {
                    ThrowValueNullException();
                }
            }
            else if (default(TValue) != null)
            {
                ThrowValueNullException();
            }
        }

        //
        private static void ThrowValueNullException()
        {
            throw new ArgumentException(SR.ConcurrentDictionary_TypeOfValueIncorrect);
        }

        #endregion
    }

    // 
    [DebuggerDisplay("Count = {Count}")]
    [StructLayout(LayoutKind.Explicit, Size = PaddingHelpers.CACHE_LINE_SIZE)]
    internal class ConcurrentDictionaryCounter
    {
        [FieldOffset(0)]
        public long Count;
    }

    // 
    //[StructLayout(LayoutKind.Explicit, Size = PaddingHelpers.CACHE_LINE_SIZE * 4)]
    internal class ConcurrentDictionaryCycleBuffer
    {
        public ConcurrentDictionaryCycleBuffer(int capacity)
        {
            Root = new ConcurrentDictionaryCycleBufferSegment(capacity);
            Reader = Root;
            Writer = Root;
        }

        // the first segment
        public ConcurrentDictionaryCycleBufferSegment Root;
        // current reader segment
        public ConcurrentDictionaryCycleBufferSegment Reader;
        // current writer segment
        public ConcurrentDictionaryCycleBufferSegment Writer;
    }

    // 
    //[StructLayout(LayoutKind.Explicit, Size = PaddingHelpers.CACHE_LINE_SIZE * 4)]
    internal class ConcurrentDictionaryCycleBufferSegment
    {
        public ConcurrentDictionaryCycleBufferSegment(int capacity)
        {
            Messages = new int[capacity];
        }

        //[FieldOffset(PaddingHelpers.CACHE_LINE_SIZE * 1)]
        public int ReaderPosition;

        //[FieldOffset(PaddingHelpers.CACHE_LINE_SIZE * 2)]
        public int WriterPosition;

        //[FieldOffset(PaddingHelpers.CACHE_LINE_SIZE * 3)]
        public int[] Messages;

        //[FieldOffset(PaddingHelpers.CACHE_LINE_SIZE * 4)]
        public ConcurrentDictionaryCycleBufferSegment Next;
    }

    /// <summary>
    ///     A size greater than or equal to the size of the most common CPU cache lines.
    /// </summary>
    internal static class PaddingHelpers
    {
#if TARGET_ARM64
        internal const int CACHE_LINE_SIZE = 128;
#elif TARGET_32BIT
        internal const int CACHE_LINE_SIZE = 32;
#else
        internal const int CACHE_LINE_SIZE = 64;
#endif
    }

    internal sealed class IDictionaryDebugView<K, V> where K : notnull
    {
        private readonly IDictionary<K, V> _dictionary;

        public IDictionaryDebugView(IDictionary<K, V> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            _dictionary = dictionary;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<K, V>[] Items
        {
            get
            {
                KeyValuePair<K, V>[] items = new KeyValuePair<K, V>[_dictionary.Count];
                _dictionary.CopyTo(items, 0);
                return items;
            }
        }
    }

    // 
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal struct Link
    {
        // owner id
        [FieldOffset(0)]
        public int Id;
        // entry index
        [FieldOffset(4)]
        public int Positon;
        [FieldOffset(0)]
        public long Int64View;
    }
}
