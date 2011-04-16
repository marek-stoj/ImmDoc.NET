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
using System.Diagnostics;

using Imm.ImmDocNetLib.MyReflection.Attributes;
using Imm.ImmDocNetLib.Documenters;
using Mono.Cecil;

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
  public class MyNestedTypeInfo : MetaClass, ISummarisableMember
  {
    private NestedTypes metaType;
    private MyClassAttributes attributes;

    #region Constructor(s)

    public MyNestedTypeInfo(TypeDefinition typeDefinition, MyClassInfo declaringType)
      : base()
    {
      Debug.Assert(Utils.IsTypeNested(typeDefinition), "Impossible! Given type is not a nested type.");

      string[] readableForms = Tools.GetHumanReadableForms(typeDefinition);
      this.name = readableForms[0];

      int indexOfLastSlash = this.name.LastIndexOf('/');
      Debug.Assert(indexOfLastSlash != -1 && indexOfLastSlash + 1 < this.Name.Length, "Impossible! This is a nested type.");

      this.name = this.name.Substring(indexOfLastSlash + 1);

      this.attributes = MyClassInfo.GetMyClassAttributes(typeDefinition);
      this.declaringType = declaringType;

      this.metaType = GetMetaType(typeDefinition);

      if (metaType == NestedTypes.Unknown)
      {
        Logger.Warning("Unrecognized meta type of '{0}'", typeDefinition.FullName);
      }
    }

    #endregion

    #region Private helper methods

    private static NestedTypes GetMetaType(TypeDefinition typeDefinition)
    {
      if (typeDefinition.IsClass && !Utils.IsDelegate(typeDefinition))
      {
        return NestedTypes.Class;
      }
      else if (typeDefinition.IsValueType && !typeDefinition.IsEnum)
      {
        return NestedTypes.Structure;
      }
      else if (typeDefinition.IsInterface)
      {
        return NestedTypes.Interface;
      }
      else if (typeDefinition.IsEnum)
      {
        return NestedTypes.Enumeration;
      }
      else if (Utils.IsDelegate(typeDefinition))
      {
        return NestedTypes.Delegate;
      }
      else
      {
        return NestedTypes.Unknown;
      }
    }

    private static string NestedTypesToString(NestedTypes metaType)
    {
      switch (metaType)
      {
        case NestedTypes.Class: { return "class"; }
        case NestedTypes.Delegate: { return "delegate"; }
        case NestedTypes.Enumeration: { return "enum"; }
        case NestedTypes.Interface: { return "interface"; }
        case NestedTypes.Structure: { return "struct"; }

        default:
          {
            Debug.Assert(false, "Impossible! Unrecognized nested type.");

            break;
          }
      }

      return String.Empty;
    }

    #endregion

    #region Public properties

    public string FullName
    {
      get { return declaringType == null ? name : declaringType.Name + "/" + name; }
    }

    public NestedTypes MetaType
    {
      get { return metaType; }
    }

    public string AttributesString
    {
      get { return MyClassInfo.MyClassAttributesToString(attributes); }
    }

    public bool IsPublic
    {
      get { return (attributes & MyClassAttributes.Public) != 0; }
    }

    public bool IsProtected
    {
      get { return (attributes & MyClassAttributes.Protected) != 0; }
    }

    public bool IsInternal
    {
      get { return (attributes & MyClassAttributes.Internal) != 0; }
    }

    public bool IsPrivate
    {
      get { return (attributes & MyClassAttributes.Private) != 0; }
    }

    public bool IsProtectedInternal
    {
      get { return IsProtected && IsInternal; }
    }

    public bool IsStatic
    {
      get { return (attributes & MyClassAttributes.Static) == MyClassAttributes.Static; }
    }

    public bool IsAbstract
    {
      get { return (attributes & MyClassAttributes.Abstract) == MyClassAttributes.Abstract; }
    }

    public bool IsSealed
    {
      get { return (attributes & MyClassAttributes.Sealed) == MyClassAttributes.Sealed; }
    }

    #endregion

    #region ISummarisableMember Members

    public string DisplayableName
    {
      get { return Utils.GetUnqualifiedName(name); }
    }

    #endregion

    #region MetaClass overrides

    public override string GetMetaName()
    {
      return "Nested Type";
    }

    #endregion
  }
}
