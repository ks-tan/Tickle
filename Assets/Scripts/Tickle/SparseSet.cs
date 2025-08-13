using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Tickle.Collections
{
    public struct SparseItem
    {
        public int DenseArrayIndex;
        public int NextFree;
    }

    public unsafe struct SparseSet<T> where T : unmanaged
    {
        private NativeArray<SparseItem> _sparseArray;
        private NativeArray<T> _denseArray;
        private int _freeSparseIndex;
        private int _denseArrayCount;
        private int _minimumSize;

        public SparseSet(int size)
        {
            _minimumSize = size;
            _freeSparseIndex = 0;
            _denseArrayCount = 0;
            _sparseArray = new NativeArray<SparseItem>(size, Allocator.Persistent);
            _denseArray = new NativeArray<T>(size, Allocator.Persistent);
            
            for(int i = 0; i < size; i++)
            {
                _sparseArray[i] = new SparseItem() {
                    DenseArrayIndex = -1,
                    NextFree = i >= size - 1 ? -1 : i + 1
                };
            }
        }

        public void Dispose()
        {
            if (_sparseArray.IsCreated) _sparseArray.Dispose();
            if (_denseArray.IsCreated)  _denseArray.Dispose();
        }

        public int GetFreeIndex()
        { 
            return _freeSparseIndex; 
        }

        public int GetDenseArrayCount()
        {
            return _denseArrayCount;
        }

        public NativeArray<T> GetDenseArray()
        {
            return _denseArray;
        }

        public int Add(T data, int index = -1)
        {
            SparseItem* sparsePtr = (SparseItem*)NativeArrayUnsafeUtility.GetUnsafePtr(_sparseArray);

            // Point currently available sparse item to a dense array slot
            // and update linked list for tracking next free sparse item
            index = index == -1 ? _freeSparseIndex : index;
            if (sparsePtr[index].DenseArrayIndex != -1)
            {
                Debug.Log("SparseSet insertion failed: Specified ID is already taken");
                return -1;
            }
            sparsePtr[index].DenseArrayIndex = _denseArrayCount;

            // Storing data in dense array
            _denseArray[sparsePtr[index].DenseArrayIndex] = data;

            // Update remaining state properties of sparse set
            _freeSparseIndex = sparsePtr[index].NextFree;
            _denseArrayCount++;

            // Make sure that we have enough space to insert the next T item
            Resize();

            return index;
        }
        
        public void Remove(int id)
        {
            SparseItem* sparsePtr = (SparseItem*)NativeArrayUnsafeUtility.GetUnsafePtr(_sparseArray);

            // Update corresponding sparse item
            sparsePtr[id].NextFree = _freeSparseIndex;
            sparsePtr[id].DenseArrayIndex = -1;
            _freeSparseIndex = id;

            // Remove item from array
            _denseArray[sparsePtr[id].DenseArrayIndex] = _denseArray[_denseArrayCount - 1];
            _denseArrayCount--;
        }

        public bool TryGet(int id, ref T data)
        {
            if (_sparseArray[id].DenseArrayIndex == -1)
                return false;
            data = _denseArray[_sparseArray[id].DenseArrayIndex];
            return true;
        }

        public void Resize()
        {
            // Increase size of sparse array
            // Note: We never reduce size of sparse array because it might
            // break the linked list that is tracking free sparse items
            if (_denseArrayCount > 3/4 * _sparseArray.Length)
                ResizeSparseArray(_sparseArray.Length * 2);

            // Increase size of both dense arrays
            if (_denseArrayCount > 3/4 * _denseArray.Length)
            {
                ResizeDenseArray(_denseArray.Length * 2);
                return;
            }

            // Reduce size of just the dense array
            if (_denseArray.Length / 2 > _minimumSize && 
                _denseArrayCount < 1/4 * _denseArray.Length)
            {
                ResizeDenseArray(_denseArray.Length / 2);
                return;
            }
        }

        private void ResizeDenseArray(int newSize)
        {
            var newArray = new NativeArray<T>(newSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            NativeArray<T>.Copy(_denseArray, newArray, _denseArrayCount);
            _denseArray.Dispose();
            _denseArray = newArray;
        }

        private void ResizeSparseArray(int newSize)
        {
            var originalLength = _sparseArray.Length;

            var newArray = new NativeArray<SparseItem>(newSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            NativeArray<SparseItem>.Copy(_sparseArray, newArray, originalLength);
            _sparseArray.Dispose();
            _sparseArray = newArray;

            // Update the linked list with the new entries
            for(int i = originalLength; i < newSize; i++)
            {
                _sparseArray[i] = new SparseItem()
                {
                    DenseArrayIndex = -1,
                    NextFree = i >= newSize - 1 ? _freeSparseIndex : i + 1
                };
            }

            // The root of the linked list (_freeSparseIndex) shall be the
            // first item in the new set of created sparse items
            _freeSparseIndex = originalLength;
        }
    }
}
