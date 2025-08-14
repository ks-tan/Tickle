using UnityEditor;
using UnityEngine;
using Tickle.Collections;

namespace Tickle.Internal.Editor
{
    public static class UnitTests
    {
        private delegate void UnitTest(out object result, out object expected);

        [MenuItem("Tickle/Test")]
        public static void RunTests()
        {
            UnitTest[] tests =
            {
                SparseSetTests.InsertMultipleAndGetSingle,
                SparseSetTests.InsertMultipleAndRemoveSingle,
                SparseSetTests.InsertMultipleAndCheckLength,
                SparseSetTests.InsertMultipleAndCheckFreeKey,
                SparseSetTests.RemoveMultipleAndCheckFreeKey,
            };

            var failures = 0;

            foreach(var test in tests)
            {
                test.Invoke(out object result, out object expected);
                var isSuccess = result.Equals(expected);
                if (!isSuccess) failures++;
                Debug.Assert(isSuccess, $"Failed {test.Method.Name}. Expected {expected} but received {result}");
            }

            if (failures == 0)
                Debug.Log("Passed all unit tests!");
        }
    }

    public static unsafe class SparseSetTests
    {
        public static void InsertMultipleAndGetSingle(out object result, out object expected)
        {
            var sparseSet = new SparseSet<int>(64);
            for (int i = 0; i < 64; i++)
                sparseSet.Add(i);
            sparseSet.TryGet(5, out var data);
            result = *data;
            expected = 5;
            sparseSet.Dispose();
        }

        public static void InsertMultipleAndRemoveSingle(out object result, out object expected)
        {
            var sparseSet = new SparseSet<int>(64);
            for (int i = 0; i < 64; i++)
                sparseSet.Add(i);
            sparseSet.Remove(10);
            result = sparseSet.TryGet(10, out var _);
            expected = false;
            sparseSet.Dispose();
        }

        public static void InsertMultipleAndCheckLength(out object result, out object expected)
        {
            var sparseSet = new SparseSet<int>(128);
            for (int i = 0; i < 128; i++)
                sparseSet.Add(i);
            result = sparseSet.GetDataCount();
            expected = 128;
            sparseSet.Dispose();
        }

        public static void InsertMultipleAndCheckFreeKey(out object result, out object expected)
        {
            var sparseSet = new SparseSet<int>(64);
            for (int i = 0; i < 64; i++)
                sparseSet.Add(i);
            result = sparseSet.GetFreeKey();
            expected = 64;
            sparseSet.Dispose();
        }
    
        public static void RemoveMultipleAndCheckFreeKey(out object result, out object expected)
        {
            var sparseSet = new SparseSet<int>(64);
            for (int i = 0; i < 64; i++)
                sparseSet.Add(i);
            for (int i = 0; i < 10; i++)
                sparseSet.Remove(i);
            result = sparseSet.GetFreeKey();
            expected = 9;
            sparseSet.Dispose();
        }
    }
}
