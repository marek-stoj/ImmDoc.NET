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
using System.IO;
using Mono.Cecil;

using Imm.ImmDocNetLib.MyReflection.Attributes;
using System.Diagnostics;

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
  class MyParameterInfo : MetaClass
  {
    private string typeFullName;
    private string typeFullNameWithXmlCompatibleArrayStrings;
    private MyParameterAttributes attributes;

    #region Constructor(s)

    public MyParameterInfo(ParameterDefinition parameterDefinition)
    {
      this.name = parameterDefinition.Name;
      string[] readableForms = Tools.GetHumanReadableForms(parameterDefinition.ParameterType);
      this.typeFullName = readableForms[0];
      this.typeFullNameWithXmlCompatibleArrayStrings = readableForms[1];
      this.attributes = GetMyParameterAttributes(parameterDefinition);

      this.CheckSupport(parameterDefinition.Attributes);
    }

    #endregion

    #region Private helper methods

    private void CheckSupport(ParameterAttributes parameterAttributes)
    {
      string warningTemplate = "Parameter '" + name + "' has unsupported attribute: '{0}'.";

      // in order to reduce output we warn only about important attributes which are not currently
      // supported:

      // TODO: support this: if ((parameterAttributes & ParameterAttributes.HasDefault) != 0) { Logger.Warning(warningTemplate, "HasDefault"); }
      // TODO: support this: if ((parameterAttributes & ParameterAttributes.HasFieldMarshal) != 0) { Logger.Warning(warningTemplate, "HasFieldMarshal"); }
      // TODO: support this: if ((parameterAttributes & ParameterAttributes.Lcid) != 0) { Logger.Warning(warningTemplate, "Lcid"); }
      // TODO: support this: if ((parameterAttributes & ParameterAttributes.Optional) != 0) { Logger.Warning(warningTemplate, "Optional"); }
      // TODO: support this: if ((parameterAttributes & ParameterAttributes.Retval) != 0) { Logger.Warning(warningTemplate, "Retval"); }
    }

    private static MyParameterAttributes GetMyParameterAttributes(ParameterDefinition parameterDefinition)
    {
      MyParameterAttributes myParameterAttributes = MyParameterAttributes.None;

      string parameterTypeName = parameterDefinition.ParameterType.FullName == null ? parameterDefinition.ParameterType.Name : parameterDefinition.ParameterType.FullName;
      bool isRef = parameterTypeName.EndsWith("&");

      if (isRef)
      {
        if (parameterDefinition.IsOut) { myParameterAttributes |= MyParameterAttributes.Out; }
        else { myParameterAttributes |= MyParameterAttributes.Ref; }
      }

      if (Utils.ContainsCustomAttribute(parameterDefinition, "System.ParamArrayAttribute"))
      {
        myParameterAttributes |= MyParameterAttributes.Params;
      }

      return myParameterAttributes;
    }

    private static string MyParameterAttributesToString(MyParameterAttributes myParameterAttributes)
    {
      StringBuilder sb = new StringBuilder();

      if ((myParameterAttributes & MyParameterAttributes.Params) != 0) { sb.Append("params "); }
      if ((myParameterAttributes & MyParameterAttributes.Ref) != 0) { sb.Append("ref "); }
      if ((myParameterAttributes & MyParameterAttributes.Out) != 0) { sb.Append("out "); }

      if (sb.Length > 0)
      {
        sb.Length = sb.Length - 1;
      }

      return sb.ToString();
    }

    #endregion

    #region Public helper methods

    public string GetXMLCompatibleRepresentation()
    {
      return GetXMLCompatibleRepresentation(typeFullNameWithXmlCompatibleArrayStrings, IsOut, IsRef);
    }

    public static string GetXMLCompatibleRepresentation(string typeFullNameWithXmlCompatibleArrayStrings, bool isOut, bool isRef)
    {
      string result = typeFullNameWithXmlCompatibleArrayStrings.Replace('/', '.').Replace(" ", "");

      if (isOut || isRef)
      {
        result += "@";
      }

      return result;
    }

    #endregion

    #region Public properties

    public string AttributesString
    {
      get { return MyParameterAttributesToString(attributes); }
    }

    public string TypeFullName
    {
      get { return typeFullName.TrimEnd('&'); }
    }

    public bool IsOut
    {
      get { return (attributes & MyParameterAttributes.Out) != 0; }
    }

    public bool IsRef
    {
      get { return (attributes & MyParameterAttributes.Ref) != 0; }
    }

    #endregion

    #region MetaClass overrides

    public override string GetMetaName()
    {
      return "Parameter";
    }

    #endregion
  }
}
