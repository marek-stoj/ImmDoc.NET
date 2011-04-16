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
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Mono.Cecil;

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
  class MyDelegateInfo : MyClassInfo
  {
    private string returnTypeFullName;
    private List<string> parametersNames;
    private Dictionary<string, MyParameterInfo> parameters;
    private string returnValueSummary = String.Empty;

    #region Constructor(s)

    public MyDelegateInfo(TypeDefinition typeDefinition, string assemblyName)
    {
      Debug.Assert(Utils.IsDelegate(typeDefinition), "Impossible! Given type is not a delegate type.");

      this.assemblyName = assemblyName;

      this.Initialize(typeDefinition);
      this.AddMembers(typeDefinition);
      this.CheckSupport(typeDefinition);
    }

    #endregion

    #region Protected helper methods

    protected override void AddNestedType(TypeDefinition typeDefinition)
    {
      Debug.Assert(false, "Delegates can't contain nested types.");
    }

    protected override void AddEvent(EventDefinition eventInfo)
    {
      Debug.Assert(false, "Delegates can't contain events.");
    }

    protected override void AddMethod(MethodDefinition methodDefinition)
    {
      if (methodDefinition.Name == "Invoke")
      {
        // skip the built-in method but read its signature

        // just temporary create MyMethodInfo instance to simplify obtaining method's signature
        // and return typeDefinition
        MyMethodInfo myMethodInfo = new MyMethodInfo(methodDefinition, this);

        returnTypeFullName = myMethodInfo.ReturnTypeFullName;

        parametersNames = new List<string>();
        parametersNames.AddRange(myMethodInfo.ParametersNames);

        parameters = new Dictionary<string, MyParameterInfo>();
        foreach (string parameterName in myMethodInfo.Parameters.Keys)
        {
          parameters[parameterName] = myMethodInfo.Parameters[parameterName];
        }
      }
    }

    #endregion

    #region Public properties

    public List<string> ParametersNames
    {
      get { return parametersNames; }
    }

    public Dictionary<string, MyParameterInfo> Parameters
    {
      get { return parameters; }
    }

    public string ReturnTypeFullName
    {
      get { return returnTypeFullName; }
    }

    public override string AttributesString
    {
      get { return base.AttributesString.Replace("sealed ", "").Replace("sealed", "").TrimEnd() + " delegate"; }
    }

    public string ReturnValueSummary
    {
      get { return returnValueSummary; }
      set { returnValueSummary = value; }
    }

    #endregion

    #region MetaClass overrides

    public override string GetMetaName()
    {
      return "Delegate";
    }

    #endregion
  }
}
