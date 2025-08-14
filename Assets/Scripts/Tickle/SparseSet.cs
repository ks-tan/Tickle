using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Tickle.Collections
{
    public struct SparseKey
    {
        public int DataIndex;
        public int NextFreeKey;
    }

    public unsafe struct SparseSet<T> where T : unmanaged
    {
        private NativeArray<SparseKey> _sparseKeys;
        private NativeArray<T> _denseData;
        private int _nextFreeKey;
        private int _dataCount;
        private int _minimumSize;

        public SparseSet(int size)
        {
            _minimumSize = size;
            _nextFreeKey = 0;
            _dataCount = 0;
            _sparseKeys = new NativeArray<SparseKey>(size, Allocator.Persistent);
            _denseData = new NativeArray<T>(size, Allocator.Persistent);
            
            for(int i = 0; i < size; i++)
            {
                _sparseKeys[i] = new SparseKey() {
                    DataIndex = -1,
                    NextFreeKey = i >= size - 1 ? -1 : i + 1
                };
            }
        }

        public void Dispose()
        {
            if (_sparseKeys.IsCreated) _sparseKeys.Dispose();
            if (_denseData.IsCreated)  _denseData.Dispose();
        }

        public int GetFreeKey()
        { 
            return _nextFreeKey; 
        }

        public int GetDataCount()
        {
            return _dataCount;
        }

        public NativeArray<T> GetDenseData()
        {
            return _denseData;
        }

        public int Add(T data)
        {
            SparseKey* sparsePtr = (SparseKey*)NativeArrayUnsafeUtility.GetUnsafePtr(_sparseKeys);

            var freeKey = _nextFreeKey;

            // Point currently available sparse item to a dense array slot
            // and update linked list for tracking next free sparse item
            sparsePtr[freeKey].DataIndex = _dataCount;

            // Storing data in dense data array
            _denseData[sparsePtr[freeKey].DataIndex] = data;

            // Update remaining state properties of sparse set
            _nextFreeKey = sparsePtr[freeKey].NextFreeKey;
            _dataCount++;

            // Make sure that we have enough space to insert the next T item
            Resize();

            return freeKey;
        }
        
        public void Remove(int id)
        {
            SparseKey* sparsePtr = (SparseKey*)NativeArrayUnsafeUtility.GetUnsafePtr(_sparseKeys);

            // Remove item from data array
            _denseData[sparsePtr[id].DataIndex] = _denseData[_dataCount - 1];
            _dataCount--;

            // Update corresponding sparse key
            sparsePtr[id].NextFreeKey = _nextFreeKey;
            sparsePtr[id].DataIndex = -1;
            _nextFreeKey = id;
        }

        public bool TryGet(int id, ref T data)
        {
            if (_sparseKeys[id].DataIndex == -1)
                return false;
            if (_sparseKeys[id].DataIndex >= _dataCount)
                return false;
            data = _denseData[_sparseKeys[id].DataIndex];
            return true;
        }

        public void Resize()
        {
            // Increase size of sparse keys array
            // Note: We never reduce size of sparse array because it might
            // break the linked list that is tracking free sparse keys
            if (_dataCount >= _sparseKeys.Length)
                ResizeKeysArray(_sparseKeys.Length * 2);

            // Increase size of both dense data arrays
            if (_dataCount >= _denseData.Length)
            {
                ResizeDataArray(_denseData.Length * 2);
                return;
            }

            // Reduce size of just the dense data array
            if (_denseData.Length / 2f > _minimumSize &&
                _denseData.Length / 2f > _dataCount)
            {
                ResizeDataArray(_denseData.Length / 2);
                return;
            }
        }

        private void ResizeDataArray(int newSize)
        {
            var newArray = new NativeArray<T>(newSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            NativeArray<T>.Copy(_denseData, newArray, _dataCount);
            _denseData.Dispose();
            _denseData = newArray;
        }

        private void ResizeKeysArray(int newSize)
        {
            var originalLength = _sparseKeys.Length;

            var newArray = new NativeArray<SparseKey>(newSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            NativeArray<SparseKey>.Copy(_sparseKeys, newArray, originalLength);
            _sparseKeys.Dispose();
            _sparseKeys = newArray;

            // Update the linked list with the new entries
            for(int i = originalLength; i < newSize; i++)
            {
                _sparseKeys[i] = new SparseKey()
                {
                    DataIndex = -1,
                    NextFreeKey = i >= newSize - 1 ? _nextFreeKey : i + 1
                };
            }

            // The root of the linked list (_nextFreeKey) shall be the
            // first item in the new set of created sparse keys
            _nextFreeKey = originalLength;
        }
    }
}
