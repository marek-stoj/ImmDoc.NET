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
using Mono.Cecil;
using System.Diagnostics;

using Imm.ImmDocNetLib.MyReflection.Attributes;
using Imm.ImmDocNetLib.Documenters;
using Mono.Cecil.Metadata;
using Mono.Collections.Generic;

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
  public class MyClassInfo : MetaClass, ISummarisableMember
  {
    public const string SPECIAL_MODULE_CLASS_NAME = "<Module>";

    protected string assemblyName;
    private string namezpace;
    private MyClassAttributes attributes;
    private Dictionary<ClassMembersGroups, Dictionary<string, MetaClass>> membersGroups;
    private Dictionary<ClassMembersGroups, Dictionary<string, string>> genericNamesMappings;

    private string baseTypeName;
    private List<string> implementedInterfacesNames;

    private List<MyGenericParameterInfo> genericParameters;
    private List<string> allGenericParametersNames;
    private string example = String.Empty;

    #region Constructor(s)

    protected MyClassInfo()
      : base()
    {
      this.membersGroups = new Dictionary<ClassMembersGroups, Dictionary<string, MetaClass>>();
      this.implementedInterfacesNames = new List<string>();
    }

    public MyClassInfo(TypeDefinition typeDefinition, string assemblyName)
      : this()
    {
      Debug.Assert(typeDefinition.IsClass, "Impossible! Given type is not a class type.");

      this.assemblyName = assemblyName;

      this.Initialize(typeDefinition);
      this.AddMembers(typeDefinition);
      this.CheckSupport(typeDefinition);
    }

    #endregion

    #region Protected helper methods

    protected void AddMembers(TypeDefinition typeDefinition)
    {
      var members = Utils.GetTypeMembers(typeDefinition);

      foreach (MemberReference memberInfo in members)
      {
        if (Utils.ShouldIncludeMember(memberInfo))
        {
          AddMember(memberInfo, typeDefinition);
        }
      }
    }

    protected void Initialize(TypeDefinition typeDefinition)
    {
      string[] readableForms = Tools.GetHumanReadableForms(typeDefinition);

      this.name = Utils.GetUnqualifiedName(readableForms[0]);

      if (Utils.IsTypeGeneric(typeDefinition))
      {
        allGenericParametersNames =
          Tools.ExamineGenericParameters(typeDefinition.GenericParameters,
                                         typeDefinition.DeclaringType,
                                         out genericParameters,
                                         true);
      }

      this.namezpace = Utils.GetTypeNamespace(typeDefinition);
      this.attributes = GetMyClassAttributes(typeDefinition);

      if (typeDefinition.BaseType != null)
      {
        readableForms = Tools.GetHumanReadableForms(typeDefinition.BaseType);
        this.baseTypeName = readableForms[0];
      }

      var interfaces = typeDefinition.Interfaces;

      if (interfaces != null)
      {
        for (int i = 0; i < interfaces.Count; i++)
        {
          readableForms = Tools.GetHumanReadableForms(interfaces[i]);

          implementedInterfacesNames.Add(readableForms[0]);
        }
      }
    }

    protected virtual void CheckSupport(TypeDefinition typeDefinition)
    {
      TypeAttributes typeAttributes = typeDefinition.Attributes;
      string metaType = "UnknownMetaType";

      if (Utils.IsDelegate(typeDefinition)) { metaType = "Delegate"; }
      else if (typeDefinition.IsClass) { metaType = "Class"; }
      else if (typeDefinition.IsInterface) { metaType = "Interface"; }
      else if (typeDefinition.IsValueType) { metaType = "Structure"; }
      else if (typeDefinition.IsEnum) { metaType = "Enumeration"; }
      else { metaType = "UnknownMetaType"; }

      string warningTemplate = String.Format("{0} '{1}' has unsupported attribute: '{{0}}'.", metaType, name.Replace("{", "{{").Replace("}", "}}"));

      // in order to reduce output we warn only about important attributes which are not currently
      // supported:

      //if ((typeAttributes & TypeAttributes.AnsiClass) != 0) { Logger.Warning(warningTemplate, "AnsiClass"); }
      // TODO: support this: if ((typeAttributes & TypeAttributes.AutoClass) != 0) { Logger.Warning(warningTemplate, "AutoClass"); }
      //if ((typeAttributes & TypeAttributes.AutoLayout) != 0) { Logger.Warning(warningTemplate, "AutoLayout"); }
      //if ((typeAttributes & TypeAttributes.BeforeFieldInit) != 0) { Logger.Warning(warningTemplate, "BeforeFieldInit"); }
      //if ((typeAttributes & TypeAttributes.CustomFormatClass) != 0) { Logger.Warning(warningTemplate, "CustomFormatClass"); }
      //if ((typeAttributes & TypeAttributes.ExplicitLayout) != 0) { Logger.Warning(warningTemplate, "ExplicitLayout"); }
      // TODO: support this: if ((typeAttributes & TypeAttributes.HasSecurity) != 0) { Logger.Warning(warningTemplate, "HasSecurity"); }
      // TODO: support this: if ((typeAttributes & TypeAttributes.Import) != 0) { Logger.Warning(warningTemplate, "Import"); }
      //if ((typeAttributes & TypeAttributes.NestedFamANDAssem) != 0) { Logger.Warning(warningTemplate, "NestedFamANDAssem"); }
      //if ((typeAttributes & TypeAttributes.RTSpecialName) != 0) { Logger.Warning(warningTemplate, "RTSpecialName"); }
      //if ((typeAttributes & TypeAttributes.SequentialLayout) != 0) { Logger.Warning(warningTemplate, "SequentialLayout"); }
      // TODO: support this: if ((typeAttributes & TypeAttributes.Serializable) != 0) { Logger.Warning(warningTemplate, "Serializable"); }
      //if ((typeAttributes & TypeAttributes.SpecialName) != 0) { Logger.Warning(warningTemplate, "SpecialName"); }
      // TODO: support this: if ((typeAttributes & TypeAttributes.UnicodeClass) != 0) { Logger.Warning(warningTemplate, "UnicodeClass"); }
    }

    protected virtual void AddField(FieldDefinition fieldDefinition)
    {
      MyFieldInfo myFieldInfo = new MyFieldInfo(fieldDefinition, this);
      Dictionary<string, MetaClass> membersGroup;

      if (myFieldInfo.IsPublic)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PublicFields);
      }
      else if (myFieldInfo.IsProtectedInternal)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedInternalFields);
      }
      else if (myFieldInfo.IsProtected)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedFields);
      }
      else if (myFieldInfo.IsInternal)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.InternalFields);
      }
      else if (myFieldInfo.IsPrivate)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PrivateFields);
      }
      else
      {
        Debug.Assert(false, "Impossible! Visibility of a member is not supported.");
        return;
      }

      if (!membersGroup.ContainsKey(fieldDefinition.Name))
      {
        membersGroup.Add(fieldDefinition.Name, myFieldInfo);
      }
      else
      {
        Logger.Warning("Field named '{0}' has already been added to type {1}.", fieldDefinition.Name, name);
      }
    }

    protected virtual void AddConstructor(MethodDefinition constructorDefinition)
    {
      Debug.Assert(constructorDefinition.IsConstructor);

      MyConstructorInfo myConstructorInfo = new MyConstructorInfo(constructorDefinition, this);
      Dictionary<string, MetaClass> membersGroup;

      if (myConstructorInfo.IsPublic)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PublicConstructors);
      }
      else if (myConstructorInfo.IsProtectedInternal)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedInternalConstructors);
      }
      else if (myConstructorInfo.IsProtected)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedConstructors);
      }
      else if (myConstructorInfo.IsInternal)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.InternalConstructors);
      }
      else if (myConstructorInfo.IsPrivate)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PrivateConstructors);
      }
      else
      {
        Debug.Assert(false, "Impossible! Visibility of a member is not supported.");
        return;
      }

      MyInvokableMembersOverloadsInfo constructorsOverloads;

      if (!membersGroup.ContainsKey(myConstructorInfo.Name))
      {
        constructorsOverloads = new MyInvokableMembersOverloadsInfo(myConstructorInfo.Name);
        membersGroup[myConstructorInfo.Name] = constructorsOverloads;
      }
      else
      {
        constructorsOverloads = (MyInvokableMembersOverloadsInfo)membersGroup[myConstructorInfo.Name];
      }

      constructorsOverloads.AddInvokableMember(myConstructorInfo);
    }

    protected virtual void AddMethod(MethodDefinition methodDefinition)
    {
      Debug.Assert(!methodDefinition.IsConstructor);

      MyMethodInfo myMethodInfo = new MyMethodInfo(methodDefinition, this);
      Dictionary<string, MetaClass> membersGroup;
      ClassMembersGroups classMembersGroupType;

      if (myMethodInfo.IsPublic)
      {
        classMembersGroupType = ClassMembersGroups.PublicMethodsOverloads;
      }
      else if (myMethodInfo.IsProtectedInternal)
      {
        classMembersGroupType = ClassMembersGroups.ProtectedInternalMethodsOverloads;
      }
      else if (myMethodInfo.IsProtected)
      {
        classMembersGroupType = ClassMembersGroups.ProtectedMethodsOverloads;
      }
      else if (myMethodInfo.IsInternal)
      {
        classMembersGroupType = ClassMembersGroups.InternalMethodsOverloads;
      }
      else if (myMethodInfo.IsPrivate)
      {
        classMembersGroupType = ClassMembersGroups.PrivateMethodsOverloads;
      }
      else
      {
        Debug.Assert(false, "Impossible! Visibility of a member is not supported.");
        return;
      }

      membersGroup = CreateAndGetMembersGroup(classMembersGroupType);

      MyInvokableMembersOverloadsInfo methodsOverloads;
      string overloadName = myMethodInfo.Name;
      bool overloadExists = false;

      if (myMethodInfo.GenericParametersCount > 0)
      {
        overloadName = Utils.ConvertNameToXmlDocForm(myMethodInfo.Name, true);
      }

      if (membersGroup.ContainsKey(overloadName))
      {
        var tmpMethodsOverloads = (MyInvokableMembersOverloadsInfo)membersGroup[overloadName];

        overloadExists = tmpMethodsOverloads.Count > 0
                      && tmpMethodsOverloads[0] is MyMethodInfo
                      && ((MyMethodInfo)tmpMethodsOverloads[0]).GenericParametersCount == myMethodInfo.GenericParametersCount;
      }

      if (!overloadExists)
      {
        methodsOverloads = new MyInvokableMembersOverloadsInfo(myMethodInfo.Name);
        membersGroup[overloadName] = methodsOverloads;

        AddGenericNameMappingIfNeeded(myMethodInfo, classMembersGroupType);
      }
      else
      {
        methodsOverloads = (MyInvokableMembersOverloadsInfo)membersGroup[overloadName];
      }

      methodsOverloads.AddInvokableMember(myMethodInfo);
    }

    private void AddGenericNameMappingIfNeeded(MetaClass metaClass, ClassMembersGroups classMembersGroupType)
    {
      if (!metaClass.Name.Contains("<"))
      {
        return;
      }

      Dictionary<string, string> genericNamesMappingsForGroup = GetGenericNamesMappingsForGroup(classMembersGroupType);
      string xmlName = Utils.ConvertNameToXmlDocForm(metaClass.Name, true);

      genericNamesMappingsForGroup[xmlName] = metaClass.Name;
    }

    protected virtual void AddProperty(PropertyDefinition propertyDefinition)
    {
      MyPropertyInfo myPropertyInfo = new MyPropertyInfo(propertyDefinition, this);
      Dictionary<string, MetaClass> membersGroup;

      if (myPropertyInfo.IsPublic)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PublicPropertiesOverloads);
      }
      else if (myPropertyInfo.IsProtectedInternal)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedInternalPropertiesOverloads);
      }
      else if (myPropertyInfo.IsProtected)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedPropertiesOverloads);
      }
      else if (myPropertyInfo.IsInternal)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.InternalPropertiesOverloads);
      }
      else if (myPropertyInfo.IsPrivate)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PrivatePropertiesOverloads);
      }
      else
      {
        Debug.Assert(false, "Impossible! Visibility of a member is not supported.");
        return;
      }

      MyPropertiesOverloadsInfo propertiesOverloads;
      if (!membersGroup.ContainsKey(myPropertyInfo.Name))
      {
        propertiesOverloads = new MyPropertiesOverloadsInfo(myPropertyInfo.Name);
        membersGroup[myPropertyInfo.Name] = propertiesOverloads;
      }
      else
      {
        propertiesOverloads = (MyPropertiesOverloadsInfo)membersGroup[myPropertyInfo.Name];
      }

      propertiesOverloads.AddProperty(myPropertyInfo);
    }

    protected virtual void AddEvent(EventDefinition eventDefinition)
    {
      MyEventInfo myEventInfo = new MyEventInfo(eventDefinition, this);
      Dictionary<string, MetaClass> membersGroup;

      if (myEventInfo.IsPublic)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PublicEvents);
      }
      else if (myEventInfo.IsProtectedInternal)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedInternalEvents);
      }
      else if (myEventInfo.IsProtected)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedEvents);
      }
      else if (myEventInfo.IsInternal)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.InternalEvents);
      }
      else if (myEventInfo.IsPrivate)
      {
        membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PrivateEvents);
      }
      else
      {
        Debug.Assert(false, "Impossible! Visibility of a member is not supported.");
        return;
      }

      if (!membersGroup.ContainsKey(eventDefinition.Name))
      {
        membersGroup.Add(eventDefinition.Name, myEventInfo);
      }
      else
      {
        Logger.Warning("Event named '{0}' has already been added to type {1}.", eventDefinition.Name, name);
      }
    }

    protected virtual void AddNestedType(TypeDefinition typeDefinition)
    {
      MyNestedTypeInfo myNestedTypeInfo = new MyNestedTypeInfo(typeDefinition, this);
      Dictionary<string, MetaClass> membersGroup = null;

      if (myNestedTypeInfo.IsPublic)
      {
        switch (myNestedTypeInfo.MetaType)
        {
          case NestedTypes.Class: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PublicClasses); break; }
          case NestedTypes.Structure: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PublicStructures); break; }
          case NestedTypes.Interface: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PublicInterfaces); break; }
          case NestedTypes.Delegate: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PublicDelegates); break; }
          case NestedTypes.Enumeration: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PublicEnumerations); break; }

          default: break;
        }
      }
      else if (myNestedTypeInfo.IsProtectedInternal)
      {
        switch (myNestedTypeInfo.MetaType)
        {
          case NestedTypes.Class: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedInternalClasses); break; }
          case NestedTypes.Structure: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedInternalStructures); break; }
          case NestedTypes.Interface: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedInternalInterfaces); break; }
          case NestedTypes.Delegate: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedInternalDelegates); break; }
          case NestedTypes.Enumeration: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedInternalEnumerations); break; }

          default: break;
        }
      }
      else if (myNestedTypeInfo.IsProtected)
      {
        switch (myNestedTypeInfo.MetaType)
        {
          case NestedTypes.Class: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedClasses); break; }
          case NestedTypes.Structure: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedStructures); break; }
          case NestedTypes.Interface: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedInterfaces); break; }
          case NestedTypes.Delegate: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedDelegates); break; }
          case NestedTypes.Enumeration: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.ProtectedEnumerations); break; }

          default: break;
        }
      }
      else if (myNestedTypeInfo.IsInternal)
      {
        switch (myNestedTypeInfo.MetaType)
        {
          case NestedTypes.Class: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.InternalClasses); break; }
          case NestedTypes.Structure: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.InternalStructures); break; }
          case NestedTypes.Interface: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.InternalInterfaces); break; }
          case NestedTypes.Delegate: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.InternalDelegates); break; }
          case NestedTypes.Enumeration: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.InternalEnumerations); break; }

          default: break;
        }
      }
      else if (myNestedTypeInfo.IsPrivate)
      {
        switch (myNestedTypeInfo.MetaType)
        {
          case NestedTypes.Class: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PrivateClasses); break; }
          case NestedTypes.Structure: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PrivateStructures); break; }
          case NestedTypes.Interface: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PrivateInterfaces); break; }
          case NestedTypes.Delegate: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PrivateDelegates); break; }
          case NestedTypes.Enumeration: { membersGroup = CreateAndGetMembersGroup(ClassMembersGroups.PrivateEnumerations); break; }

          default: break;
        }
      }
      else
      {
        Debug.Assert(false, "Impossible! Visibility of a member is not supported.");
        return;
      }

      if (membersGroup != null)
      {
        membersGroup.Add(myNestedTypeInfo.Name, myNestedTypeInfo);
      }
      else
      {
        Debug.Assert(false, "Impossible! Couldn't obtain members group type.");
      }
    }

    protected Dictionary<string, MetaClass> GetMembersGroup(ClassMembersGroups classMembersGroupType)
    {
      if (!membersGroups.ContainsKey(classMembersGroupType))
      {
        return null;
      }

      return membersGroups[classMembersGroupType];
    }

    #endregion

    #region Public helper methods

    public static MyClassAttributes GetMyClassAttributes(TypeDefinition typeDefinition)
    {
      MyClassAttributes myClassAttributes = MyClassAttributes.None;

      if (typeDefinition.IsPublic) { myClassAttributes |= MyClassAttributes.Public; }
      if (typeDefinition.IsAbstract) { myClassAttributes |= MyClassAttributes.Abstract; }
      if (typeDefinition.IsSealed) { myClassAttributes |= MyClassAttributes.Sealed; }
      if (typeDefinition.IsNotPublic && !Utils.IsTypeNested(typeDefinition)) { myClassAttributes |= MyClassAttributes.Internal; }

      if (typeDefinition.IsNestedPublic) { myClassAttributes |= MyClassAttributes.Public; }
      else if (typeDefinition.IsNestedFamilyOrAssembly) { myClassAttributes |= MyClassAttributes.Internal | MyClassAttributes.Protected; }
      else if (typeDefinition.IsNestedAssembly) { myClassAttributes |= MyClassAttributes.Internal; }
      else if (typeDefinition.IsNestedFamily) { myClassAttributes |= MyClassAttributes.Protected; }
      else if (typeDefinition.IsNestedPrivate) { myClassAttributes |= MyClassAttributes.Private; }

      return myClassAttributes;
    }

    public static string MyClassAttributesToString(MyClassAttributes myClassAttributes)
    {
      StringBuilder sb = new StringBuilder();

      if ((myClassAttributes & MyClassAttributes.Public) != 0) { sb.Append("public "); }
      else if ((myClassAttributes & MyClassAttributes.Private) != 0) { sb.Append("private "); }
      else if ((myClassAttributes & MyClassAttributes.Protected) != 0) { sb.Append("protected "); }

      if ((myClassAttributes & MyClassAttributes.Internal) != 0) { sb.Append("internal "); }
      if ((myClassAttributes & MyClassAttributes.Abstract) != 0 && (myClassAttributes & MyClassAttributes.Sealed) != 0) { sb.Append("static "); }
      else if (((myClassAttributes & MyClassAttributes.Sealed)) != 0) { sb.Append("sealed "); }
      else if (((myClassAttributes & MyClassAttributes.Abstract)) != 0) { sb.Append("abstract "); }

      if (sb.Length > 0)
      {
        sb.Length = sb.Length - 1;
      }

      return sb.ToString();
    }

    public static string ClassMembersGroupsToString(ClassMembersGroups classMembersGroupType)
    {
      switch (classMembersGroupType)
      {
        case ClassMembersGroups.PublicClasses: return "Public Classes";
        case ClassMembersGroups.PublicInterfaces: return "Public Interfaces";
        case ClassMembersGroups.PublicStructures: return "Public Structures";
        case ClassMembersGroups.PublicDelegates: return "Public Delegates";
        case ClassMembersGroups.PublicEnumerations: return "Public Enumerations";
        case ClassMembersGroups.PublicConstructors: return "Public Constructors";
        case ClassMembersGroups.PublicMethodsOverloads: return "Public Methods";
        case ClassMembersGroups.PublicFields: return "Public Fields";
        case ClassMembersGroups.PublicEvents: return "Public Events";
        case ClassMembersGroups.PublicPropertiesOverloads: return "Public Properties";
        case ClassMembersGroups.ProtectedClasses: return "Protected Classes";
        case ClassMembersGroups.ProtectedInterfaces: return "Protected Interfaces";
        case ClassMembersGroups.ProtectedStructures: return "Protected Structures";
        case ClassMembersGroups.ProtectedDelegates: return "Protected Delegates";
        case ClassMembersGroups.ProtectedEnumerations: return "Protected Enumerations";
        case ClassMembersGroups.ProtectedConstructors: return "Protected Constructors";
        case ClassMembersGroups.ProtectedMethodsOverloads: return "Protected Methods";
        case ClassMembersGroups.ProtectedFields: return "Protected Fields";
        case ClassMembersGroups.ProtectedEvents: return "Protected Events";
        case ClassMembersGroups.ProtectedPropertiesOverloads: return "Protected Properties";
        case ClassMembersGroups.InternalClasses: return "Internal Classes";
        case ClassMembersGroups.InternalInterfaces: return "Internal Interfaces";
        case ClassMembersGroups.InternalStructures: return "Internal Structures";
        case ClassMembersGroups.InternalDelegates: return "Internal Delegates";
        case ClassMembersGroups.InternalEnumerations: return "Internal Enumerations";
        case ClassMembersGroups.InternalConstructors: return "Internal Constructors";
        case ClassMembersGroups.InternalMethodsOverloads: return "Internal Methods";
        case ClassMembersGroups.InternalFields: return "Internal Fields";
        case ClassMembersGroups.InternalEvents: return "Internal Events";
        case ClassMembersGroups.InternalPropertiesOverloads: return "Internal Properties";
        case ClassMembersGroups.ProtectedInternalClasses: return "Protected Internal Classes";
        case ClassMembersGroups.ProtectedInternalInterfaces: return "Protected Internal Interfaces";
        case ClassMembersGroups.ProtectedInternalStructures: return "Protected Internal Structures";
        case ClassMembersGroups.ProtectedInternalDelegates: return "Protected Internal Delegates";
        case ClassMembersGroups.ProtectedInternalEnumerations: return "Protected Internal Enumerations";
        case ClassMembersGroups.ProtectedInternalConstructors: return "Protected Internal Constructors";
        case ClassMembersGroups.ProtectedInternalMethodsOverloads: return "Protected Internal Methods";
        case ClassMembersGroups.ProtectedInternalFields: return "Protected Internal Fields";
        case ClassMembersGroups.ProtectedInternalEvents: return "Protected Internal Events";
        case ClassMembersGroups.ProtectedInternalPropertiesOverloads: return "Protected Internal Properties";
        case ClassMembersGroups.PrivateClasses: return "Private Classes";
        case ClassMembersGroups.PrivateInterfaces: return "Private Interfaces";
        case ClassMembersGroups.PrivateStructures: return "Private Structures";
        case ClassMembersGroups.PrivateDelegates: return "Private Delegates";
        case ClassMembersGroups.PrivateEnumerations: return "Private Enumerations";
        case ClassMembersGroups.PrivateConstructors: return "Private Constructors";
        case ClassMembersGroups.PrivateMethodsOverloads: return "Private Methods";
        case ClassMembersGroups.PrivateFields: return "Private Fields";
        case ClassMembersGroups.PrivateEvents: return "Private Events";
        case ClassMembersGroups.PrivatePropertiesOverloads: return "Private Properties";

        default: return classMembersGroupType.ToString();
      }
    }

    public static string GetBaseGroupName(ClassMembersGroups classMembersGroupType)
    {
      switch (classMembersGroupType)
      {
        case ClassMembersGroups.PublicClasses: { return "Classes"; }
        case ClassMembersGroups.PublicInterfaces: { return "Interfaces"; }
        case ClassMembersGroups.PublicStructures: { return "Structures"; }
        case ClassMembersGroups.PublicDelegates: { return "Delegates"; }
        case ClassMembersGroups.PublicEnumerations: { return "Enumerations"; }
        case ClassMembersGroups.PublicFields: { return "Fields"; }
        case ClassMembersGroups.PublicConstructors: { return "Constructors"; }
        case ClassMembersGroups.PublicMethodsOverloads: { return "Methods"; }
        case ClassMembersGroups.PublicPropertiesOverloads: { return "Properties"; }
        case ClassMembersGroups.PublicEvents: { return "Events"; }
        case ClassMembersGroups.ProtectedClasses: { return "Classes"; }
        case ClassMembersGroups.ProtectedInterfaces: { return "Interfaces"; }
        case ClassMembersGroups.ProtectedStructures: { return "Structures"; }
        case ClassMembersGroups.ProtectedDelegates: { return "Delegates"; }
        case ClassMembersGroups.ProtectedEnumerations: { return "Enumerations"; }
        case ClassMembersGroups.ProtectedFields: { return "Fields"; }
        case ClassMembersGroups.ProtectedConstructors: { return "Constructors"; }
        case ClassMembersGroups.ProtectedMethodsOverloads: { return "Methods"; }
        case ClassMembersGroups.ProtectedPropertiesOverloads: { return "Properties"; }
        case ClassMembersGroups.ProtectedEvents: { return "Events"; }
        case ClassMembersGroups.InternalClasses: { return "Classes"; }
        case ClassMembersGroups.InternalInterfaces: { return "Interfaces"; }
        case ClassMembersGroups.InternalStructures: { return "Structures"; }
        case ClassMembersGroups.InternalDelegates: { return "Delegates"; }
        case ClassMembersGroups.InternalEnumerations: { return "Enumerations"; }
        case ClassMembersGroups.InternalFields: { return "Fields"; }
        case ClassMembersGroups.InternalConstructors: { return "Constructors"; }
        case ClassMembersGroups.InternalMethodsOverloads: { return "Methods"; }
        case ClassMembersGroups.InternalPropertiesOverloads: { return "Properties"; }
        case ClassMembersGroups.InternalEvents: { return "Events"; }
        case ClassMembersGroups.ProtectedInternalClasses: { return "Classes"; }
        case ClassMembersGroups.ProtectedInternalInterfaces: { return "Interfaces"; }
        case ClassMembersGroups.ProtectedInternalStructures: { return "Structures"; }
        case ClassMembersGroups.ProtectedInternalDelegates: { return "Delegates"; }
        case ClassMembersGroups.ProtectedInternalEnumerations: { return "Enumerations"; }
        case ClassMembersGroups.ProtectedInternalFields: { return "Fields"; }
        case ClassMembersGroups.ProtectedInternalConstructors: { return "Constructors"; }
        case ClassMembersGroups.ProtectedInternalMethodsOverloads: { return "Methods"; }
        case ClassMembersGroups.ProtectedInternalPropertiesOverloads: { return "Properties"; }
        case ClassMembersGroups.ProtectedInternalEvents: { return "Events"; }
        case ClassMembersGroups.PrivateClasses: { return "Classes"; }
        case ClassMembersGroups.PrivateInterfaces: { return "Interfaces"; }
        case ClassMembersGroups.PrivateStructures: { return "Structures"; }
        case ClassMembersGroups.PrivateDelegates: { return "Delegates"; }
        case ClassMembersGroups.PrivateEnumerations: { return "Enumerations"; }
        case ClassMembersGroups.PrivateFields: { return "Fields"; }
        case ClassMembersGroups.PrivateConstructors: { return "Constructors"; }
        case ClassMembersGroups.PrivateMethodsOverloads: { return "Methods"; }
        case ClassMembersGroups.PrivatePropertiesOverloads: { return "Properties"; }
        case ClassMembersGroups.PrivateEvents: { return "Events"; }

        default:
          {
            Debug.Assert(false, "Impossible! Couldn't recognize type of a class member.");

            return null;
          }
      }
    }

    public static bool IsMembersGroupPublic(ClassMembersGroups classMembersGroupType)
    {
      switch (classMembersGroupType)
      {
        case ClassMembersGroups.PublicClasses: { return true; }
        case ClassMembersGroups.PublicInterfaces: { return true; }
        case ClassMembersGroups.PublicStructures: { return true; }
        case ClassMembersGroups.PublicDelegates: { return true; }
        case ClassMembersGroups.PublicEnumerations: { return true; }
        case ClassMembersGroups.PublicFields: { return true; }
        case ClassMembersGroups.PublicConstructors: { return true; }
        case ClassMembersGroups.PublicMethodsOverloads: { return true; }
        case ClassMembersGroups.PublicPropertiesOverloads: { return true; }
        case ClassMembersGroups.PublicEvents: { return true; }

        default: return false;
      }
    }

    public bool HasProtectedGroupOfTheSameType(ClassMembersGroups classMembersGroupType)
    {
      switch (classMembersGroupType)
      {
        case ClassMembersGroups.PublicClasses: { return GetMembersCount(ClassMembersGroups.ProtectedClasses) > 0; }
        case ClassMembersGroups.PublicInterfaces: { return GetMembersCount(ClassMembersGroups.ProtectedInterfaces) > 0; }
        case ClassMembersGroups.PublicStructures: { return GetMembersCount(ClassMembersGroups.ProtectedStructures) > 0; }
        case ClassMembersGroups.PublicDelegates: { return GetMembersCount(ClassMembersGroups.ProtectedDelegates) > 0; }
        case ClassMembersGroups.PublicEnumerations: { return GetMembersCount(ClassMembersGroups.ProtectedEnumerations) > 0; }
        case ClassMembersGroups.PublicFields: { return GetMembersCount(ClassMembersGroups.ProtectedFields) > 0; }
        case ClassMembersGroups.PublicConstructors: { return GetMembersCount(ClassMembersGroups.ProtectedConstructors) > 0; }
        case ClassMembersGroups.PublicMethodsOverloads: { return GetMembersCount(ClassMembersGroups.ProtectedMethodsOverloads) > 0; }
        case ClassMembersGroups.PublicPropertiesOverloads: { return GetMembersCount(ClassMembersGroups.ProtectedPropertiesOverloads) > 0; }
        case ClassMembersGroups.PublicEvents: { return GetMembersCount(ClassMembersGroups.ProtectedEvents) > 0; }

        default: return false;
      }
    }

    public bool HasProtectedInternalGroupOfTheSameType(ClassMembersGroups classMembersGroupType)
    {
      switch (classMembersGroupType)
      {
        case ClassMembersGroups.PublicClasses: { return GetMembersCount(ClassMembersGroups.ProtectedInternalClasses) > 0; }
        case ClassMembersGroups.PublicInterfaces: { return GetMembersCount(ClassMembersGroups.ProtectedInternalInterfaces) > 0; }
        case ClassMembersGroups.PublicStructures: { return GetMembersCount(ClassMembersGroups.ProtectedInternalStructures) > 0; }
        case ClassMembersGroups.PublicDelegates: { return GetMembersCount(ClassMembersGroups.ProtectedInternalDelegates) > 0; }
        case ClassMembersGroups.PublicEnumerations: { return GetMembersCount(ClassMembersGroups.ProtectedInternalEnumerations) > 0; }
        case ClassMembersGroups.PublicFields: { return GetMembersCount(ClassMembersGroups.ProtectedInternalFields) > 0; }
        case ClassMembersGroups.PublicConstructors: { return GetMembersCount(ClassMembersGroups.ProtectedInternalConstructors) > 0; }
        case ClassMembersGroups.PublicMethodsOverloads: { return GetMembersCount(ClassMembersGroups.ProtectedInternalMethodsOverloads) > 0; }
        case ClassMembersGroups.PublicPropertiesOverloads: { return GetMembersCount(ClassMembersGroups.ProtectedInternalPropertiesOverloads) > 0; }
        case ClassMembersGroups.PublicEvents: { return GetMembersCount(ClassMembersGroups.ProtectedInternalEvents) > 0; }

        default: return false;
      }
    }

    public bool HasInternalGroupOfTheSameType(ClassMembersGroups classMembersGroupType)
    {
      switch (classMembersGroupType)
      {
        case ClassMembersGroups.PublicClasses: { return GetMembersCount(ClassMembersGroups.InternalClasses) > 0; }
        case ClassMembersGroups.PublicInterfaces: { return GetMembersCount(ClassMembersGroups.InternalInterfaces) > 0; }
        case ClassMembersGroups.PublicStructures: { return GetMembersCount(ClassMembersGroups.InternalStructures) > 0; }
        case ClassMembersGroups.PublicDelegates: { return GetMembersCount(ClassMembersGroups.InternalDelegates) > 0; }
        case ClassMembersGroups.PublicEnumerations: { return GetMembersCount(ClassMembersGroups.InternalEnumerations) > 0; }
        case ClassMembersGroups.PublicFields: { return GetMembersCount(ClassMembersGroups.InternalFields) > 0; }
        case ClassMembersGroups.PublicConstructors: { return GetMembersCount(ClassMembersGroups.InternalConstructors) > 0; }
        case ClassMembersGroups.PublicMethodsOverloads: { return GetMembersCount(ClassMembersGroups.InternalMethodsOverloads) > 0; }
        case ClassMembersGroups.PublicPropertiesOverloads: { return GetMembersCount(ClassMembersGroups.InternalPropertiesOverloads) > 0; }
        case ClassMembersGroups.PublicEvents: { return GetMembersCount(ClassMembersGroups.InternalEvents) > 0; }

        default: return false;
      }
    }

    public bool HasPrivateGroupOfTheSameType(ClassMembersGroups classMembersGroupType)
    {
      switch (classMembersGroupType)
      {
        case ClassMembersGroups.PublicClasses: { return GetMembersCount(ClassMembersGroups.PrivateClasses) > 0; }
        case ClassMembersGroups.PublicInterfaces: { return GetMembersCount(ClassMembersGroups.PrivateInterfaces) > 0; }
        case ClassMembersGroups.PublicStructures: { return GetMembersCount(ClassMembersGroups.PrivateStructures) > 0; }
        case ClassMembersGroups.PublicDelegates: { return GetMembersCount(ClassMembersGroups.PrivateDelegates) > 0; }
        case ClassMembersGroups.PublicEnumerations: { return GetMembersCount(ClassMembersGroups.PrivateEnumerations) > 0; }
        case ClassMembersGroups.PublicFields: { return GetMembersCount(ClassMembersGroups.PrivateFields) > 0; }
        case ClassMembersGroups.PublicConstructors: { return GetMembersCount(ClassMembersGroups.PrivateConstructors) > 0; }
        case ClassMembersGroups.PublicMethodsOverloads: { return GetMembersCount(ClassMembersGroups.PrivateMethodsOverloads) > 0; }
        case ClassMembersGroups.PublicPropertiesOverloads: { return GetMembersCount(ClassMembersGroups.PrivatePropertiesOverloads) > 0; }
        case ClassMembersGroups.PublicEvents: { return GetMembersCount(ClassMembersGroups.PrivateEvents) > 0; }

        default: return false;
      }
    }

    #endregion

    #region Private helper methods

    private void AddMember(MemberReference member, TypeDefinition declaringClass)
    {
      switch (member.MetadataToken.TokenType)
      {
        case TokenType.Field:
          {
            FieldDefinition fieldDefinition = (FieldDefinition)member;

            if (!IsEventField(fieldDefinition, declaringClass))
            {
              AddField(fieldDefinition);
            }

            break;
          }

        case TokenType.Method:
          {
            MethodDefinition methodDefinition = (MethodDefinition)member;

            if (methodDefinition.IsConstructor)
            {
              AddConstructor(methodDefinition);
            }
            else
            {
              AddMethod(methodDefinition);
            }

            break;
          }

        case TokenType.Property:
          {
            AddProperty((PropertyDefinition)member);

            break;
          }

        case TokenType.Event:
          {
            AddEvent((EventDefinition)member);

            break;
          }

        case TokenType.TypeDef:
          {
            TypeDefinition nestedType = Utils.GetNestedType(declaringClass, member.Name);
            Debug.Assert(nestedType != null, "Impossible! This nested type should be present.");

            if (Utils.ShouldIncludeType(nestedType))
            {
              AddNestedType(nestedType);
            }

            break;
          }

        default:
          {
            Logger.Warning("Omitting member of class '{0}' named '{1}' of type '{2}'.", name, member.Name, member.MetadataToken.TokenType);

            break;
          }
      }
    }

    private bool IsEventField(FieldDefinition fieldDefinition, TypeDefinition declaringClass)
    {
      return Utils.GetEvent(declaringClass, fieldDefinition.Name) != null;
    }

    private Dictionary<string, MetaClass> CreateAndGetMembersGroup(ClassMembersGroups classMembersGroupType)
    {
      Dictionary<string, MetaClass> membersGroup;

      if (!membersGroups.ContainsKey(classMembersGroupType))
      {
        membersGroup = new Dictionary<string, MetaClass>();
        membersGroups[classMembersGroupType] = membersGroup;
      }
      else
      {
        membersGroup = membersGroups[classMembersGroupType];
      }

      return membersGroup;
    }

    private Dictionary<string, string> GetGenericNamesMappingsForGroup(ClassMembersGroups classMembersGroupType)
    {
      if (genericNamesMappings == null)
      {
        genericNamesMappings = new Dictionary<ClassMembersGroups, Dictionary<string, string>>();
      }

      Dictionary<string, string> genericNamesMappingsForGroup;

      if (!genericNamesMappings.ContainsKey(classMembersGroupType))
      {
        genericNamesMappingsForGroup = new Dictionary<string, string>();
        genericNamesMappings[classMembersGroupType] = genericNamesMappingsForGroup;
      }
      else
      {
        genericNamesMappingsForGroup = genericNamesMappings[classMembersGroupType];
      }

      return genericNamesMappingsForGroup;
    }

    #endregion

    #region Public methods

    public MetaClass GetMember(ClassMembersGroups classMembersGroupType, string name)
    {
      if (GetMembersCount(classMembersGroupType) == 0)
      {
        return null;
      }

      Dictionary<string, MetaClass> membersGroup = membersGroups[classMembersGroupType];

      if (!membersGroup.ContainsKey(name))
      {
        // try indirect search (for generic types)

        if (genericNamesMappings == null || !genericNamesMappings.ContainsKey(classMembersGroupType))
        {
          return null;
        }

        Dictionary<string, string> genericNamesMappingForGroup = genericNamesMappings[classMembersGroupType];

        if (!genericNamesMappingForGroup.ContainsKey(name))
        {
          return null;
        }

        name = genericNamesMappingForGroup[name];

        if (!membersGroup.ContainsKey(name))
        {
          return null;
        }
      }

      return membersGroup[name];
    }

    public int GetMembersCount(ClassMembersGroups classMembersGroupType)
    {
      if (!membersGroups.ContainsKey(classMembersGroupType))
      {
        return 0;
      }

      return membersGroups[classMembersGroupType].Count;
    }

    public MyNestedTypeInfo GetNestedTypeMember(MyClassInfo physicalClass)
    {
      int indexOfLastPlus = physicalClass.Name.LastIndexOf('/');
      Debug.Assert(indexOfLastPlus != -1 && indexOfLastPlus + 1 < physicalClass.Name.Length, "Impossible! Trying to get nested type for a type which is not nested.");

      if (indexOfLastPlus == -1)
      {
        return null;
      }

      string memberName = physicalClass.Name.Substring(indexOfLastPlus + 1);
      MyNestedTypeInfo nestedType;

      if (physicalClass.IsPublic)
      {
        nestedType = GetMember(ClassMembersGroups.PublicClasses, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.PublicStructures, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.PublicInterfaces, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.PublicDelegates, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.PublicEnumerations, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;
      }
      else if (physicalClass.IsProtectedInternal)
      {
        nestedType = GetMember(ClassMembersGroups.ProtectedInternalClasses, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.ProtectedInternalStructures, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.ProtectedInternalInterfaces, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.ProtectedInternalDelegates, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.ProtectedInternalEnumerations, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;
      }
      else if (physicalClass.IsProtected)
      {
        nestedType = GetMember(ClassMembersGroups.ProtectedClasses, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.ProtectedStructures, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.ProtectedInterfaces, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.ProtectedDelegates, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.ProtectedEnumerations, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;
      }
      else if (physicalClass.IsInternal)
      {
        nestedType = GetMember(ClassMembersGroups.InternalClasses, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.InternalStructures, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.InternalInterfaces, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.InternalDelegates, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.InternalEnumerations, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;
      }
      else if (physicalClass.IsPrivate)
      {
        nestedType = GetMember(ClassMembersGroups.PrivateClasses, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.PrivateStructures, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.PrivateInterfaces, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.PrivateDelegates, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;

        nestedType = GetMember(ClassMembersGroups.PrivateEnumerations, memberName) as MyNestedTypeInfo;
        if (nestedType != null) return nestedType;
      }
      else
      {
        Debug.Assert(false, "Impossible! Unsupported type visibility.");
      }

      return null;
    }

    public List<MetaClass> GetMembers(ClassMembersGroups classMembersGroupType)
    {
      List<MetaClass> result = new List<MetaClass>();

      Dictionary<string, MetaClass> membersGroup = GetMembersGroup(classMembersGroupType);
      if (membersGroup == null)
      {
        return result;
      }

      result.AddRange(membersGroup.Values);

      return result;
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

    #region Public properties

    public string AssemblyName
    {
      get { return assemblyName; }
    }

    public string Namespace
    {
      get { return namezpace; }
    }

    public virtual string AttributesString
    {
      get { return MyClassAttributesToString(attributes); }
    }

    public string BaseTypeName
    {
      get { return baseTypeName; }
    }

    public List<string> ImplementedInterfacesNames
    {
      get { return implementedInterfacesNames; }
    }

    /// <summary>
    /// Only declared parameters.
    /// </summary>
    public int GenericParametersCount
    {
      get { return genericParameters == null ? 0 : genericParameters.Count; }
    }

    /// <summary>
    /// Only declared parameters.
    /// </summary>
    public List<MyGenericParameterInfo> GenericParameters
    {
      get
      {
        Debug.Assert(genericParameters != null, "This typeDefinition is not a generic type!");

        return genericParameters;
      }
    }

    /// <summary>
    /// Declared and "inherited" from containing types.
    /// </summary>
    public int AllGenericParametersNamesCount
    {
      get { return allGenericParametersNames == null ? 0 : allGenericParametersNames.Count; }
    }

    /// <summary>
    /// Declared and "inherited" from containing types.
    /// </summary>
    public List<string> AllGenericParametersNames
    {
      get
      {
        Debug.Assert(allGenericParametersNames != null, "This type is not a generic type!");

        return allGenericParametersNames;
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

    public bool IsNested
    {
      get { return name.Contains("/"); }
    }

    public bool HasMembers
    {
      get
      {
        return membersGroups.Keys.Count > 0;
      }
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

    public string Example
    {
      get { return example; }
      set { example = value; }
    }

    #endregion

    #region ISummarisableMember Members

    public string DisplayableName
    {
      get { return Name.Replace('/', '.'); }
    }

    #endregion

    #region Enumeration

    public IEnumerator<ISummarisableMember> GetEnumerator(ClassMembersGroups classMembersGroupType)
    {
      if (!membersGroups.ContainsKey(classMembersGroupType))
      {
        Debug.Assert(false, "Impossible! Requested for members of an empty group ('" + classMembersGroupType + "').");
        yield break;
      }

      Dictionary<string, MetaClass> membersGroup = membersGroups[classMembersGroupType];
      List<string> sortedKeys = new List<string>();

      sortedKeys.AddRange(membersGroup.Keys);

      sortedKeys.Sort();

      foreach (string key in sortedKeys)
      {
        yield return (ISummarisableMember)membersGroup[key];
      }
    }

    #endregion

    #region MetaClass overrides

    public override string GetMetaName()
    {
      return "Class";
    }

    #endregion
  }
}
