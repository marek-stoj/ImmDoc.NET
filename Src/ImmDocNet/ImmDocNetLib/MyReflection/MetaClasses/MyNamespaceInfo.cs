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
using System.IO;
using System.Diagnostics;

using Imm.ImmDocNetLib.Documenters;
using Mono.Cecil;

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
  class MyNamespaceInfo : MetaClass, ISummarisableMember
  {
    public const string GLOBAL_NAMESPACE_NAME = "[GLOBAL]";

    private string assemblyName;
    private Dictionary<NamespaceMembersGroups, Dictionary<string, MetaClass>> membersGroups;
    private Dictionary<NamespaceMembersGroups, Dictionary<string, string>> genericNamesMappings;

    #region Constructor(s)

    public MyNamespaceInfo(string name, string assemblyName)
    {
      this.name = name;
      this.assemblyName = assemblyName;
      this.membersGroups = new Dictionary<NamespaceMembersGroups, Dictionary<string, MetaClass>>();
    }

    #endregion

    #region Public methods

    public void AddType(TypeDefinition typeDefinition)
    {
      if (typeDefinition.IsValueType && !typeDefinition.IsEnum)
      {
        MyStructureInfo myStructureInfo = new MyStructureInfo(typeDefinition, assemblyName);
        Dictionary<string, MetaClass> membersGroup;
        NamespaceMembersGroups namespaceMembersGroupType;

        if (myStructureInfo.IsPublic)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.PublicStructures;
        }
        else if (myStructureInfo.IsProtectedInternal)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.ProtectedInternalStructures;
        }
        else if (myStructureInfo.IsProtected)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.ProtectedStructures;
        }
        else if (myStructureInfo.IsInternal)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.InternalStructures;
        }
        else if (myStructureInfo.IsPrivate)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.PrivateStructures;
        }
        else
        {
          Debug.Assert(false, "Impossible! Visibility of a type is not supported.");
          return;
        }

        membersGroup = GetMembersGroup(namespaceMembersGroupType);

        if (!membersGroup.ContainsKey(myStructureInfo.Name))
        {
          membersGroup.Add(myStructureInfo.Name, myStructureInfo);

          AddGenericNameMappingIfNeeded(myStructureInfo, namespaceMembersGroupType);
        }
        else
        {
          Logger.Warning("Structure named '{0}' has already been added to namespace {1}.", myStructureInfo.Name, name);
        }
      }
      else if (typeDefinition.IsInterface)
      {
        MyInterfaceInfo myInterfaceInfo = new MyInterfaceInfo(typeDefinition, assemblyName);
        Dictionary<string, MetaClass> membersGroup;
        NamespaceMembersGroups namespaceMembersGroupType;

        if (myInterfaceInfo.IsPublic)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.PublicInterfaces;
        }
        else if (myInterfaceInfo.IsProtectedInternal)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.ProtectedInternalInterfaces;
        }
        else if (myInterfaceInfo.IsProtected)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.ProtectedInterfaces;
        }
        else if (myInterfaceInfo.IsInternal)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.InternalInterfaces;
        }
        else if (myInterfaceInfo.IsPrivate)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.PrivateInterfaces;
        }
        else
        {
          Debug.Assert(false, "Impossible! Visibility of a type is not supported.");
          return;
        }

        membersGroup = GetMembersGroup(namespaceMembersGroupType);

        if (!membersGroup.ContainsKey(myInterfaceInfo.Name))
        {
          membersGroup.Add(myInterfaceInfo.Name, myInterfaceInfo);

          AddGenericNameMappingIfNeeded(myInterfaceInfo, namespaceMembersGroupType);
        }
        else
        {
          Logger.Warning("Interface named '{0}' has already been added to namespace {1}.", myInterfaceInfo.Name, name);
        }
      }
      else if (typeDefinition.IsEnum)
      {
        MyEnumerationInfo myEnumerationInfo = new MyEnumerationInfo(typeDefinition, assemblyName);
        Dictionary<string, MetaClass> membersGroup;
        NamespaceMembersGroups namespaceMembersGroupType;

        if (myEnumerationInfo.IsPublic)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.PublicEnumerations;
        }
        else if (myEnumerationInfo.IsProtectedInternal)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.ProtectedInternalEnumerations;
        }
        else if (myEnumerationInfo.IsProtected)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.ProtectedEnumerations;
        }
        else if (myEnumerationInfo.IsInternal)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.InternalEnumerations;
        }
        else if (myEnumerationInfo.IsPrivate)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.PrivateEnumerations;
        }
        else
        {
          Debug.Assert(false, "Impossible! Visibility of a type is not supported.");
          return;
        }

        membersGroup = GetMembersGroup(namespaceMembersGroupType);

        if (!membersGroup.ContainsKey(myEnumerationInfo.Name))
        {
          membersGroup.Add(myEnumerationInfo.Name, myEnumerationInfo);

          AddGenericNameMappingIfNeeded(myEnumerationInfo, namespaceMembersGroupType);
        }
        else
        {
          Logger.Warning("Enumeration named '{0}' has already been added to namespace {1}.", myEnumerationInfo.Name, name);
        }
      }
      else if (typeDefinition.IsClass && !Utils.IsDelegate(typeDefinition))
      {
        MyClassInfo myClassInfo = new MyClassInfo(typeDefinition, assemblyName);
        Dictionary<string, MetaClass> membersGroup;
        NamespaceMembersGroups namespaceMembersGroupType;

        if (myClassInfo.IsPublic)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.PublicClasses;
        }
        else if (myClassInfo.IsProtectedInternal)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.ProtectedInternalClasses;
        }
        else if (myClassInfo.IsProtected)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.ProtectedClasses;
        }
        else if (myClassInfo.IsInternal)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.InternalClasses;
        }
        else if (myClassInfo.IsPrivate)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.PrivateClasses;
        }
        else
        {
          Debug.Assert(false, "Impossible! Visibility of a type is not supported.");
          return;
        }

        membersGroup = GetMembersGroup(namespaceMembersGroupType);

        if (!membersGroup.ContainsKey(myClassInfo.Name))
        {
          membersGroup.Add(myClassInfo.Name, myClassInfo);

          AddGenericNameMappingIfNeeded(myClassInfo, namespaceMembersGroupType);
        }
        else
        {
          Logger.Warning("Class named '{0}' has already been added to namespace {1}.", myClassInfo.Name, name);
        }
      }
      else if (Utils.IsDelegate(typeDefinition))
      {
        MyDelegateInfo myDelegateInfo = new MyDelegateInfo(typeDefinition, assemblyName);
        Dictionary<string, MetaClass> membersGroup;
        NamespaceMembersGroups namespaceMembersGroupType;

        if (myDelegateInfo.IsPublic)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.PublicDelegates;
        }
        else if (myDelegateInfo.IsProtectedInternal)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.ProtectedInternalDelegates;
        }
        else if (myDelegateInfo.IsProtected)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.ProtectedDelegates;
        }
        else if (myDelegateInfo.IsInternal)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.InternalDelegates;
        }
        else if (myDelegateInfo.IsPrivate)
        {
          namespaceMembersGroupType = NamespaceMembersGroups.PrivateDelegates;
        }
        else
        {
          Debug.Assert(false, "Impossible! Visibility of a type is not supported.");
          return;
        }

        membersGroup = GetMembersGroup(namespaceMembersGroupType);

        if (!membersGroup.ContainsKey(myDelegateInfo.Name))
        {
          membersGroup.Add(myDelegateInfo.Name, myDelegateInfo);

          AddGenericNameMappingIfNeeded(myDelegateInfo, namespaceMembersGroupType);
        }
        else
        {
          Logger.Warning("Delegate named '{0}' has already been added to namespace {1}.", myDelegateInfo.Name, name);
        }
      }
      else
      {
        Logger.Warning("Unrecognized type: {0}.", typeDefinition.FullName);
      }
    }

    public MyClassInfo FindMember(NamespaceMembersGroups namespaceMembersGroupType, string memberName)
    {
      if (!membersGroups.ContainsKey(namespaceMembersGroupType))
      {
        return null;
      }

      Dictionary<string, MetaClass> membersGroup = membersGroups[namespaceMembersGroupType];
      MetaClass member = null;

      if (!membersGroup.ContainsKey(memberName))
      {
        // try indirect search (for generic types)
        if (genericNamesMappings == null || !genericNamesMappings.ContainsKey(namespaceMembersGroupType))
        {
          return null;
        }

        Dictionary<string, string> genericNamesMappingsForGroup = genericNamesMappings[namespaceMembersGroupType];
        if (!genericNamesMappingsForGroup.ContainsKey(memberName))
        {
          return null;
        }

        memberName = genericNamesMappingsForGroup[memberName];

        if (!membersGroup.ContainsKey(memberName))
        {
          return null;
        }
      }

      member = membersGroup[memberName];
      Debug.Assert(member is MyClassInfo, "Impossible! All namespace member inherit from MyClassInfo.");

      return (MyClassInfo)member;
    }

    public int GetMembersCount(NamespaceMembersGroups namespaceMembersGroupType)
    {
      if (!membersGroups.ContainsKey(namespaceMembersGroupType))
      {
        return 0;
      }

      return membersGroups[namespaceMembersGroupType].Count;
    }

    public Dictionary<string, MetaClass> GetMembers(NamespaceMembersGroups namespaceMembersGroupType)
    {
      if (!membersGroups.ContainsKey(namespaceMembersGroupType))
      {
        return null;
      }

      return membersGroups[namespaceMembersGroupType];
    }

    public static string NamespaceMembersGroupToString(NamespaceMembersGroups membersGroupType)
    {
      switch (membersGroupType)
      {
        case NamespaceMembersGroups.PublicClasses: { return "Public Classes"; }
        case NamespaceMembersGroups.PublicStructures: { return "Public Structures"; }
        case NamespaceMembersGroups.PublicInterfaces: { return "Public Interfaces"; }
        case NamespaceMembersGroups.PublicDelegates: { return "Public Delegates"; }
        case NamespaceMembersGroups.PublicEnumerations: { return "Public Enumerations"; }
        case NamespaceMembersGroups.ProtectedInternalClasses: { return "Protected Internal Classes"; }
        case NamespaceMembersGroups.ProtectedInternalStructures: { return "Protected Internal Structures"; }
        case NamespaceMembersGroups.ProtectedInternalInterfaces: { return "Protected Internal Interfaces"; }
        case NamespaceMembersGroups.ProtectedInternalDelegates: { return "Protected Internal Delegates"; }
        case NamespaceMembersGroups.ProtectedInternalEnumerations: { return "Protected Internal Enumerations"; }
        case NamespaceMembersGroups.ProtectedClasses: { return "Protected Classes"; }
        case NamespaceMembersGroups.ProtectedStructures: { return "Protected Structures"; }
        case NamespaceMembersGroups.ProtectedInterfaces: { return "Protected Interfaces"; }
        case NamespaceMembersGroups.ProtectedDelegates: { return "Protected Delegates"; }
        case NamespaceMembersGroups.ProtectedEnumerations: { return "Protected Enumerations"; }
        case NamespaceMembersGroups.InternalClasses: { return "Internal Classes"; }
        case NamespaceMembersGroups.InternalStructures: { return "Internal Structures"; }
        case NamespaceMembersGroups.InternalInterfaces: { return "Internal Interfaces"; }
        case NamespaceMembersGroups.InternalDelegates: { return "Internal Delegates"; }
        case NamespaceMembersGroups.InternalEnumerations: { return "Internal Enumerations"; }
        case NamespaceMembersGroups.PrivateClasses: { return "Private Classes"; }
        case NamespaceMembersGroups.PrivateStructures: { return "Private Structures"; }
        case NamespaceMembersGroups.PrivateInterfaces: { return "Private Interfaces"; }
        case NamespaceMembersGroups.PrivateDelegates: { return "Private Delegates"; }
        case NamespaceMembersGroups.PrivateEnumerations: { return "Private Enumerations"; }

        default:
          {
            Debug.Assert(false, "Impossible! Couldn't recognize type of a namespace member.");

            return null;
          }
      }
    }

    public static string GetBaseGroupName(NamespaceMembersGroups membersGroupType)
    {
      switch (membersGroupType)
      {
        case NamespaceMembersGroups.PublicClasses: { return "Classes"; }
        case NamespaceMembersGroups.PublicStructures: { return "Structures"; }
        case NamespaceMembersGroups.PublicInterfaces: { return "Interfaces"; }
        case NamespaceMembersGroups.PublicDelegates: { return "Delegates"; }
        case NamespaceMembersGroups.PublicEnumerations: { return "Enumerations"; }
        case NamespaceMembersGroups.ProtectedInternalClasses: { return "Classes"; }
        case NamespaceMembersGroups.ProtectedInternalStructures: { return "Structures"; }
        case NamespaceMembersGroups.ProtectedInternalInterfaces: { return "Interfaces"; }
        case NamespaceMembersGroups.ProtectedInternalDelegates: { return "Delegates"; }
        case NamespaceMembersGroups.ProtectedInternalEnumerations: { return "Enumerations"; }
        case NamespaceMembersGroups.ProtectedClasses: { return "Classes"; }
        case NamespaceMembersGroups.ProtectedStructures: { return "Structures"; }
        case NamespaceMembersGroups.ProtectedInterfaces: { return "Interfaces"; }
        case NamespaceMembersGroups.ProtectedDelegates: { return "Delegates"; }
        case NamespaceMembersGroups.ProtectedEnumerations: { return "Enumerations"; }
        case NamespaceMembersGroups.InternalClasses: { return "Classes"; }
        case NamespaceMembersGroups.InternalStructures: { return "Structures"; }
        case NamespaceMembersGroups.InternalInterfaces: { return "Interfaces"; }
        case NamespaceMembersGroups.InternalDelegates: { return "Delegates"; }
        case NamespaceMembersGroups.InternalEnumerations: { return "Enumerations"; }
        case NamespaceMembersGroups.PrivateClasses: { return "Classes"; }
        case NamespaceMembersGroups.PrivateStructures: { return "Structures"; }
        case NamespaceMembersGroups.PrivateInterfaces: { return "Interfaces"; }
        case NamespaceMembersGroups.PrivateDelegates: { return "Delegates"; }
        case NamespaceMembersGroups.PrivateEnumerations: { return "Enumerations"; }

        default:
          {
            Debug.Assert(false, "Impossible! Couldn't recognize type of a namespace member.");

            return null;
          }
      }
    }

    public static bool IsMembersGroupTypePublic(NamespaceMembersGroups membersGroupType)
    {
      switch (membersGroupType)
      {
        case NamespaceMembersGroups.PublicClasses: { return true; }
        case NamespaceMembersGroups.PublicStructures: { return true; }
        case NamespaceMembersGroups.PublicInterfaces: { return true; }
        case NamespaceMembersGroups.PublicDelegates: { return true; }
        case NamespaceMembersGroups.PublicEnumerations: { return true; }

        default: { return false; }
      }
    }

    public bool HasProtectedGroupOfTheSameType(NamespaceMembersGroups membersGroupType)
    {
      switch (membersGroupType)
      {
        case NamespaceMembersGroups.PublicClasses: { return GetMembersCount(NamespaceMembersGroups.ProtectedClasses) > 0; }
        case NamespaceMembersGroups.PublicStructures: { return GetMembersCount(NamespaceMembersGroups.ProtectedStructures) > 0; }
        case NamespaceMembersGroups.PublicInterfaces: { return GetMembersCount(NamespaceMembersGroups.ProtectedInterfaces) > 0; }
        case NamespaceMembersGroups.PublicDelegates: { return GetMembersCount(NamespaceMembersGroups.ProtectedDelegates) > 0; }
        case NamespaceMembersGroups.PublicEnumerations: { return GetMembersCount(NamespaceMembersGroups.ProtectedEnumerations) > 0; }

        default: return false;
      }
    }

    public bool HasProtectedInternalGroupOfTheSameType(NamespaceMembersGroups membersGroupType)
    {
      switch (membersGroupType)
      {
        case NamespaceMembersGroups.PublicClasses: { return GetMembersCount(NamespaceMembersGroups.ProtectedInternalClasses) > 0; }
        case NamespaceMembersGroups.PublicStructures: { return GetMembersCount(NamespaceMembersGroups.ProtectedInternalStructures) > 0; }
        case NamespaceMembersGroups.PublicInterfaces: { return GetMembersCount(NamespaceMembersGroups.ProtectedInternalInterfaces) > 0; }
        case NamespaceMembersGroups.PublicDelegates: { return GetMembersCount(NamespaceMembersGroups.ProtectedInternalDelegates) > 0; }
        case NamespaceMembersGroups.PublicEnumerations: { return GetMembersCount(NamespaceMembersGroups.ProtectedInternalEnumerations) > 0; }

        default: return false;
      }
    }

    public bool HasInternalGroupOfTheSameType(NamespaceMembersGroups membersGroupType)
    {
      switch (membersGroupType)
      {
        case NamespaceMembersGroups.PublicClasses: { return GetMembersCount(NamespaceMembersGroups.InternalClasses) > 0; }
        case NamespaceMembersGroups.PublicStructures: { return GetMembersCount(NamespaceMembersGroups.InternalStructures) > 0; }
        case NamespaceMembersGroups.PublicInterfaces: { return GetMembersCount(NamespaceMembersGroups.InternalInterfaces) > 0; }
        case NamespaceMembersGroups.PublicDelegates: { return GetMembersCount(NamespaceMembersGroups.InternalDelegates) > 0; }
        case NamespaceMembersGroups.PublicEnumerations: { return GetMembersCount(NamespaceMembersGroups.InternalEnumerations) > 0; }

        default: return false;
      }
    }

    public bool HasPrivateGroupOfTheSameType(NamespaceMembersGroups membersGroupType)
    {
      switch (membersGroupType)
      {
        case NamespaceMembersGroups.PublicClasses: { return GetMembersCount(NamespaceMembersGroups.PrivateClasses) > 0; }
        case NamespaceMembersGroups.PublicStructures: { return GetMembersCount(NamespaceMembersGroups.PrivateStructures) > 0; }
        case NamespaceMembersGroups.PublicInterfaces: { return GetMembersCount(NamespaceMembersGroups.PrivateInterfaces) > 0; }
        case NamespaceMembersGroups.PublicDelegates: { return GetMembersCount(NamespaceMembersGroups.PrivateDelegates) > 0; }
        case NamespaceMembersGroups.PublicEnumerations: { return GetMembersCount(NamespaceMembersGroups.PrivateEnumerations) > 0; }

        default: return false;
      }
    }

    #endregion

    #region Public properties

    public string AssemblyName
    {
      get { return assemblyName; }
    }

    public bool HasMembers
    {
      get
      {
        return membersGroups.Keys.Count > 0;
      }
    }

    #endregion

    #region Private helper methods

    private Dictionary<string, MetaClass> GetMembersGroup(NamespaceMembersGroups namespaceMembersGroupType)
    {
      Dictionary<string, MetaClass> membersGroup;

      if (!membersGroups.ContainsKey(namespaceMembersGroupType))
      {
        membersGroup = new Dictionary<string, MetaClass>();
        membersGroups[namespaceMembersGroupType] = membersGroup;
      }
      else
      {
        membersGroup = membersGroups[namespaceMembersGroupType];
      }

      return membersGroup;
    }

    private Dictionary<string, string> GetGenericNamesMappingsForGroup(NamespaceMembersGroups namespaceMembersGroupType)
    {
      if (genericNamesMappings == null)
      {
        genericNamesMappings = new Dictionary<NamespaceMembersGroups, Dictionary<string, string>>();
      }

      Dictionary<string, string> genericNamesMappingsForGroup;

      if (!genericNamesMappings.ContainsKey(namespaceMembersGroupType))
      {
        genericNamesMappingsForGroup = new Dictionary<string, string>();
        genericNamesMappings[namespaceMembersGroupType] = genericNamesMappingsForGroup;
      }
      else
      {
        genericNamesMappingsForGroup = genericNamesMappings[namespaceMembersGroupType];
      }

      return genericNamesMappingsForGroup;
    }

    private void AddGenericNameMappingIfNeeded(MyClassInfo myClassInfo, NamespaceMembersGroups namespaceMembersGroupType)
    {
      if (!myClassInfo.Name.Contains("<"))
      {
        return;
      }

      Dictionary<string, string> genericNamesMappingsForGroup = GetGenericNamesMappingsForGroup(namespaceMembersGroupType);
      string xmlName = Utils.ConvertNameToXmlDocForm(myClassInfo.Name);

      genericNamesMappingsForGroup[xmlName] = myClassInfo.Name;
    }

    #endregion

    #region ISummarisableMember Members

    public string DisplayableName
    {
      get { return Name; }
    }

    #endregion

    #region MetaClass overrides

    public override string GetMetaName()
    {
      return "Namespace";
    }

    #endregion

    #region Enumeration

    internal IEnumerator<ISummarisableMember> GetEnumerator(NamespaceMembersGroups namespaceMembersGroupType)
    {
      if (!membersGroups.ContainsKey(namespaceMembersGroupType))
      {
        Debug.Assert(false, "Impossible! Couldn't recognize members group ('" + namespaceMembersGroupType + "').");
        yield break;
      }

      Dictionary<string, MetaClass> membersGroup = membersGroups[namespaceMembersGroupType];
      List<string> sortedKeys = new List<string>();

      sortedKeys.AddRange(membersGroup.Keys);

      sortedKeys.Sort();

      foreach (string key in sortedKeys)
      {
        yield return (ISummarisableMember)membersGroup[key];
      }
    }

    public IEnumerable<MetaClass> GetEnumerator()
    {
      List<MetaClass> sortedMembers = new List<MetaClass>();
      int namespaceMembersGroupIndex = 0;

      while (Enum.IsDefined(typeof(NamespaceMembersGroups), namespaceMembersGroupIndex))
      {
        NamespaceMembersGroups namespaceMembersGroup = (NamespaceMembersGroups)namespaceMembersGroupIndex;

        if (GetMembersCount(namespaceMembersGroup) > 0)
        {
          Dictionary<string, MetaClass> membersGroup = GetMembers(namespaceMembersGroup);

          foreach (MetaClass member in membersGroup.Values)
          {
            sortedMembers.Add(member);
          }
        }

        namespaceMembersGroupIndex++;
      }

      sortedMembers.Sort(new Comparison<MetaClass>(MembersComparison));

      return sortedMembers;
    }

    private int MembersComparison(MetaClass m1, MetaClass m2)
    {
      return m1.Name.CompareTo(m2.Name);
    }

    #endregion
  }
}
