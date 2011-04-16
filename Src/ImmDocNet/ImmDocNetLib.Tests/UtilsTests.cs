using System.Linq;
using Imm.ImmDocNetLib;
using Mono.Cecil;
using NUnit.Framework;

namespace ImmDocNetLib.Tests
{
  [TestFixture]
  public class UtilsTests : TestFixtureBase
  {
    #region Tests

    [Test]
    public void GetTypeMembers_should_not_return_special_property_methods_as_ordinary_methods()
    {
      var testClassType = GetSampleClassType("TestClass1");
      var members = Utils.GetTypeMembers(testClassType);

      Assert.IsFalse(members.Cast<MemberReference>().Any(mr => mr.Name.StartsWith("get_")));
      Assert.IsFalse(members.Cast<MemberReference>().Any(mr => mr.Name.StartsWith("set_")));
    }

    [Test]
    public void Test_GetEvent()
    {
      var testClassType = GetSampleClassType("TestClass1");

      Assert.IsNotNull(Utils.GetEvent(testClassType, "SomeEvent"));
    }

    [Test]
    public void Test_GetNestedType()
    {
      var testClassType = GetSampleClassType("TestClass1");

      Assert.IsNotNull(Utils.GetNestedType(testClassType, "NestedClass1"));
      Assert.IsNotNull(Utils.GetNestedType(testClassType, "PrivateNestedClass1"));
      Assert.IsNotNull(Utils.GetNestedType(testClassType, "NestedStruct1"));
      Assert.IsNotNull(Utils.GetNestedType(testClassType, "PrivateNestedStruct1"));
      Assert.IsNull(Utils.GetNestedType(testClassType, "NonExistingNestedClass"));
    }

    [Test]
    public void Test_IsDelegate()
    {
      var testClassType = GetSampleClassType("TestClass1");
      var delegateType = testClassType.NestedTypes.Cast<TypeDefinition>().Single(td => td.Name == "SomeDelegate1");

      Assert.IsTrue(Utils.IsDelegate(delegateType));
      Assert.IsFalse(Utils.IsDelegate(testClassType));
    }

    [Test]
    public void Test_IsMethodGeneric()
    {
      var testClassType = GetSampleClassType("TestClass1");
      var genericMethod = testClassType.Methods.Cast<MethodReference>().Single(mr => mr.Name == "GenericMethod");
      var nonGenericMethod = testClassType.Methods.Cast<MethodReference>().Single(mr => mr.Name == "NonGenericMethod");

      Assert.IsTrue(Utils.IsGenericMethod(genericMethod));
      Assert.IsFalse(Utils.IsGenericMethod(nonGenericMethod));
    }

    [Test]
    public void Test_ContainsCustomAttribute()
    {
      var testClassType = GetSampleClassType("TestClass1");
      var method = testClassType.Methods.Cast<MethodReference>().Single(mr => mr.Name == "MethodWithParamWithAttribute");

      Assert.IsTrue(Utils.ContainsCustomAttribute(method.Parameters[0], "System.ParamArrayAttribute"));
      Assert.IsFalse(Utils.ContainsCustomAttribute(method.Parameters[0], "ParamArrayAttribute"));
    }

    [Test]
    public void IsTypeGeneric()
    {
      var testClass1Type = GetSampleClassType("TestClass1");
      var testClass3Type = GetSampleClassType("TestClass3`2");
      
      Assert.IsFalse(Utils.IsTypeGeneric(testClass1Type));
      Assert.IsTrue(Utils.IsTypeGeneric(testClass3Type));
    }

    [Test]
    public void Test_IsGenericParameter()
    {
      var testClass3Type = GetSampleClassType("TestClass3`2");
      var tGenericParam = testClass3Type.GenericParameters.Cast<GenericParameter>().Single(gp => gp.Name == "T");
      var uGenericParam = testClass3Type.GenericParameters.Cast<GenericParameter>().Single(gp => gp.Name == "U");

      Assert.IsFalse(Utils.IsGenericParameter(testClass3Type));
      Assert.AreEqual(1, tGenericParam.Constraints.Count);
      Assert.IsTrue(Utils.IsGenericParameter(uGenericParam));
    }

    [Test]
    public void Test_GetProperty()
    {
      var testClassType = GetSampleClassType("TestClass1");
    }

    #endregion

    #region Private methods

    private TypeDefinition GetSampleClassType(string className)
    {
      return sampleAssembly.MainModule.Types.Cast<TypeDefinition>().Single(td => td.Name == className);
    }

    #endregion
  }
}
