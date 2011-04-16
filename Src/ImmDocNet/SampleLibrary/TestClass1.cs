using System;
using System.Collections.Generic;
using SampleLibrary2;

namespace SampleLibrary
{
  /// <summary>
  /// Some TestClass.
  /// </summary>
  public class TestClass1 : SampleClass<int>
  {
    /// <summary>
    /// Some prop.
    /// </summary>
    public int Prop
    {
      get { return 1; }
      set { }
    }

    /// <summary>
    /// Some indexer.
    /// </summary>
    /// <param name="index">Some indexer parameter.</param>
    /// <returns>Some value.</returns>
    public int this[int index]
    {
      get { return 1; }
      set { }
    }

    /// <summary>
    /// Unresolved field.
    /// </summary>
    public SampleClass<int> someClassFromLib2;

    public event EventHandler SomeEvent;

    public class NestedClass1 { }

    private class PrivateNestedClass1 { }

    public struct NestedStruct1 { }

    private struct PrivateNestedStruct1 { }

    public delegate void SomeDelegate1(int a);

    private void NonGenericMethod(int a, List<int> list)
    {
    }

    private void GenericMethod<T>(T t)
    {
    }

    private void MethodWithParamWithAttribute(params object[] args)
    {
    }

    public event EventHandler ManualEvent
    {
      add
      {
        Console.WriteLine(value.ToString());
      }

      remove
      {
        Console.WriteLine(value.ToString());
      }
    }
  }
}
