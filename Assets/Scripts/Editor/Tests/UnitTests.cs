using System;
using UnityEditor;
using UnityEngine;
using Tickle.Collections;

public static class UnitTests
{
    private delegate void UnitTest(out object result, out object expected);

    [MenuItem("Tickle/Test")]
    public static void RunTests()
    {
        UnitTest[] tests =
        {
            SparseSetTests.InsertSingleAndGetSingle,
            SparseSetTests.InsertMultipleAndGetSingle,
            SparseSetTests.InsertSingleAndRemoveSingle,
            SparseSetTests.InsertMultipleAndRemoveSingle,
            SparseSetTests.InsertMultipleAndCheckLength,
            SparseSetTests.InsertMultipleAndCheckFreeKey
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

public static class SparseSetTests
{
    public static void InsertSingleAndGetSingle(out object result, out object expected)
    {
        var sparseSet = new SparseSet<int>(64);
        var index = sparseSet.Add(1);
        var data = -1;
        sparseSet.TryGet(index, ref data);
        result = data;
        expected = 1;
        sparseSet.Dispose();
    }

    public static void InsertMultipleAndGetSingle(out object result, out object expected)
    {
        var sparseSet = new SparseSet<int>(64);
        for (int i = 0; i < 64; i++)
            sparseSet.Add(i);
        var data = -1;
        sparseSet.TryGet(5, ref data);
        result = data;
        expected = 5;
        sparseSet.Dispose();
    }

    public static void InsertSingleAndRemoveSingle(out object result, out object expected)
    {
        var sparseSet = new SparseSet<int>(64);
        sparseSet.Add(10);
        sparseSet.Remove(0);
        var data = -1;
        result = sparseSet.TryGet(0, ref data);
        expected = false;
        sparseSet.Dispose();
    }

    public static void InsertMultipleAndRemoveSingle(out object result, out object expected)
    {
        var sparseSet = new SparseSet<int>(64);
        for (int i = 0; i < 64; i++)
            sparseSet.Add(i);
        sparseSet.Remove(10);
        var data = -1;
        result = sparseSet.TryGet(10, ref data);
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
        var sparseSet = new SparseSet<int>(128);
        for (int i = 0; i < 128; i++)
            sparseSet.Add(i);
        result = sparseSet.GetFreeKey();
        expected = 128;
        sparseSet.Dispose();
    }
}
