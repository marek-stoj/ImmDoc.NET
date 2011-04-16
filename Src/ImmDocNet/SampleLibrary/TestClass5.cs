namespace SampleLibrary
{
  /// <summary>
  /// Test class doc.
  /// </summary>
  public class RefAndOutParams
  {
    /// <summary>
    /// Foo docs. Doc error (fixed now).
    /// </summary>
    /// <param name="i">The out param.</param>
    public void Foo(out int i)
    {
      i = 1;
    }
    /// <summary>
    /// Foo docs. Doc error (fixed now).
    /// </summary>
    /// <param name="s">The ref param.</param>
    public void Baz(ref string s)
    {
    }
    /// <summary>
    /// Foo docs. Generates fine.
    /// </summary>
    /// <param name="j">The first param.</param>
    /// <param name="tmp">The second param.</param>
    public void Bar(int j, string tmp)
    {
    }
  }
}
