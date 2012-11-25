/*
 * Copyright 2007 - 2009 Marek Stój
 * 
 * This file is part of ImmDoc .NET.
 *
 * ImmDoc .NET is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * ImmDoc .NET is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ImmDoc .NET; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.IO;
using Mono.Cecil;
using System.Diagnostics;
using System.Collections.Generic;

using Imm.ImmDocNetLib.MyReflection.Attributes;

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
  class MyMethodInfo : MyInvokableMemberInfo
  {
    public static readonly Dictionary<string, string> MethodsNamesMappings;

    protected string returnTypeFullName;
    protected string returnTypeFullNameWithoutRevArrayStrings;
    protected string returnValueSummary = String.Empty;

    private readonly List<MyGenericParameterInfo> genericParameters;

    #region Constructor(s)

    public MyMethodInfo(MethodDefinition methodDefinition, MyClassInfo declaringType)
      : base(methodDefinition, declaringType)
    {
      this.name = null;

      if ((methodDefinition.Attributes & MethodAttributes.SpecialName) != 0)
      {
        if (MethodsNamesMappings.ContainsKey(methodDefinition.Name))
        {
          this.name = MethodsNamesMappings[methodDefinition.Name];
        }
      }

      if (this.name == null)
      {
        this.name = methodDefinition.Name;
      }

      if (Utils.IsGenericMethod(methodDefinition))
      {
        Tools.ExamineGenericParameters(methodDefinition.GenericParameters, null, out genericParameters);
      }

      if (genericParameters != null && genericParameters.Count > 0)
      {
        this.name += Tools.CreateFormalGenericParametersString(genericParameters);
      }

      string[] readableForms = Tools.GetHumanReadableForms(methodDefinition.ReturnType);
      this.returnTypeFullName = readableForms[0];
      this.returnTypeFullNameWithoutRevArrayStrings = readableForms[1];

      this.CheckSupport(methodDefinition.Attributes);

      AddParameters(methodDefinition.Parameters);
    }

    static MyMethodInfo()
    {
      MethodsNamesMappings = new Dictionary<string, string>();

      MethodsNamesMappings["op_Addition"] = "operator +";
      MethodsNamesMappings["op_BitwiseAnd"] = "operator &";
      MethodsNamesMappings["op_BitwiseOr"] = "operator |";
      MethodsNamesMappings["op_Decrement"] = "operator --";
      MethodsNamesMappings["op_Division"] = "operator /";
      MethodsNamesMappings["op_Equality"] = "operator ==";
      MethodsNamesMappings["op_ExclusiveOr"] = "operator ^";
      MethodsNamesMappings["op_False"] = "operator false";
      MethodsNamesMappings["op_GreaterThan"] = "operator >";
      MethodsNamesMappings["op_GreaterThanOrEqual"] = "operator >=";
      MethodsNamesMappings["op_Increment"] = "operator ++";
      MethodsNamesMappings["op_Inequality"] = "operator !=";
      MethodsNamesMappings["op_LeftShift"] = "operator <<";
      MethodsNamesMappings["op_LessThan"] = "operator <";
      MethodsNamesMappings["op_LessThanOrEqual"] = "operator <=";
      MethodsNamesMappings["op_LogicalNot"] = "operator !";
      MethodsNamesMappings["op_Modulus"] = "operator %";
      MethodsNamesMappings["op_Multiply"] = "operator *";
      MethodsNamesMappings["op_OnesComplement"] = "operator ~";
      MethodsNamesMappings["op_RightShift"] = "operator >>";
      MethodsNamesMappings["op_Subtraction"] = "operator -";
      MethodsNamesMappings["op_True"] = "operator true";
      MethodsNamesMappings["op_UnaryNegation"] = "operator -";
      MethodsNamesMappings["op_UnaryPlus"] = "operator +";

      MethodsNamesMappings["op_Implicit"] = "operator implicit";
      MethodsNamesMappings["op_Explicit"] = "operator explicit";
    }

    #endregion

    #region Implementation of abstract methods

    protected override void DumpShallow(TextWriter textWriter, string prefix)
    {
#if DEBUG
      textWriter.Write("{0}{1} {2} {3}(", prefix, AttributesString, returnTypeFullName, name);
#endif
    }

    #endregion

    #region Public methods

    public static bool IsMethodNameMapped(string methodName)
    {
      return MethodsNamesMappings.ContainsKey(methodName);
    }

    public MyGenericParameterInfo FindGenericParameter(string typeParamName)
    {
      if (genericParameters == null) { return null; }

      foreach (MyGenericParameterInfo myGenericParameterInfo in genericParameters)
      {
        if (myGenericParameterInfo.Name == typeParamName)
        {
          return myGenericParameterInfo;
        }
      }

      return null;
    }

    #endregion

    #region MetaClass overrides

    public override string GetMetaName()
    {
      return "Method";
    }

    #endregion

    #region Public properties

    public string ReturnTypeFullName
    {
      get { return returnTypeFullName; }
    }

    public string ReturnTypeFullNameWithoutRevArrayStrings
    {
      get { return returnTypeFullNameWithoutRevArrayStrings; }
    }

    public string ReturnValueSummary
    {
      get { return returnValueSummary; }
      set { returnValueSummary = value; }
    }

    public int GenericParametersCount
    {
      get { return genericParameters == null ? 0 : genericParameters.Count; }
    }

    public List<MyGenericParameterInfo> GenericParameters
    {
      get
      {
        Debug.Assert(genericParameters != null, "This type is not a generic type!");

        return genericParameters;
      }
    }

    public bool ContainsGenericParameterWithConstraints
    {
      get
      {
        if (genericParameters == null) { return false; }

        foreach (MyGenericParameterInfo myGenericParameterInfo in genericParameters)
        {
          if (myGenericParameterInfo.ConstraintsCount > 0)
          {
            return true;
          }
        }

        return false;
      }
    }

    #endregion
  }
}
