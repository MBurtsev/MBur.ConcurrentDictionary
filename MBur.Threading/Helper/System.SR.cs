using System.Runtime.Serialization;

namespace FxResources.System.Collections.Concurrent
{
    internal static class SR { }
}
namespace System
{
    // For compatibility with .Net 5.+
    internal static partial class SR
    {
        private static global::System.Resources.ResourceManager s_resourceManager;
        internal static global::System.Resources.ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new global::System.Resources.ResourceManager(typeof(FxResources.System.Collections.Concurrent.SR)));

        /// <summary>CompleteAdding may not be used concurrently with additions to the collection.</summary>
        internal static string @BlockingCollection_Add_ConcurrentCompleteAdd => GetResourceString("BlockingCollection_Add_ConcurrentCompleteAdd", @"CompleteAdding may not be used concurrently with additions to the collection.");

        /// <summary>The underlying collection didn't accept the item.</summary>
        internal static string @BlockingCollection_Add_Failed => GetResourceString("BlockingCollection_Add_Failed", @"The underlying collection didn't accept the item.");
        /// <summary>At least one of the specified collections is marked as complete with regards to additions.</summary>
        internal static string @BlockingCollection_CantAddAnyWhenCompleted => GetResourceString("BlockingCollection_CantAddAnyWhenCompleted", @"At least one of the specified collections is marked as complete with regards to additions.");
        /// <summary>All collections are marked as complete with regards to additions.</summary>
        internal static string @BlockingCollection_CantTakeAnyWhenAllDone => GetResourceString("BlockingCollection_CantTakeAnyWhenAllDone", @"All collections are marked as complete with regards to additions.");
        /// <summary>The collection argument is empty and has been marked as complete with regards to additions.</summary>
        internal static string @BlockingCollection_CantTakeWhenDone => GetResourceString("BlockingCollection_CantTakeWhenDone", @"The collection argument is empty and has been marked as complete with regards to additions.");
        /// <summary>The collection has been marked as complete with regards to additions.</summary>
        internal static string @BlockingCollection_Completed => GetResourceString("BlockingCollection_Completed", @"The collection has been marked as complete with regards to additions.");
        /// <summary>The array argument is of the incorrect type.</summary>
        internal static string @BlockingCollection_CopyTo_IncorrectType => GetResourceString("BlockingCollection_CopyTo_IncorrectType", @"The array argument is of the incorrect type.");
        /// <summary>The array argument is multidimensional.</summary>
        internal static string @BlockingCollection_CopyTo_MultiDim => GetResourceString("BlockingCollection_CopyTo_MultiDim", @"The array argument is multidimensional.");
        /// <summary>The index argument must be greater than or equal zero.</summary>
        internal static string @BlockingCollection_CopyTo_NonNegative => GetResourceString("BlockingCollection_CopyTo_NonNegative", @"The index argument must be greater than or equal zero.");
        /// <summary>The number of elements in the collection is greater than the available space from index to the end of the destination array.</summary>
        internal static string @Collection_CopyTo_TooManyElems => GetResourceString("Collection_CopyTo_TooManyElems", @"The number of elements in the collection is greater than the available space from index to the end of the destination array.");
        /// <summary>The boundedCapacity argument must be positive.</summary>
        internal static string @BlockingCollection_ctor_BoundedCapacityRange => GetResourceString("BlockingCollection_ctor_BoundedCapacityRange", @"The boundedCapacity argument must be positive.");
        /// <summary>The collection argument contains more items than are allowed by the boundedCapacity.</summary>
        internal static string @BlockingCollection_ctor_CountMoreThanCapacity => GetResourceString("BlockingCollection_ctor_CountMoreThanCapacity", @"The collection argument contains more items than are allowed by the boundedCapacity.");
        /// <summary>The collection has been disposed.</summary>
        internal static string @BlockingCollection_Disposed => GetResourceString("BlockingCollection_Disposed", @"The collection has been disposed.");
        /// <summary>The underlying collection was modified from outside of the BlockingCollection&lt;T&gt;.</summary>
        internal static string @BlockingCollection_Take_CollectionModified => GetResourceString("BlockingCollection_Take_CollectionModified", @"The underlying collection was modified from outside of the BlockingCollection<T>.");
        /// <summary>The specified timeout must represent a value between -1 and {0}, inclusive.</summary>
        internal static string @BlockingCollection_TimeoutInvalid => GetResourceString("BlockingCollection_TimeoutInvalid", @"The specified timeout must represent a value between -1 and {0}, inclusive.");
        /// <summary>The collections argument contains at least one disposed element.</summary>
        internal static string @BlockingCollection_ValidateCollectionsArray_DispElems => GetResourceString("BlockingCollection_ValidateCollectionsArray_DispElems", @"The collections argument contains at least one disposed element.");
        /// <summary>The collections length is greater than the supported range.</summary>
        internal static string @BlockingCollection_ValidateCollectionsArray_LargeSize => GetResourceString("BlockingCollection_ValidateCollectionsArray_LargeSize", @"The collections length is greater than the supported range.");
        /// <summary>The collections argument contains at least one null element.</summary>
        internal static string @BlockingCollection_ValidateCollectionsArray_NullElems => GetResourceString("BlockingCollection_ValidateCollectionsArray_NullElems", @"The collections argument contains at least one null element.");
        /// <summary>The collections argument is a zero-length array.</summary>
        internal static string @BlockingCollection_ValidateCollectionsArray_ZeroSize => GetResourceString("BlockingCollection_ValidateCollectionsArray_ZeroSize", @"The collections argument is a zero-length array.");
        /// <summary>The collection argument is null.</summary>
        internal static string @ConcurrentBag_Ctor_ArgumentNullException => GetResourceString("ConcurrentBag_Ctor_ArgumentNullException", @"The collection argument is null.");
        /// <summary>The array argument is null.</summary>
        internal static string @ConcurrentBag_CopyTo_ArgumentNullException => GetResourceString("ConcurrentBag_CopyTo_ArgumentNullException", @"The array argument is null.");
        /// <summary>The index argument must be greater than or equal zero.</summary>
        internal static string @Collection_CopyTo_ArgumentOutOfRangeException => GetResourceString("Collection_CopyTo_ArgumentOutOfRangeException", @"The index argument must be greater than or equal zero.");
        /// <summary>The SyncRoot property may not be used for the synchronization of concurrent collections.</summary>
        internal static string @ConcurrentCollection_SyncRoot_NotSupported => GetResourceString("ConcurrentCollection_SyncRoot_NotSupported", @"The SyncRoot property may not be used for the synchronization of concurrent collections.");
        /// <summary>The array is multidimensional, or the type parameter for the set cannot be cast automatically to the type of the destination array.</summary>
        internal static string @ConcurrentDictionary_ArrayIncorrectType => GetResourceString("ConcurrentDictionary_ArrayIncorrectType", @"The array is multidimensional, or the type parameter for the set cannot be cast automatically to the type of the destination array.");
        /// <summary>The source argument contains duplicate keys.</summary>
        internal static string @ConcurrentDictionary_SourceContainsDuplicateKeys => GetResourceString("ConcurrentDictionary_SourceContainsDuplicateKeys", @"The source argument contains duplicate keys.");
        /// <summary>The concurrencyLevel argument must be positive.</summary>
        internal static string @ConcurrentDictionary_ConcurrencyLevelMustBePositive => GetResourceString("ConcurrentDictionary_ConcurrencyLevelMustBePositive", @"The concurrencyLevel argument must be positive.");
        /// <summary>The capacity argument must be greater than or equal to zero.</summary>
        internal static string @ConcurrentDictionary_CapacityMustNotBeNegative => GetResourceString("ConcurrentDictionary_CapacityMustNotBeNegative", @"The capacity argument must be greater than or equal to zero.");
        /// <summary>The index argument is less than zero.</summary>
        internal static string @ConcurrentDictionary_IndexIsNegative => GetResourceString("ConcurrentDictionary_IndexIsNegative", @"The index argument is less than zero.");
        /// <summary>The index is equal to or greater than the length of the array, or the number of elements in the dictionary is greater than the available space from index to the end of the destination array.</summary>
        internal static string @ConcurrentDictionary_ArrayNotLargeEnough => GetResourceString("ConcurrentDictionary_ArrayNotLargeEnough", @"The index is equal to or greater than the length of the array, or the number of elements in the dictionary is greater than the available space from index to the end of the destination array.");
        /// <summary>The key already existed in the dictionary.</summary>
        internal static string @ConcurrentDictionary_KeyAlreadyExisted => GetResourceString("ConcurrentDictionary_KeyAlreadyExisted", @"The key already existed in the dictionary.");
        /// <summary>TKey is a reference type and item.Key is null.</summary>
        internal static string @ConcurrentDictionary_ItemKeyIsNull => GetResourceString("ConcurrentDictionary_ItemKeyIsNull", @"TKey is a reference type and item.Key is null.");
        /// <summary>The key was of an incorrect type for this dictionary.</summary>
        internal static string @ConcurrentDictionary_TypeOfKeyIncorrect => GetResourceString("ConcurrentDictionary_TypeOfKeyIncorrect", @"The key was of an incorrect type for this dictionary.");
        /// <summary>The value was of an incorrect type for this dictionary.</summary>
        internal static string @ConcurrentDictionary_TypeOfValueIncorrect => GetResourceString("ConcurrentDictionary_TypeOfValueIncorrect", @"The value was of an incorrect type for this dictionary.");
        /// <summary>The count argument must be greater than or equal to zero.</summary>
        internal static string @ConcurrentStack_PushPopRange_CountOutOfRange => GetResourceString("ConcurrentStack_PushPopRange_CountOutOfRange", @"The count argument must be greater than or equal to zero.");
        /// <summary>The sum of the startIndex and count arguments must be less than or equal to the collection's Count.</summary>
        internal static string @ConcurrentStack_PushPopRange_InvalidCount => GetResourceString("ConcurrentStack_PushPopRange_InvalidCount", @"The sum of the startIndex and count arguments must be less than or equal to the collection's Count.");
        /// <summary>The startIndex argument must be greater than or equal to zero.</summary>
        internal static string @ConcurrentStack_PushPopRange_StartOutOfRange => GetResourceString("ConcurrentStack_PushPopRange_StartOutOfRange", @"The startIndex argument must be greater than or equal to zero.");
        /// <summary>Dynamic partitions are not supported by this partitioner.</summary>
        internal static string @Partitioner_DynamicPartitionsNotSupported => GetResourceString("Partitioner_DynamicPartitionsNotSupported", @"Dynamic partitions are not supported by this partitioner.");
        /// <summary>Can not call GetEnumerator on partitions after the source enumerable is disposed</summary>
        internal static string @PartitionerStatic_CanNotCallGetEnumeratorAfterSourceHasBeenDisposed => GetResourceString("PartitionerStatic_CanNotCallGetEnumeratorAfterSourceHasBeenDisposed", @"Can not call GetEnumerator on partitions after the source enumerable is disposed");
        /// <summary>MoveNext must be called at least once before calling Current.</summary>
        internal static string @PartitionerStatic_CurrentCalledBeforeMoveNext => GetResourceString("PartitionerStatic_CurrentCalledBeforeMoveNext", @"MoveNext must be called at least once before calling Current.");
        /// <summary>Enumeration has either not started or has already finished.</summary>
        internal static string @ConcurrentBag_Enumerator_EnumerationNotStartedOrAlreadyFinished => GetResourceString("ConcurrentBag_Enumerator_EnumerationNotStartedOrAlreadyFinished", @"Enumeration has either not started or has already finished.");
        /// <summary>The given key '{0}' was not present in the dictionary.</summary>
        internal static string @Arg_KeyNotFoundWithKey => GetResourceString("Arg_KeyNotFoundWithKey", @"The given key '{0}' was not present in the dictionary.");
        
        private static string GetResourceString(string v1, string v2)
        {
            return v1 + " - " + v2;
        }

        internal static string Format(string v1, string v2)
        {
            return v1 + " - " + v2;
        }
    }
}