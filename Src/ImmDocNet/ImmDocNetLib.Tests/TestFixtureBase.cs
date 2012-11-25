using Mono.Cecil;
using NUnit.Framework;

namespace ImmDocNetLib.Tests
{
  public class TestFixtureBase
  {
    protected AssemblyDefinition sampleAssembly;

    #region SetUp and TearDown

    [SetUp]
    public virtual void SetUp()
    {
      sampleAssembly = AssemblyDefinition.ReadAssembly("SampleLibrary.dll");
    }

    [TearDown]
    public virtual void TearDown()
    {
    }

    #endregion
  }
}
