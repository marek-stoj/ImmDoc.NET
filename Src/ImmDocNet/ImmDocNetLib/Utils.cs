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
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Xml.XPath;
using System.Xml;
using Imm.ImmDocNetLib.MyReflection.MetaClasses;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Imm.ImmDocNetLib
{
  public static class Utils
  {
    private static readonly Regex _StackTracePattern = new Regex(".* (w|at) (.*) (w|in) (.*):.* ([0-9]+)", RegexOptions.Compiled | RegexOptions.Multiline);

    // matches formal generic arguments, eg. <T,  B_2> etc.
    private static readonly Regex _FormalGenericParamsPattern = new Regex("<[ \t]*([A-Za-z_][A-Za-z_0-9]*)([ \t]*,[ \t]*[A-Za-z_][A-Za-z_0-9]*)*[ \t]*>", RegexOptions.Compiled);

    private static bool _includeInternalMembers;
    private static bool _includePrivateMembers;

    #region HTML and XML routines

    public static string HTMLEncode(string text)
    {
      return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    public static string GetAssemblyNameFromXmlDocumentation(string xmlDocPath)
    {
      try
      {
        var xPathDocument = new XPathDocument(xmlDocPath);
        XPathNavigator xPathNavigator = xPathDocument.CreateNavigator();
        XPathNodeIterator xPathNodeIterator = xPathNavigator.Select("/doc/assembly/name");
        bool hasAtLeastOneNode = xPathNodeIterator.MoveNext();

        if (!hasAtLeastOneNode)
        {
          return null;
        }

        string assemblyName = xPathNodeIterator.Current.Value.Trim();

        // let's check if there is the absolute path to the assembly (Mono does this)
        int lastIndexOfSlash = assemblyName.LastIndexOf('/');
        int lastIndexOfBackslash = assemblyName.LastIndexOf('\\');
        int lastIndex = Math.Max(lastIndexOfSlash, lastIndexOfBackslash);

        if (lastIndex == -1)
        {
          return assemblyName;
        }

        return assemblyName.Substring(lastIndex + 1);
      }
      catch (XmlException)
      {
        return null;
      }
    }

    public static string ConvertNameToXmlDocForm(string name)
    {
      return ConvertNameToXmlDocForm(name, false);
    }

    public static string ConvertNameToXmlDocForm(string name, bool useDoubleChar)
    {
      MatchEvaluator matchEvaluator =
        useDoubleChar
          ? MatchEvaluatorWrapper.DoubleChar
          : MatchEvaluatorWrapper.SingleChar;

      return _FormalGenericParamsPattern.Replace(name, matchEvaluator);
    }

    #endregion

    #region Various routines

    public static string GetUnqualifiedName(string fullName)
    {
      int indexOfLastDot = fullName.LastIndexOf('.');
      string result = String.Empty;

      if (indexOfLastDot == -1)
      {
        result = fullName;
      }
      else if (indexOfLastDot + 1 < fullName.Length)
      {
        result = fullName.Substring(indexOfLastDot + 1);
      }

      return result;
    }

    public static string GetExtension(string path)
    {
      // body of this method has been copied from System.IO.Path.GetExtension() but with removed
      // calls to CheckInvalidPathChars()

      if (path == null)
      {
        throw new ArgumentException("path");
      }

      int length = path.Length;
      int startIndex = length;

      while (--startIndex >= 0)
      {
        char ch = path[startIndex];

        if (ch == '.')
        {
          if (startIndex != (length - 1))
          {
            return path.Substring(startIndex, length - startIndex);
          }

          return String.Empty;
        }

        if (((ch == Path.DirectorySeparatorChar) || (ch == Path.AltDirectorySeparatorChar)) || (ch == Path.VolumeSeparatorChar))
        {
          break;
        }
      }

      return String.Empty;
    }

    public static bool IsPathRooted(string path)
    {
      // body of this method has been copied from System.IO.Path.IsPathRooted() but with removed
      // calls to CheckInvalidPathChars()

      if (path == null)
      {
        throw new ArgumentException("path");
      }

      int length = path.Length;

      if (((length >= 1) && ((path[0] == Path.DirectorySeparatorChar) || (path[0] == Path.AltDirectorySeparatorChar))) || ((length >= 2) && (path[1] == Path.VolumeSeparatorChar)))
      {
        return true;
      }

      return false;
    }

    public static string CombinePaths(string path1, string path2)
    {
      // body of this method has been copied from System.IO.Path.Combine() but with removed
      // calls to CheckInvalidPathChars()

      if ((path1 == null) || (path2 == null))
      {
        throw new ArgumentNullException((path1 == null) ? "path1" : "path2");
      }

      if (path2.Length == 0)
      {
        return path1;
      }

      if (path1.Length == 0)
      {
        return path2;
      }

      if (IsPathRooted(path2))
      {
        return path2;
      }

      char ch = path1[path1.Length - 1];

      if (((ch != Path.DirectorySeparatorChar) && (ch != Path.AltDirectorySeparatorChar)) && (ch != Path.VolumeSeparatorChar))
      {
        return path1 + Path.DirectorySeparatorChar + path2;
      }

      return path1 + path2;
    }

    public static string CombineMultiplePaths(params string[] paths)
    {
      string result = paths[0];

      for (int i = 1; i < paths.Length; i++)
      {
        result = CombinePaths(result, paths[i]);
      }

      return result;
    }

    public static string SimplifyStackTrace(string stackTrace)
    {
      MatchCollection matches = _StackTracePattern.Matches(stackTrace);
      var result = new StringBuilder();

      foreach (Match match in matches)
      {
        string fileName = Path.GetFileName(match.Groups[4].Value);
        string methodName = match.Groups[2].Value;

        int i = methodName.IndexOf('(');
        int j = methodName.Substring(0, methodName.IndexOf('(')).LastIndexOf('.') + 1;

        methodName = methodName.Substring(j, i - j) + "()";

        result.Append("\n  at " + methodName + " in " + fileName + " : " + match.Groups[5].Value);
      }

      return result + "\n";
    }

    public static string CreateNSpaces(int n)
    {
      var sb = new StringBuilder(n);

      for (int i = 0; i < n; i++)
      {
        sb.Append(' ');
      }

      return sb.ToString();
    }

    public static Collection<MemberReference> GetTypeMembers(TypeDefinition typeDefinition)
    {
      var members = new Collection<MemberReference>();

      foreach (MemberReference member in typeDefinition.Events)
      {
        members.Add(member);
      }

      foreach (MemberReference member in typeDefinition.Fields)
      {
        members.Add(member);
      }

      foreach (MemberReference member in typeDefinition.Methods)
      {
        if (member is MethodDefinition)
        {
          var methodDefinition = (MethodDefinition)member;

          if (methodDefinition.IsSpecialName &&
             !methodDefinition.IsConstructor &&
             !MyMethodInfo.IsMethodNameMapped(methodDefinition.Name))
          {
            continue;
          }
        }

        members.Add(member);
      }

      foreach (MemberReference member in typeDefinition.Properties)
      {
        members.Add(member);
      }

      foreach (MemberReference member in typeDefinition.NestedTypes)
      {
        members.Add(member);
      }

      return members;
    }

    public static EventDefinition GetEvent(TypeDefinition declaringClass, string eventName)
    {
      foreach (EventDefinition eventDefinition in declaringClass.Events)
      {
        if (eventDefinition.Name == eventName)
        {
          return eventDefinition;
        }
      }

      return null;
    }

    public static TypeDefinition GetNestedType(TypeDefinition declaringClass, string name)
    {
      foreach (TypeDefinition nestedType in declaringClass.NestedTypes)
      {
        if (nestedType.Name == name)
        {
          return nestedType;
        }
      }

      return null;
    }

    public static bool IsGenericParameter(TypeReference typeReference)
    {
      return typeReference is GenericParameter;
    }

    public static bool IsTypeNested(TypeDefinition typeDefinition)
    {
      return typeDefinition.IsNestedAssembly || typeDefinition.IsNestedFamily || typeDefinition.IsNestedFamilyAndAssembly || typeDefinition.IsNestedFamilyOrAssembly || typeDefinition.IsNestedPrivate || typeDefinition.IsNestedPublic;
    }

    public static bool IsDelegate(TypeDefinition typeDefinition)
    {
      return typeDefinition.BaseType != null && typeDefinition.BaseType.FullName == "System.MulticastDelegate";
    }

    public static bool IsTypeGeneric(TypeDefinition typeDefinition)
    {
      return typeDefinition.GenericParameters.Count > 0;
    }

    public static bool IsGenericMethod(MethodReference methodDefinition)
    {
      return methodDefinition.GenericParameters.Count > 0;
    }

    public static List<TypeDefinition> GetAllTypes(AssemblyDefinition assemblyDefinition)
    {
      var types = new List<TypeDefinition>();

      foreach (ModuleDefinition moduleDef in assemblyDefinition.Modules)
      {
        foreach (TypeDefinition typeDef in moduleDef.Types)
        {
          types.Add(typeDef);
        }
      }

      return types;
    }

    public static bool ContainsCustomAttribute(ParameterDefinition parameterDefinition, string attributeFullName)
    {
      foreach (CustomAttribute customAttribute in parameterDefinition.CustomAttributes)
      {
        if (customAttribute.Constructor.DeclaringType.FullName == attributeFullName)
        {
          return true;
        }
      }

      return false;
    }

    public static string GetTypeNamespace(TypeDefinition typeDefinition)
    {
      if (IsTypeNested(typeDefinition))
      {
        return GetNamespaceOfNestedType(typeDefinition);
      }

      return string.IsNullOrEmpty(typeDefinition.Namespace) ? MyNamespaceInfo.GLOBAL_NAMESPACE_NAME : typeDefinition.Namespace;
    }

    #endregion

    #region MatchEvaluatorWrapper inner class

    private class MatchEvaluatorWrapper
    {
      private static readonly MatchEvaluator _SingleChar = new MatchEvaluatorWrapper("`").GenericParamsConversionMatchEvaluator;
      private static readonly MatchEvaluator _DoubleChar = new MatchEvaluatorWrapper("``").GenericParamsConversionMatchEvaluator;

      private readonly string _prefix;

      #region Constructor(s)

      private MatchEvaluatorWrapper(string prefix)
      {
        _prefix = prefix;
      }

      #endregion

      #region MatchEvaluator

      private static int CountChars(string str, char ch)
      {
        int result = 0;

        for (int i = 0; i < str.Length; i++)
        {
          if (str[i] == ch) { result++; }
        }

        return result;
      }

      private string GenericParamsConversionMatchEvaluator(Match match)
      {
        Debug.Assert(match.Value != "", "Impossible! 1");

        int paramsCount = CountChars(match.Value, ',') + 1;

        return _prefix + paramsCount;
      }

      #endregion

      #region Public properties

      public static MatchEvaluator SingleChar
      {
        get { return _SingleChar; }
      }

      public static MatchEvaluator DoubleChar
      {
        get { return _DoubleChar; }
      }

      #endregion
    }

    #endregion

    #region Types and members inclusion routines

    public static bool ShouldIncludeType(TypeDefinition typeDefinition)
    {
      if (IsCompilerGenerated(typeDefinition))
      {
        return false;
      }

      bool result = typeDefinition.IsPublic || typeDefinition.IsNestedFamily || typeDefinition.IsNestedPublic || typeDefinition.IsNestedFamilyOrAssembly;

      if (_includeInternalMembers)
      {
        if (IsTypeNested(typeDefinition))
        {
          result = result || typeDefinition.IsNestedAssembly;
        }
        else
        {
          result = result || typeDefinition.IsNotPublic;
        }
      }

      if (_includePrivateMembers)
      {
        result = result || typeDefinition.IsNestedPrivate;
      }

      if (IsTypeNested(typeDefinition))
      {
        // nested type should only be included if parent type is visible
        var typeRef = typeDefinition.DeclaringType;
        foreach (TypeDefinition type in typeDefinition.Module.Types)
        {
           if (type.FullName == typeRef.FullName)
           {
             return result && ShouldIncludeType(type);
           }
        }
      }

      return result;
    }

    private static bool IsCompilerGenerated(TypeDefinition type)
    {
      if (type.CustomAttributes.Count == 0)
      {
        return false;
      }

      var typeName = typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).FullName;
      return type.CustomAttributes.OfType<CustomAttribute>().Any(ca => ca.Constructor.DeclaringType.FullName == typeName);
    }

    public static bool ShouldIncludeMember(MemberReference memberReference)
    {
      if (memberReference is TypeReference)
      {
        return ShouldIncludeType((TypeDefinition)memberReference);
      }

      if (memberReference is EventDefinition)
      {
        var eventDefinition = (EventDefinition)memberReference;

        MethodDefinition adderInfo = eventDefinition.AddMethod;
        Debug.Assert(adderInfo != null, "Impossible! add_Event() must have been generated.");

        bool result = adderInfo.IsPublic || adderInfo.IsFamily || adderInfo.IsFamilyOrAssembly;

        if (_includeInternalMembers)
        {
          result = result || adderInfo.IsAssembly;
        }

        if (_includePrivateMembers)
        {
          result = result || adderInfo.IsPrivate;
        }

        return result;
      }

      if (memberReference is FieldDefinition)
      {
        var fieldDefinition = (FieldDefinition)memberReference;
        bool result = fieldDefinition.IsPublic || fieldDefinition.IsFamily || fieldDefinition.IsFamilyOrAssembly;

        if (_includeInternalMembers)
        {
          result = result || fieldDefinition.IsAssembly;
        }

        if (_includePrivateMembers)
        {
          result = result || fieldDefinition.IsPrivate;
        }

        return result;
      }

      if (memberReference is MethodDefinition)
      {
        var methodDefinition = (MethodDefinition)memberReference;
        bool result = methodDefinition.IsPublic || methodDefinition.IsFamily || methodDefinition.IsFamilyOrAssembly;

        if (_includeInternalMembers)
        {
          result = result || methodDefinition.IsAssembly;
        }

        if (_includePrivateMembers)
        {
          result = result || methodDefinition.IsPrivate;
        }

        return result;
      }

      if (memberReference is PropertyDefinition)
      {
        var propertyDefinition = (PropertyDefinition)memberReference;
        MethodDefinition getter = propertyDefinition.GetMethod;
        MethodDefinition setter = propertyDefinition.SetMethod;
        MethodDefinition getterOrSetter = getter ?? setter;
        Debug.Assert(getterOrSetter != null, "Impossible! Property must have either getter or setter or both.");

        bool result = getterOrSetter.IsPublic || getterOrSetter.IsFamily || getterOrSetter.IsFamilyOrAssembly;

        if (_includeInternalMembers)
        {
          result = result || getterOrSetter.IsAssembly;
        }

        if (_includePrivateMembers)
        {
          result = result || getterOrSetter.IsPrivate;
        }

        return result;
      }

      Debug.Assert(false, "Impossible! Couldn't recognize type of a member.");

      return false;
    }

    #endregion

    #region Collection utils

    public static void RemoveItems<T>(IList<T> list, IList<int> indicesToBeRemoved)
    {
      for (int i = indicesToBeRemoved.Count - 1; i >= 0; i--)
      {
        list.RemoveAt(indicesToBeRemoved[i]);
      }
    }

    #endregion

    #region Generics utils

    public static Collection<TypeReference> GetGenericArguments(TypeReference type)
    {
      if (type is GenericInstanceType)
      {
        return ((GenericInstanceType)type).GenericArguments;
      }

      return null;
    }

    #endregion

    #region Private helper methods

    private static string GetNamespaceOfNestedType(TypeDefinition nestedTypeDefinition)
    {
      Debug.Assert(IsTypeNested(nestedTypeDefinition));

      TypeReference typeReference = nestedTypeDefinition;

      while (string.IsNullOrEmpty(typeReference.Namespace))
      {
        typeReference = typeReference.DeclaringType;

        if (typeReference == null)
        {
          break;
        }
      }

      if (typeReference != null && !string.IsNullOrEmpty(typeReference.Namespace))
      {
        return typeReference.Namespace;
      }

      return MyNamespaceInfo.GLOBAL_NAMESPACE_NAME;
    }

    #endregion

    #region Properties

    public static bool IncludeInternalMembers
    {
      get { return _includeInternalMembers; }
      set { _includeInternalMembers = value; }
    }

    public static bool IncludePrivateMembers
    {
      get { return _includePrivateMembers; }
      set { _includePrivateMembers = value; }
    }

    #endregion
  }
}
