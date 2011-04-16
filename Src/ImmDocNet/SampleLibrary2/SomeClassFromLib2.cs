using System.Collections.Generic;
using System;

namespace SampleLibrary2
{
  /// <summary>
  /// qweqweqwe
  /// </summary>
  public class SampleClass<WWW>
  {
    /// <summary>
    /// asdhaksdasd
    /// </summary>
    /// <typeparam name="T">TTT</typeparam>
    /// <param name="s">sss</param>
    /// <returns>rrrrrrrrrr</returns>
    public static int HashMe<T>(List<Dictionary<int, T>> s)
    {
      return s.GetHashCode();
    }

    /// <summary>
    /// 2222222 asdsadasd
    /// </summary>
    /// <typeparam name="T">222 T</typeparam>
    /// <param name="s">222 sss</param>
    /// <param name="a">22 aaaaa</param>
    /// <returns>22 rrrrrr</returns>
    public static int HashMe<T>(T s, int a)
    {
      return s.GetHashCode();
    }

    /// <summary>
    /// 3333333 asdsadasd
    /// </summary>
    /// <typeparam name="X">333 XXX</typeparam>
    /// <param name="s">333 sss</param>
    /// <param name="a">33 aaaaa</param>
    /// <returns>33 rrrrrr</returns>
    public static int HashMe<X>(X s, string a)
    {
      return s.GetHashCode();
    }

    /// <summary>
    /// asdasdasdsad
    /// </summary>
    /// <typeparam name="X">XXXX</typeparam>
    /// <typeparam name="Y">YYYYYYYY</typeparam>
    /// <param name="s">ssssssssssssss</param>
    /// <param name="a">aaaaaa</param>
    /// <returns>qweqweqwe</returns>
    public static int HashMe<X, Y>(X s, Y a)
    {
      return s.GetHashCode();
    }

    /// <summary>
    /// 2 asdasd
    /// </summary>
    /// <typeparam name="X">2 XX</typeparam>
    /// <typeparam name="Y">2 Y</typeparam>
    /// <param name="s">2 s</param>
    /// <param name="a">2 a</param>
    /// <param name="ccc">2 ccc</param>
    /// <returns>adasdasd</returns>
    public static int HashMe<X, Y>(X s, Y a, string ccc)
    {
      return s.GetHashCode();
    }

    /// <summary>
    /// asdasdasd
    /// </summary>
    /// <param name="a">aaa</param>
    /// <param name="c">ccXXXXXXXXXXc</param>
    /// <returns>0</returns>
    public static int operator +(int a, SampleClass<WWW> c)
    {
      return 0;
    }
  }

  public class C
  {
    /// <summary>
    /// asdasdasd
    /// </summary>
    /// <param name="a">aaa</param>
    /// <param name="c">ccc</param>
    /// <returns>0</returns>
    public static int operator +(int a, C c)
    {
      return 0;
    }
  }

  /// <summary>
  /// qweqwe
  /// </summary>
  /// <typeparam name="T">TTTT</typeparam>
  /// <param name="a">aaaa</param>
  public delegate void D1<T>(int a);

  /// <summary>
  /// 2 asdassd
  /// </summary>
  /// <typeparam name="A">2 A</typeparam>
  /// <typeparam name="B">2 B</typeparam>
  /// <param name="a">2 a</param>
  public delegate void D1<A, B>(int a);
}
