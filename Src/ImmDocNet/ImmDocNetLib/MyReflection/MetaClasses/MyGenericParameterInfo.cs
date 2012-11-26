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
using System.Collections.Generic;
using Mono.Cecil;
using System.Diagnostics;

using Imm.ImmDocNetLib.Documenters;
using Imm.ImmDocNetLib.MyReflection.GenericConstraints;
using System.Text;

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
  public class MyGenericParameterInfo : MetaClass, ISummarisableMember
  {
    private List<GenericConstraint> constraints;

    #region Constructor(s)

    public MyGenericParameterInfo(GenericParameter genericParameter)
      : base()
    {
      this.name = genericParameter.Name;
      this.constraints = CreateGenericConstraints(genericParameter);
    }

    #endregion

    #region Private helper methods

    private List<GenericConstraint> CreateGenericConstraints(GenericParameter genericParameter)
    {
      List<GenericConstraint> result = null;

      if ((genericParameter.Attributes & GenericParameterAttributes.ReferenceTypeConstraint) == GenericParameterAttributes.ReferenceTypeConstraint)
      {
        result = new List<GenericConstraint>();

        result.Add(new BuiltInGenericConstraint(BuiltInGenericConstraintsTypes.Class));
      }
      else if ((genericParameter.Attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) == GenericParameterAttributes.NotNullableValueTypeConstraint)
      {
        result = new List<GenericConstraint>();

        result.Add(new BuiltInGenericConstraint(BuiltInGenericConstraintsTypes.Struct));
      }

      var baseOrInterfaceConstraints = genericParameter.Constraints;

      if (baseOrInterfaceConstraints != null)
      {
        if (result == null) { result = new List<GenericConstraint>(); }

        for (int i = 0; i < baseOrInterfaceConstraints.Count; i++)
        {
          TypeReference baseTypeOrInterface = baseOrInterfaceConstraints[i];

          if (baseTypeOrInterface.FullName == "System.ValueType")
          {
            continue;
          }

          if (Utils.IsGenericParameter(baseTypeOrInterface))
          {
            result.Add(new NakedTypeConstraint(baseTypeOrInterface.Name));
          }
          else
          {
            string[] readableForms = Tools.GetHumanReadableForms(baseTypeOrInterface);

            result.Add(new TypeGenericConstraint(readableForms[0]));
          }
        }
      }

      if ((genericParameter.Attributes & GenericParameterAttributes.DefaultConstructorConstraint) == GenericParameterAttributes.DefaultConstructorConstraint
       && (genericParameter.Attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0)
      {
        if (result == null) { result = new List<GenericConstraint>(); }

        result.Add(new BuiltInGenericConstraint(BuiltInGenericConstraintsTypes.New));
      }

      return result;
    }

    #endregion

    #region Public properties

    public int ConstraintsCount
    {
      get { return constraints == null ? 0 : constraints.Count; }
    }

    public List<GenericConstraint> Constraints
    {
      get
      {
        Debug.Assert(constraints != null, "Impossible! This generic parameter doesn't have constraints.");

        return constraints;
      }
    }

    #endregion

    #region MetaClass overrides

    public override string GetMetaName()
    {
      return "Generic Parameter";
    }

    #endregion

    #region ISummarisableMember Members

    public string DisplayableName
    {
      get { return name; }
    }

    #endregion
  }
}
