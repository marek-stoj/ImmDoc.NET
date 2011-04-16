using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampleLibrary
{
  /// <summary>
  /// Some funny generic class <br />
  /// See <see cref="DoSomething{T, V}">the funny method.</see>
  /// </summary>
  /// <example>
  /// <para>This is an example:</para>
  /// <code>
  /// public virtual void DoSomething(int a)
  /// {
  ///     int b = a + 1;
  /// }
  /// </code>
  /// </example>
  /// <typeparam name="T">Some type param named T.</typeparam>
  /// <typeparam name="U">Some type param named U.</typeparam>
  public class GenericClass<T, U> : Dictionary<List<T>, Dictionary<string[], List<U?[,][][]>>>
    where T : class, IEnumerable<List<U>>, new()
    where U : struct
  {
    /// <summary>
    /// Some funny method.
    /// </summary>
    /// <typeparam name="T">Type param named T.</typeparam>
    /// <typeparam name="V">Type param named V.</typeparam>
    /// <param name="param1">Some param 1.</param>
    /// <param name="param2">Some param 2</param>
    /// <exception cref="MyException{T}">When anything goes wrong.</exception>
    /// <returns>Nothing.</returns>
    public List<U?>[][][, ,] DoSomething<T, V>(V[] param1, Dictionary<string, List<U?>> param2)
    {
      return null;
    }

    /// <summary>
    /// Some overload of funny method. See parameter <paramref name="a" />. See type param <typeparamref name="T" />.
    /// </summary>
    /// <param name="a">Some param a.</param>
    /// <exception cref="MyException{T}">When anything goes wrong.</exception>
    /// <typeparam name="T">Type param named T.</typeparam>
    /// <typeparam name="V">Type param named V.</typeparam>
    public void DoSomething<T, V>(int a)
    {
    }
  }

  public class MyException<T> : Exception
  {
  }
}
