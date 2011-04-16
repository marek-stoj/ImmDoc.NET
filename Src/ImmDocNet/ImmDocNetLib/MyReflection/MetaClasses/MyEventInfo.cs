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
using System.Diagnostics;

using Imm.ImmDocNetLib.MyReflection.Attributes;
using Imm.ImmDocNetLib.Documenters;

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
  class MyEventInfo : MetaClass, ISummarisableMember
  {
    private string typeFullName;
    private string typeFullNameWithoutRevArrayStrings;
    private MyEventAttributes attributes;
    private MyInvokableMemberAttributes underlyingMethodsAttributes;
    private string example = String.Empty;

    #region Constructor(s)

    public MyEventInfo(EventDefinition eventDefinition, MyClassInfo declaringType)
      : base()
    {
      this.name = eventDefinition.Name;

      string[] readableForms = Tools.GetHumanReadableForms(eventDefinition.EventType);
      this.typeFullName = readableForms[0];
      this.typeFullNameWithoutRevArrayStrings = readableForms[1];

      this.declaringType = declaringType;

      MethodDefinition adderInfo = eventDefinition.AddMethod;
      Debug.Assert(adderInfo != null, "Impossible! add_Event() must have been generated.");

      this.attributes = GetMyEventAttributes(eventDefinition);
      this.underlyingMethodsAttributes = GetMyInvokableMemberAttributes(adderInfo);

      this.CheckSupport(eventDefinition, adderInfo.Attributes);
    }

    #endregion

    #region Private helper methods

    private void CheckSupport(EventDefinition eventInfo, MethodAttributes methodAttributes)
    {
      EventAttributes eventAttributes = eventInfo.Attributes;

      string warningTemplate = "Event '" + name + "' has unsupported attribute: '{0}'.";

      // in order to reduce output we warn only about important attributes which are not currently
      // supported:

      // EventDefinition properties

      // EventAttributes
      //if ((eventAttributes & EventAttributes.RTSpecialName) != 0) { Logger.Warning(warningTemplate, "RTSpecialName"); }
      //if ((eventAttributes & EventAttributes.SpecialName) != 0) { Logger.Warning(warningTemplate, "SpecialName"); }

      // MethodAttributes
      //if ((methodAttributes & MethodAttributes.CheckAccessOnOverride) != 0) { Logger.Warning(warningTemplate, "CheckAccessOnOverride"); }
      //if ((methodAttributes & MethodAttributes.FamANDAssem) != 0) { Logger.Warning(warningTemplate, "FamANDAssem"); }
      // TODO: support this: if ((methodAttributes & MethodAttributes.HasSecurity) != 0) { Logger.Warning(warningTemplate, "HasSecurity"); }
      //if ((methodAttributes & MethodAttributes.HideBySig) != 0) { Logger.Warning(warningTemplate, "HideBySig"); }
      //if ((methodAttributes & MethodAttributes.NewSlot) != 0) { Logger.Warning(warningTemplate, "NewSlot"); }
      // TODO: support this: if ((methodAttributes & MethodAttributes.PinvokeImpl) != 0) { Logger.Warning(warningTemplate, "PinvokeImpl"); }
      //if ((methodAttributes & MethodAttributes.PrivateScope) != 0) { Logger.Warning(warningTemplate, "PrivateScope"); }
      // TODO: support this: if ((methodAttributes & MethodAttributes.RequireSecObject) != 0) { Logger.Warning(warningTemplate, "RequiresSecObject"); }
      //if ((methodAttributes & MethodAttributes.ReuseSlot) != 0) { Logger.Warning(warningTemplate, "ReuseSlot"); }
      //if ((methodAttributes & MethodAttributes.RTSpecialName) != 0) { Logger.Warning(warningTemplate, "RTSpecialName"); }
      //if ((methodAttributes & MethodAttributes.SpecialName) != 0) { Logger.Warning(warningTemplate, "SpecialName"); }
      // TODO: support this: if ((methodAttributes & MethodAttributes.UnmanagedExport) != 0) { Logger.Warning(warningTemplate, "UnmanagedExport"); }
    }

    private static MyEventAttributes GetMyEventAttributes(EventDefinition eventDefinition)
    {
      MyEventAttributes myEventAttributes = MyEventAttributes.None;

      return myEventAttributes;
    }

    private static MyInvokableMemberAttributes GetMyInvokableMemberAttributes(MethodDefinition methodDefinition)
    {
      return MyInvokableMemberInfo.GetMyInvokableMemberAttributes(methodDefinition);
    }

    private static string MyEventAndMyInvokableMemberAttributesToString(MyEventAttributes myEventAttributes, MyInvokableMemberAttributes myInvokableMemberAttributes)
    {
      // for now only MyInvokableMemberInfo attributes
      return MyInvokableMemberInfo.MyInvokableMemberAttributesToString(myInvokableMemberAttributes);
    }

    #endregion

    #region Public properties

    public string TypeFullName
    {
      get { return typeFullName; }
    }

    public string AttributesString
    {
      get { return MyEventAndMyInvokableMemberAttributesToString(attributes, underlyingMethodsAttributes); }
    }

    public bool IsPublic
    {
      get { return (underlyingMethodsAttributes & MyInvokableMemberAttributes.Public) != 0; }
    }

    public bool IsProtected
    {
      get { return (underlyingMethodsAttributes & MyInvokableMemberAttributes.Protected) != 0; }
    }

    public bool IsInternal
    {
      get { return (underlyingMethodsAttributes & MyInvokableMemberAttributes.Internal) != 0; }
    }

    public bool IsPrivate
    {
      get { return (underlyingMethodsAttributes & MyInvokableMemberAttributes.Private) != 0; }
    }

    public bool IsProtectedInternal
    {
      get { return IsProtected && IsInternal; }
    }

    public bool IsStatic
    {
      get { return (underlyingMethodsAttributes & MyInvokableMemberAttributes.Static) != 0; }
    }

    public bool IsAbstract
    {
      get { return (underlyingMethodsAttributes & MyInvokableMemberAttributes.Abstract) != 0; }
    }

    public bool IsVirtual
    {
      get { return (underlyingMethodsAttributes & MyInvokableMemberAttributes.Virtual) != 0; }
    }

    public string Example
    {
      get { return example; }
      set { example = value; }
    }

    #endregion

    #region ISummarisableMember Members

    public string DisplayableName
    {
      get { return name; }
    }

    #endregion

    #region MetaClass overrides

    public override string GetMetaName()
    {
      return "Event";
    }

    #endregion
  }
}
