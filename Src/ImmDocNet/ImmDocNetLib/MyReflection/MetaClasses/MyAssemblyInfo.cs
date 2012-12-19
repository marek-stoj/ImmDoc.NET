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
using System.Linq;
using Mono.Cecil;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

using Imm.ImmDocNetLib.Documenters;

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
  class MyAssemblyInfo : MetaClass, ISummarisableMember
  {
    private static readonly char[] commaArray = new char[] { ',' };

    private Dictionary<string, MyNamespaceInfo> namespaces;
    private readonly AssembliesInfo assembliesInfo;

    #region Constructor(s)

    public MyAssemblyInfo(string name, AssembliesInfo assembliesInfo)
    {
      this.name = name;
      this.assembliesInfo = assembliesInfo;
    }

    #endregion

    #region Public methods

    public void ReadAssembly(string assemblyAbsolutePath, IEnumerable<string> excludedNamespaces)
    {
      AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyAbsolutePath);
      string assemblyName = assembly.Name.Name;

      if (assemblyName != name)
      {
        Logger.Error("Can't load contents of an assembly to another assembly which has different name.");
        return;
      }

      List<TypeDefinition> types = Utils.GetAllTypes(assembly);

      foreach (TypeDefinition typeDefinition in types)
      {
        if (!Utils.ShouldIncludeType(typeDefinition))
        {
          continue;
        }

        if (string.IsNullOrEmpty(typeDefinition.Namespace) && typeDefinition.FullName == MyClassInfo.SPECIAL_MODULE_CLASS_NAME)
        {
          // skip the special <Module> class

          continue;
        }

        MyNamespaceInfo namespaceInfo;
        string typeNamespace = Utils.GetTypeNamespace(typeDefinition);

        if (excludedNamespaces.Contains(typeNamespace))
        {
          continue;
        }

        if (namespaces == null)
        {
          namespaces = new Dictionary<string, MyNamespaceInfo>();
        }

        if (!namespaces.ContainsKey(typeNamespace))
        {
          namespaceInfo = new MyNamespaceInfo(typeNamespace, assemblyName);
          namespaces.Add(typeNamespace, namespaceInfo);
        }
        else
        {
          namespaceInfo = namespaces[typeNamespace];
        }

        namespaceInfo.AddType(typeDefinition);
      }
    }

    public void ReadXmlDocumentation(string xmlDocPath)
    {
      string xmlDocAbsolutePath = Path.GetFullPath(xmlDocPath);
      string assemblyName = Utils.GetAssemblyNameFromXmlDocumentation(xmlDocAbsolutePath);

      if (assemblyName == null)
      {
        Logger.Warning("Wrong format of XML documentation file: " + xmlDocPath);

        return;
      }

      Debug.Assert(assemblyName == name, "Can't load contents of an assembly to another assembly wich has different name.");

      try
      {
        XPathDocument xPathDocument = new XPathDocument(xmlDocAbsolutePath);
        XPathNavigator docNavigator = xPathDocument.CreateNavigator();

        XPathNodeIterator memberNodes = docNavigator.Select("/doc/members/member");
        while (memberNodes.MoveNext())
        {
          ProcessMemberNode(memberNodes.Current, docNavigator);
        }
      }
      catch (XmlException)
      {
        Logger.Warning("Wrong format of XML documentation file: " + xmlDocPath);
      }
    }

    public MyNamespaceInfo FindNamespace(string namezpace)
    {
      if (NamespacesCount == 0 || !namespaces.ContainsKey(namezpace))
      {
        return null;
      }

      return namespaces[namezpace];
    }

    #endregion

    #region Private XML documentation processing helper methods

    private void ProcessMemberNode(XPathNavigator memberNodeNavigator, XPathNavigator docNavigator)
    {
      string nameAttribValue = memberNodeNavigator.GetAttribute("name", "");
      string xmlMemberId = nameAttribValue.Substring(2);
      char memberType = nameAttribValue[0];

      switch (memberType)
      {
        case 'T':
          {
            MyClassInfo myClassInfo = FindNamespaceMember(xmlMemberId);
            if (myClassInfo == null)
            {
              Logger.Warning("(XML) Couldn't locate namespace member '{0}'.", xmlMemberId);

              break;
            }

            myClassInfo.Summary = ReadXmlNodes(memberNodeNavigator, "summary");
            myClassInfo.Remarks = ReadXmlNodes(memberNodeNavigator, "remarks");

            if (myClassInfo.IsNested)
            {
              // the class/struct/etc. is a nested typeDefinition
              int indexOfLastDot = xmlMemberId.LastIndexOf('.');
              Debug.Assert(indexOfLastDot != -1, "There must be dot because this type is nested.");

              string declaringTypeFullName = xmlMemberId.Substring(0, indexOfLastDot);
              MyClassInfo declaringType = FindNamespaceMember(declaringTypeFullName);
              if (declaringType == null)
              {
                  // Declaring type of a nested type is private
                  break;
              }

              MyNestedTypeInfo myNestedTypeInfo = declaringType.GetNestedTypeMember(myClassInfo);
              Debug.Assert(myNestedTypeInfo != null, "Impossible! This nested type must be there.");

              myNestedTypeInfo.Summary = myClassInfo.Summary;
            }

            if (myClassInfo is MyDelegateInfo)
            {
              MyDelegateInfo myDelegateInfo = (MyDelegateInfo)myClassInfo;

              // returns node
              string returnValueSummary = ReadXmlNodes(memberNodeNavigator, "returns");

              if (myDelegateInfo.ReturnTypeFullName.ToLower() != "system.void")
              {
                myDelegateInfo.ReturnValueSummary = returnValueSummary;
              }
              else if (returnValueSummary != "")
              {
                Logger.Warning("(XML) There's a description of return value for delegate '{0}' but this delegate returns nothing.", xmlMemberId);
              }

              // param nodes
              XPathNodeIterator paramNodes = memberNodeNavigator.Select("param");
              while (paramNodes.MoveNext())
              {
                string paramName = paramNodes.Current.GetAttribute("name", "");
                if (paramName == null)
                {
                  Logger.Warning("(XML) Node 'param' doesn't have required attribute 'name'.");
                  continue;
                }

                MyParameterInfo myParameterInfo = FindParameter(myDelegateInfo, paramName);
                if (myParameterInfo == null)
                {
                  Logger.Warning("(XML) Node 'param' with name '{0}' referenced in XML documentation couldn't be found in method/constructor '{1}'.", paramName, xmlMemberId);
                  continue;
                }

                myParameterInfo.Summary = paramNodes.Current.InnerXml;
              }
            }

            // typeparam nodes
            XPathNodeIterator typeParamNodes = memberNodeNavigator.Select("typeparam");
            while (typeParamNodes.MoveNext())
            {
              string typeParamName = typeParamNodes.Current.GetAttribute("name", "");
              if (typeParamName == null)
              {
                Logger.Warning("(XML) Node 'typeparam' doesn't have required attribute 'name'.");
                continue;
              }

              MyGenericParameterInfo myGenericParameterInfo = FindGenericParameter(myClassInfo, typeParamName);
              if (myGenericParameterInfo == null)
              {
                Logger.Warning("(XML) Node 'typeparam' with name '{0}' referenced in XML documentation couldn't be found in type '{1}'.", typeParamName, xmlMemberId);
                continue;
              }

              myGenericParameterInfo.Summary = typeParamNodes.Current.InnerXml;
            }

            myClassInfo.Example = ReadXmlNodes(memberNodeNavigator, "example");

            break;
          }

        case 'M':
          {
            MyInvokableMemberInfo myInvokableMemberInfo = FindMethodOrConstructor(xmlMemberId);
            if (myInvokableMemberInfo == null)
            {
              Logger.Warning("(XML) Couldn't locate invokable member '{0}'.", xmlMemberId);

              break;
            }

            myInvokableMemberInfo.Summary = ReadXmlNodes(memberNodeNavigator, "summary");
            myInvokableMemberInfo.Remarks = ReadXmlNodes(memberNodeNavigator, "remarks");

            if (myInvokableMemberInfo is MyMethodInfo)
            {
              MyMethodInfo myMethodInfo = (MyMethodInfo)myInvokableMemberInfo;

              // returns node
              string returnValueSummary = ReadXmlNodes(memberNodeNavigator, "returns");

              if (myMethodInfo.ReturnTypeFullName.ToLower() != "system.void")
              {
                myMethodInfo.ReturnValueSummary = returnValueSummary;
              }
              else if (returnValueSummary != "")
              {
                Logger.Warning("(XML) There's a description of return value for method '{0}' but this method/constructor returns nothing.", xmlMemberId);
              }

              // typeparam nodes
              XPathNodeIterator typeParamNodes = memberNodeNavigator.Select("typeparam");
              while (typeParamNodes.MoveNext())
              {
                string typeParamName = typeParamNodes.Current.GetAttribute("name", "");
                if (typeParamName == null)
                {
                  Logger.Warning("(XML) Node 'typeparam' doesn't have required attribute 'name'.");
                  continue;
                }

                MyGenericParameterInfo myGenericParameterInfo = FindGenericParameter(myMethodInfo, typeParamName);
                if (myGenericParameterInfo == null)
                {
                  Logger.Warning("(XML) Node 'typeparam' with name '{0}' referenced in XML documentation couldn't be found in type '{1}'.", typeParamName, xmlMemberId);
                  continue;
                }

                myGenericParameterInfo.Summary = typeParamNodes.Current.InnerXml;
              }
            }

            // param nodes
            XPathNodeIterator paramNodes = memberNodeNavigator.Select("param");
            while (paramNodes.MoveNext())
            {
              string paramName = paramNodes.Current.GetAttribute("name", "");
              if (paramName == null)
              {
                Logger.Warning("(XML) Node 'param' doesn't have required attribute 'name'.");
                continue;
              }

              MyParameterInfo myParameterInfo = FindParameter(myInvokableMemberInfo, paramName);
              if (myParameterInfo == null)
              {
                Logger.Warning("(XML) Node 'param' with name '{0}' referenced in XML documentation couldn't be found in method/constructor '{1}'.", paramName, xmlMemberId);
                continue;
              }

              myParameterInfo.Summary = paramNodes.Current.InnerXml;
            }

            // exception nodes
            XPathNodeIterator exceptionNodes = memberNodeNavigator.Select("exception");
            while (exceptionNodes.MoveNext())
            {
              string exceptionCref = exceptionNodes.Current.GetAttribute("cref", "");
              if (exceptionCref == null)
              {
                Logger.Warning("(XML) Node 'exception' doesn't have required attribute 'cref'.");
                continue;
              }

              int indexOfColon = exceptionCref.IndexOf(':');
              if (indexOfColon != -1 && indexOfColon + 1 < exceptionCref.Length)
              {
                exceptionCref = exceptionCref.Substring(indexOfColon + 1);
              }

              MyClassInfo exceptionClassInfo = FindGlobalNamespaceMember(exceptionCref);
              if (exceptionClassInfo != null)
              {
                myInvokableMemberInfo.ExceptionsDescrs.Add(new ExceptionDescr(exceptionClassInfo, exceptionNodes.Current.InnerXml));
              }
              else
              {
                myInvokableMemberInfo.ExceptionsDescrs.Add(new ExceptionDescr(exceptionCref, exceptionNodes.Current.InnerXml));
              }
            }

            // example nodes
            myInvokableMemberInfo.Example = ReadXmlNodes(memberNodeNavigator, "example");

            break;
          }

        case 'F':
          {
            MyFieldInfo myFieldInfo = FindField(xmlMemberId);
            if (myFieldInfo == null)
            {
              Logger.Warning("(XML) Couldn't locate field '{0}'.", xmlMemberId);

              break;
            }

            myFieldInfo.Summary = ReadXmlNodes(memberNodeNavigator, "summary");
            myFieldInfo.Remarks = ReadXmlNodes(memberNodeNavigator, "remarks");
            myFieldInfo.Example = ReadXmlNodes(memberNodeNavigator, "example");

            break;
          }

        case 'P':
          {
            MyPropertyInfo myPropertyInfo = FindProperty(xmlMemberId);
            if (myPropertyInfo == null)
            {
              Logger.Warning("(XML) Couldn't locate property '{0}'.", xmlMemberId);

              break;
            }

            myPropertyInfo.Summary = ReadXmlNodes(memberNodeNavigator, "summary");
            myPropertyInfo.Remarks = ReadXmlNodes(memberNodeNavigator, "remarks");

            string returnValueSummary = ReadXmlNodes(memberNodeNavigator, "value");
            myPropertyInfo.ReturnValueSummary = returnValueSummary;

            // param nodes
            XPathNodeIterator paramNodes = memberNodeNavigator.Select("param");
            while (paramNodes.MoveNext())
            {
              string paramName = paramNodes.Current.GetAttribute("name", "");
              if (paramName == null)
              {
                Logger.Warning("(XML) Node 'param' doesn't have required attribute 'name'.");
                continue;
              }

              MyParameterInfo myParameterInfo = FindParameter(myPropertyInfo, paramName);
              if (myParameterInfo == null)
              {
                Logger.Warning("(XML) Node 'param' with name '{0}' referenced in XML documentation couldn't be found in method/constructor '{1}'.", paramName, xmlMemberId);
                continue;
              }

              myParameterInfo.Summary = paramNodes.Current.InnerXml;
            }

            // exception nodes
            XPathNodeIterator exceptionNodes = memberNodeNavigator.Select("exception");
            while (exceptionNodes.MoveNext())
            {
              string exceptionCref = exceptionNodes.Current.GetAttribute("cref", "");
              if (exceptionCref == null)
              {
                Logger.Warning("(XML) Node 'exception' doesn't have required attribute 'cref'.");
                continue;
              }

              int indexOfColon = exceptionCref.IndexOf(':');
              if (indexOfColon != -1 && indexOfColon + 1 < exceptionCref.Length)
              {
                exceptionCref = exceptionCref.Substring(indexOfColon + 1);
              }

              MyClassInfo exceptionClassInfo = FindGlobalNamespaceMember(exceptionCref);
              if (exceptionClassInfo != null)
              {
                myPropertyInfo.ExceptionsDescrs.Add(new ExceptionDescr(exceptionClassInfo, exceptionNodes.Current.InnerXml));
              }
              else
              {
                myPropertyInfo.ExceptionsDescrs.Add(new ExceptionDescr(exceptionCref, exceptionNodes.Current.InnerXml));
              }
            }

            myPropertyInfo.Example = ReadXmlNodes(memberNodeNavigator, "example");

            break;
          }

        case 'E':
          {
            MyEventInfo myEventInfo = FindEvent(xmlMemberId);
            if (myEventInfo == null)
            {
              Logger.Warning("(XML) Couldn't locate event '{0}'.", xmlMemberId);

              break;
            }

            myEventInfo.Summary = ReadXmlNodes(memberNodeNavigator, "summary");
            myEventInfo.Remarks = ReadXmlNodes(memberNodeNavigator, "remarks");
            myEventInfo.Example = ReadXmlNodes(memberNodeNavigator, "example");

            break;
          }

        case '!':
          {
            // shouldn't normally happen; exclamation mark is not used in members
            // but for example in crefs

            Logger.Warning("(XML) There is a member which couldn't be resolved by Visual Studio: '{0}'.", xmlMemberId);

            break;
          }

        case 'N':
          {
            // shouldn't normally happen; namespaces can't normally have comments
            // but for example documentation files for .NET Framework contain such members (sic!)

            MyNamespaceInfo myNamespaceInfo = FindNamespace(xmlMemberId);
            if (myNamespaceInfo == null)
            {
              Logger.Warning("(XML) Couldn't locate namespace '{0}'.", xmlMemberId);

              break;
            }

            myNamespaceInfo.Summary = ReadXmlNodes(memberNodeNavigator, "summary");

            break;
          }

        default:
          {
            Logger.Warning("Couldn't recognize type of a member ('{0}').", memberType);

            break;
          }
      }
    }

    public MyFieldInfo FindField(string xmlMemberId)
    {
      return FindField(xmlMemberId, false);
    }

    public MyFieldInfo FindField(string xmlMemberId, bool global)
    {
      string memberName;
      string paramsStr;
      string returnTypeFullName;

      MyClassInfo memberDeclaringType = ExtractTypeMemberInfo(xmlMemberId, global, out memberName, out paramsStr, out returnTypeFullName);
      if (memberDeclaringType == null)
      {
        return null;
      }

      MyFieldInfo myFieldInfo = null;

      foreach (ClassMembersGroups classMembersGroup in new ClassMembersGroups[] { ClassMembersGroups.PublicFields, ClassMembersGroups.ProtectedFields, ClassMembersGroups.InternalFields, ClassMembersGroups.ProtectedInternalFields, ClassMembersGroups.PrivateFields })
      {
        myFieldInfo = memberDeclaringType.GetMember(classMembersGroup, memberName) as MyFieldInfo;

        if (myFieldInfo != null)
        {
          return myFieldInfo;
        }
      }

      return myFieldInfo;
    }

    public MyEventInfo FindEvent(string xmlMemberId)
    {
      return FindEvent(xmlMemberId, false);
    }

    public MyEventInfo FindEvent(string xmlMemberId, bool global)
    {
      string memberName;
      string paramsStr;
      string returnTypeFullName;

      MyClassInfo memberDeclaringType = ExtractTypeMemberInfo(xmlMemberId, global, out memberName, out paramsStr, out returnTypeFullName);
      if (memberDeclaringType == null)
      {
        return null;
      }

      MyEventInfo myEventInfo = null;

      foreach (ClassMembersGroups classMembersGroup in new ClassMembersGroups[] { ClassMembersGroups.PublicEvents, ClassMembersGroups.ProtectedEvents, ClassMembersGroups.InternalEvents, ClassMembersGroups.ProtectedInternalEvents, ClassMembersGroups.PrivateEvents })
      {
        myEventInfo = memberDeclaringType.GetMember(classMembersGroup, memberName) as MyEventInfo;

        if (myEventInfo != null)
        {
          return myEventInfo;
        }
      }

      return myEventInfo;
    }

    public MyClassInfo FindNamespaceMember(string xmlMemberId)
    {
      int indexOfDot = -1;

      while (true)
      {
        string potentialNamespace;
        string potentialMemberName;

        indexOfDot = xmlMemberId.IndexOf('.', indexOfDot + 1);
        if (indexOfDot == -1)
        {
          potentialNamespace = MyNamespaceInfo.GLOBAL_NAMESPACE_NAME;
        }
        else
        {
          potentialNamespace = xmlMemberId.Substring(0, indexOfDot);
        }

        MyNamespaceInfo myNamespaceInfo = FindNamespace(potentialNamespace);

        if (myNamespaceInfo == null)
        {
          if (indexOfDot == -1)
          {
            break;
          }
          else
          {
            continue;
          }
        }

        if (indexOfDot + 1 < xmlMemberId.Length)
        {
          potentialMemberName = xmlMemberId.Substring(indexOfDot + 1).Replace('.', '/');
        }
        else
        {
          Logger.Warning("(XML) Malformed member ID: '{0}'.", xmlMemberId);
          continue;
        }

        int namespaceMembersGroupTypeIndex = 0;

        while (Enum.IsDefined(typeof(NamespaceMembersGroups), namespaceMembersGroupTypeIndex))
        {
          NamespaceMembersGroups namespaceMembersGroupType = (NamespaceMembersGroups)namespaceMembersGroupTypeIndex;
          MyClassInfo namespaceMember = myNamespaceInfo.FindMember(namespaceMembersGroupType, potentialMemberName);

          if (namespaceMember != null)
          {
            return namespaceMember;
          }

          namespaceMembersGroupTypeIndex++;
        }

        if (indexOfDot == -1)
        {
          break;
        }
      }

      return null;
    }

    private MyClassInfo FindGlobalNamespaceMember(string xmlMemberId)
    {
      int indexOfDot = -1;

      while (true)
      {
        string potentialNamespace;
        string potentialMemberName;

        indexOfDot = xmlMemberId.IndexOf('.', indexOfDot + 1);
        if (indexOfDot == -1)
        {
          potentialNamespace = MyNamespaceInfo.GLOBAL_NAMESPACE_NAME;
        }
        else
        {
          potentialNamespace = xmlMemberId.Substring(0, indexOfDot);
        }

        List<MyNamespaceInfo> namespacesInfos = assembliesInfo.FindNamespaces(potentialNamespace);
        if (namespacesInfos.Count == 0)
        {
          if (indexOfDot == -1)
          {
            break;
          }
          else
          {
            continue;
          }
        }

        if (indexOfDot + 1 < xmlMemberId.Length)
        {
          potentialMemberName = xmlMemberId.Substring(indexOfDot + 1).Replace('.', '/');
        }
        else
        {
          Logger.Warning("(XML) Malformed member ID: '{0}'.", xmlMemberId);
          continue;
        }

        foreach (MyNamespaceInfo myNamespaceInfo in namespacesInfos)
        {
          int namespaceMembersGroupTypeIndex = 0;
          while (Enum.IsDefined(typeof(NamespaceMembersGroups), namespaceMembersGroupTypeIndex))
          {
            NamespaceMembersGroups namespaceMembersGroupType = (NamespaceMembersGroups)namespaceMembersGroupTypeIndex;
            MyClassInfo namespaceMember = myNamespaceInfo.FindMember(namespaceMembersGroupType, potentialMemberName);

            if (namespaceMember != null)
            {
              return namespaceMember;
            }

            namespaceMembersGroupTypeIndex++;
          }
        }

        if (indexOfDot == -1)
        {
          break;
        }
      }

      return null;
    }

    public MyInvokableMemberInfo FindMethodOrConstructor(string xmlMemberId)
    {
      return FindMethodOrConstructor(xmlMemberId, false);
    }

    public MyInvokableMemberInfo FindMethodOrConstructor(string xmlMemberId, bool global)
    {
      string memberName;
      string paramsStr;
      string returnTypeFullName;

      MyClassInfo memberDeclaringType = ExtractTypeMemberInfo(xmlMemberId, global, out memberName, out paramsStr, out returnTypeFullName);

      if (memberDeclaringType == null)
      {
        return null;
      }

      bool isConstructor = false;

      if (memberName == "#ctor")
      {
        isConstructor = true;
        memberName = memberDeclaringType.Name;
      }

      ClassMembersGroups[] constructorMembersGroups = new ClassMembersGroups[] { ClassMembersGroups.PublicConstructors, ClassMembersGroups.ProtectedConstructors, ClassMembersGroups.InternalConstructors, ClassMembersGroups.ProtectedInternalConstructors, ClassMembersGroups.PrivateConstructors };
      ClassMembersGroups[] methodMembersGroups = new ClassMembersGroups[] { ClassMembersGroups.PublicMethodsOverloads, ClassMembersGroups.ProtectedMethodsOverloads, ClassMembersGroups.InternalMethodsOverloads, ClassMembersGroups.ProtectedInternalMethodsOverloads, ClassMembersGroups.PrivateMethodsOverloads };

      Debug.Assert(constructorMembersGroups.Length == methodMembersGroups.Length);

      for (int i = 0; i < constructorMembersGroups.Length; i++)
      {
        ClassMembersGroups classMembersGroup;

        if (isConstructor)
        {
          classMembersGroup = constructorMembersGroups[i];
        }
        else
        {
          classMembersGroup = methodMembersGroups[i];

          if (MyMethodInfo.MethodsNamesMappings.ContainsKey(memberName))
          {
            memberName = MyMethodInfo.MethodsNamesMappings[memberName];
          }
        }

        MyInvokableMembersOverloadsInfo invokableMembers = memberDeclaringType.GetMember(classMembersGroup, memberName) as MyInvokableMembersOverloadsInfo;

        if (invokableMembers == null)
        {
          continue;
        }

        List<string> paramsTypes = SplitXmlParamsString(paramsStr);
        foreach (MyInvokableMemberInfo myInvokableMember in invokableMembers)
        {
          List<MyGenericParameterInfo> memberGenericParameters = null;

          if (!isConstructor)
          {
            MyMethodInfo myMethodInfo = (MyMethodInfo)myInvokableMember;

            if (myMethodInfo.GenericParametersCount > 0)
            {
              memberGenericParameters = myMethodInfo.GenericParameters;
            }
          }

          if (SignaturesMatch(myInvokableMember.ParametersNames, myInvokableMember.Parameters, paramsTypes, memberDeclaringType, memberGenericParameters)
              && (isConstructor || returnTypeFullName == null
                  || ReturnTypesMatch(((MyMethodInfo)myInvokableMember).ReturnTypeFullNameWithoutRevArrayStrings, returnTypeFullName, memberDeclaringType, memberGenericParameters)))
          {
            return myInvokableMember;
          }
        }
      }

      return null;
    }

    public MyPropertyInfo FindProperty(string xmlMemberId)
    {
      return FindProperty(xmlMemberId, false);
    }

    public MyPropertyInfo FindProperty(string xmlMemberId, bool global)
    {
      string memberName;
      string paramsStr;
      string returnTypeFullName;

      MyClassInfo memberDeclaringType = ExtractTypeMemberInfo(xmlMemberId, global, out memberName, out paramsStr, out returnTypeFullName);

      if (memberDeclaringType == null)
      {
        return null;
      }

      foreach (ClassMembersGroups classMembersGroup in new ClassMembersGroups[] { ClassMembersGroups.PublicPropertiesOverloads, ClassMembersGroups.ProtectedPropertiesOverloads, ClassMembersGroups.InternalPropertiesOverloads, ClassMembersGroups.ProtectedInternalPropertiesOverloads, ClassMembersGroups.PrivatePropertiesOverloads })
      {
        MyPropertiesOverloadsInfo properties = memberDeclaringType.GetMember(classMembersGroup, memberName) as MyPropertiesOverloadsInfo;

        if (properties == null)
        {
          continue;
        }

        List<string> paramsTypes = SplitXmlParamsString(paramsStr);

        foreach (MyPropertyInfo myPropertyInfo in properties)
        {
          if (SignaturesMatch(myPropertyInfo.ParametersNames, myPropertyInfo.Parameters, paramsTypes, memberDeclaringType, null))
          {
            return myPropertyInfo;
          }
        }
      }

      return null;
    }

    private static MyParameterInfo FindParameter(MyInvokableMemberInfo myInvokableMemberInfo, string paramName)
    {
      if (!myInvokableMemberInfo.Parameters.ContainsKey(paramName))
      {
        return null;
      }

      return myInvokableMemberInfo.Parameters[paramName];
    }

    private static MyParameterInfo FindParameter(MyPropertyInfo myPropertyInfo, string paramName)
    {
      if (!myPropertyInfo.Parameters.ContainsKey(paramName))
      {
        return null;
      }

      return myPropertyInfo.Parameters[paramName];
    }

    private static MyParameterInfo FindParameter(MyDelegateInfo myDelegateInfo, string paramName)
    {
      if (!myDelegateInfo.Parameters.ContainsKey(paramName))
      {
        return null;
      }

      return myDelegateInfo.Parameters[paramName];
    }

    private static MyGenericParameterInfo FindGenericParameter(MyClassInfo myClassInfo, string typeParamName)
    {
      return myClassInfo.FindGenericParameter(typeParamName);
    }

    private static MyGenericParameterInfo FindGenericParameter(MyMethodInfo myMethodInfo, string typeParamName)
    {
      return myMethodInfo.FindGenericParameter(typeParamName);
    }

    private MyClassInfo ExtractTypeMemberInfo(string xmlMemberId, bool global, out string memberName, out string paramsStr, out string returnTypeFullName)
    {
      memberName = null;
      paramsStr = null;
      returnTypeFullName = null;

      string fullMemberName;

      int indexOfLastTilde = xmlMemberId.LastIndexOf('~');
      if (indexOfLastTilde != -1)
      {
        if (indexOfLastTilde + 1 >= xmlMemberId.Length)
        {
          Logger.Warning("(XML) Malformed member ID: '{0}'.", xmlMemberId);
          return null;
        }

        returnTypeFullName = xmlMemberId.Substring(indexOfLastTilde + 1);
        xmlMemberId = xmlMemberId.Substring(0, indexOfLastTilde);
      }

      int indexOfOpeningBrace = xmlMemberId.IndexOf('(');
      if (indexOfOpeningBrace != -1)
      {
        if (indexOfOpeningBrace + 1 >= xmlMemberId.Length)
        {
          Logger.Warning("(XML) Malformed member ID: '{0}'.", xmlMemberId);
          return null;
        }

        paramsStr = xmlMemberId.Substring(indexOfOpeningBrace + 1, xmlMemberId.Length - indexOfOpeningBrace - 2);
        fullMemberName = xmlMemberId.Substring(0, indexOfOpeningBrace);
      }
      else // no params
      {
        fullMemberName = xmlMemberId;
      }

      int indexOfLastDot = fullMemberName.LastIndexOf('.');
      if (indexOfLastDot == -1 || indexOfLastDot + 1 >= fullMemberName.Length)
      {
        Logger.Warning("(XML) Malformed member ID: '{0}'.", xmlMemberId);
        return null;
      }

      string declaringTypeXmlId = fullMemberName.Substring(0, indexOfLastDot);
      MyClassInfo declaringType = null;

      if (global)
      {
        foreach (MyAssemblyInfo myAssemblyInfo in assembliesInfo.Assemblies)
        {
          declaringType = myAssemblyInfo.FindNamespaceMember(declaringTypeXmlId);
          if (declaringType != null)
          {
            break;
          }
        }
      }
      else
      {
        declaringType = FindNamespaceMember(declaringTypeXmlId);
      }

      if (declaringType == null)
      {
        Logger.Warning("(XML) Couldn't find declaring type of a member with id '{0}'.", xmlMemberId);
        return null;
      }

      memberName = fullMemberName.Substring(indexOfLastDot + 1);

      return declaringType;
    }

    private static List<string> SplitXmlParamsString(string paramsStr)
    {
      List<string> result = new List<string>();

      if (paramsStr == null)
      {
        return result;
      }

      int curlyBracesDepth = 0;
      int squareBracketsDepth = 0;
      int lastCutIndex = 0;

      for (int i = 0; i < paramsStr.Length; i++)
      {
        if (paramsStr[i] == ',')
        {
          if (curlyBracesDepth == 0 && squareBracketsDepth == 0)
          {
            result.Add(paramsStr.Substring(lastCutIndex, i - lastCutIndex));
            lastCutIndex = i + 1;
          }
        }
        else if (paramsStr[i] == '{')
        {
          curlyBracesDepth++;
        }
        else if (paramsStr[i] == '}')
        {
          curlyBracesDepth--;
        }
        else if (paramsStr[i] == '[')
        {
          squareBracketsDepth++;
        }
        else if (paramsStr[i] == ']')
        {
          squareBracketsDepth--;
        }
      }

      if (curlyBracesDepth != 0)
      {
        Logger.Warning("(XML) Malformed parameters string: '{0}'.", paramsStr);
      }

      if (paramsStr != "")
      {
        int i = paramsStr.Length;
        result.Add(paramsStr.Substring(lastCutIndex, i - lastCutIndex));
      }

      return result;
    }

    private static bool SignaturesMatch(List<String> memberParametersNames, Dictionary<string, MyParameterInfo> memberParameters, List<string> xmlParamsTypes, MyClassInfo memberDeclaringType, List<MyGenericParameterInfo> memberGenericParameters)
    {
      if (memberParametersNames.Count != xmlParamsTypes.Count)
      {
        return false;
      }

      bool signaturesMatch = true;
      int i = 0;
      foreach (string paramName in memberParametersNames)
      {
        MyParameterInfo myParameterInfo = memberParameters[paramName];

        string almostXmlRepresentation = myParameterInfo.GetXMLCompatibleRepresentation();
        string almostCSRepresentation = GetAlmostCSRepresentation(xmlParamsTypes[i], memberDeclaringType, memberGenericParameters);

        if (almostXmlRepresentation != almostCSRepresentation)
        {
          signaturesMatch = false;
          break;
        }

        i++;
      }

      return signaturesMatch;
    }

    private static bool ReturnTypesMatch(string csTypeFullName, string xmlTypeFullName, MyClassInfo memberDeclaringType, List<MyGenericParameterInfo> memberGenericParameters)
    {
      string almostXmlRepresentation = MyParameterInfo.GetXMLCompatibleRepresentation(csTypeFullName, false, false);
      string almostCSRepresentation = GetAlmostCSRepresentation(xmlTypeFullName, memberDeclaringType, memberGenericParameters);

      return almostXmlRepresentation == almostCSRepresentation;
    }

    private static string GetAlmostCSRepresentation(string xmlParamType, MyClassInfo memberDeclaringType, List<MyGenericParameterInfo> memberGenericParameters)
    {
      string result;

      if (xmlParamType.Contains("`"))
      {
        // resolve generic parameters

        result = new MatchEvaluatorWrapper(memberDeclaringType, memberGenericParameters).Process(xmlParamType);
      }
      else
      {
        result = xmlParamType;
      }

      return result.Replace('{', '<').Replace('}', '>');
    }

    #region MatchEvaluatorWrapper inner class

    private class MatchEvaluatorWrapper
    {
      // matches xml doc generic params specs, eg. `1, ``3 etc.
      private static readonly Regex pattern = new Regex("(``(?<IndexDouble>[0-9]+))|(`(?<IndexSingle>[0-9]+))", RegexOptions.Compiled | RegexOptions.Singleline);

      private readonly MyClassInfo _memberDeclaringType;
      private readonly List<MyGenericParameterInfo> _memberGenericParameters;

      #region Constructor(s)

      public MatchEvaluatorWrapper(MyClassInfo memberDeclaringType, List<MyGenericParameterInfo> memberGenericParameters)
      {
        _memberDeclaringType = memberDeclaringType;
        _memberGenericParameters = memberGenericParameters;
      }

      #endregion

      #region Generic params processing

      public string Process(string xmlParamType)
      {
        return pattern.Replace(xmlParamType, new MatchEvaluator(MatchEvaluatorImpl));
      }

      #endregion

      #region Match evaluator

      private string MatchEvaluatorImpl(Match match)
      {
        Debug.Assert(match.Groups["IndexSingle"].Value != "" || match.Groups["IndexDouble"].Value != "", "Impossible! 1");

        int indexSingle = -1;
        int indexDouble = -1;

        if (match.Groups["IndexSingle"].Value != "")
        {
          Int32.TryParse(match.Groups["IndexSingle"].Value, out indexSingle);
        }
        else // match.Groups["IndexDouble"].Value != ""
        {
          Int32.TryParse(match.Groups["IndexDouble"].Value, out indexDouble);
        }

        Debug.Assert(indexSingle != -1 || indexDouble != -1, "Impossible! 2");

        string genericParamName = null;

        if (indexSingle != -1)
        {
          Debug.Assert(_memberDeclaringType != null, "Impossible! 3");
          Debug.Assert(indexSingle < _memberDeclaringType.AllGenericParametersNamesCount, "Impossible! 4");

          genericParamName = _memberDeclaringType.AllGenericParametersNames[indexSingle];
        }
        else // indexDouble != -1
        {
          Debug.Assert(_memberGenericParameters != null, "Impossible! 5");
          Debug.Assert(indexDouble < _memberGenericParameters.Count, "Impossible! 6");

          genericParamName = _memberGenericParameters[indexDouble].Name;
        }

        return genericParamName;
      }

      #endregion
    }

    #endregion

    #endregion

    #region Private XML helper methods

    private static string ReadXmlNodes(XPathNavigator memberNodeNavigator, string xmlNodeName)
    {
      XPathNodeIterator xmlNodes = memberNodeNavigator.Select(xmlNodeName);

      string result = String.Empty;
      bool first = true;

      while (xmlNodes.MoveNext())
      {
        if (!first)
        {
          result += " ";
        }

        result += xmlNodes.Current.InnerXml.Trim();

        first = false;
      }

      return result.Trim();
    }

    #endregion

    #region Public properties

    public int NamespacesCount
    {
      get { return namespaces == null ? 0 : namespaces.Count; }
    }

    public Dictionary<string, MyNamespaceInfo>.ValueCollection Namespaces
    {
      get
      {
        if (namespaces == null)
        {
          return null;
        }

        return namespaces.Values;
      }
    }

    public bool HasMembers
    {
      get
      {
        return namespaces != null && namespaces.Count > 0;
      }
    }

    #endregion

    #region ISummarisableMember Members

    public string DisplayableName
    {
      get { return Name; }
    }

    #endregion

    #region Enumeration

    public IEnumerator<ISummarisableMember> GetEnumerator()
    {
      List<string> sortedKeys = new List<string>();

      if (namespaces != null)
      {
        sortedKeys.AddRange(namespaces.Keys);
      }

      sortedKeys.Sort();

      foreach (string key in sortedKeys)
      {
        yield return (ISummarisableMember)namespaces[key];
      }
    }

    #endregion

    #region MetaClass overrides

    public override string GetMetaName()
    {
      return "Assembly";
    }

    #endregion
  }
}
