/*
 * Copyright 2007 Marek Stój
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
using System.Diagnostics;
using System.Text.RegularExpressions;
using Imm.ImmDocNetLib.MyReflection.MetaClasses;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Imm.ImmDocNetLib.MyReflection
{
  static class Tools
  {
    private static readonly Regex CompilerGeneratedStuffRegex = new Regex("(/\\<(?<Id>[^\\>]*)\\>)|(^\\<(?<Id>[^\\>]*)\\>)", RegexOptions.Compiled);
    private static readonly Regex RevCompilerGeneratedStuffRegex = new Regex("___#(?<Id>[^\\>]*)#___", RegexOptions.Compiled);

    #region Parsing of CLR's internal representation of type names

    /// <summary>
    /// Converts CLR's internal representation of a type name to more readable form.
    /// </summary>
    /// <example>
    /// Given for example the string "System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"
    /// this method will return "System.Collections.Generic.List&lt;System.Int32&gt;". Note that HTML entities are used here only in this XML comment.
    /// If you want later to display such a type name in the web browser you have to encode special characters with HTML entities by yourself.
    /// </example>
    /// <param name="type">Type for which we want to obtain a readable name.</param>
    /// <returns>
    /// In array[0]: converted type name or null if conversion couldn't be done.
    /// In array[1]: converted type name or null if conversion couldn't be done (without array strings being reversed).
    /// </returns>
    public static string[] GetHumanReadableForms(TypeReference typeReference)
    {
      string typeFullName = GetTypeFullName(typeReference);

      Debug.Assert(typeFullName != null, "Impossible!");

      // convert to System.Reflection format
      typeFullName = ConvertToSystemReflectionFormat(typeFullName);

      if (typeFullName.EndsWith("&"))
      {
        typeFullName = typeFullName.TrimEnd('&');
      }

      if (typeReference is ArrayType)
      {
        int indexOfFirstOpeningBracket = typeFullName.IndexOf('[');
        Debug.Assert(indexOfFirstOpeningBracket != -1, "Impossible! This type is an array type.");

        string arrayStr = typeFullName.Substring(indexOfFirstOpeningBracket);
        string name = typeFullName.Substring(0, indexOfFirstOpeningBracket);

        typeFullName = name + ProcessGenericArguments(typeReference) + arrayStr;
      }
      else
      {
        typeFullName += ProcessGenericArguments(typeReference);
      }

      string[] result = GetHumanReadableFormsAux(typeFullName);

      if (result == null || result[0] == null || result[1] == null)
      {
        Logger.Warning("Couldn't parse type name '{0}'.", typeFullName);

        return new[] { "?", "?" };
      }

      return result;
    }

    private static string ProcessGenericArguments(TypeReference type)
    {
      var genericArgsOrParams = new List<TypeReference>();

      if (type is TypeDefinition)
      {
        foreach (GenericParameter genericParameter in type.GenericParameters)
        {
          genericArgsOrParams.Add(genericParameter);
        }
      }
      else
      {
        var genericArguments = Utils.GetGenericArguments(type);

        if (genericArguments != null)
        {
          foreach (TypeReference genericArgument in genericArguments)
          {
            genericArgsOrParams.Add(genericArgument);
          }
        }
      }

      if (genericArgsOrParams.Count == 0) { return ""; }

      StringBuilder sb = new StringBuilder("[");
      bool first = true;

      foreach (TypeReference genericArgOrParam in genericArgsOrParams)
      {
        if (!first) { sb.Append(','); }

        sb.Append('[');

        string typeFullName = GetTypeFullName(genericArgOrParam);

        if (genericArgOrParam.GenericParameters.Count > 0)
        {
          Debug.Assert(genericArgOrParam.FullName == null, "Impossible!");

          string arrayStr = null;

          if (genericArgOrParam is ArrayType)
          {
            int indexOfFirstOpeningBracket = typeFullName.IndexOf('[');

            Debug.Assert(indexOfFirstOpeningBracket != -1, "Impossible! This type is an array type.");

            arrayStr = typeFullName.Substring(indexOfFirstOpeningBracket);
            typeFullName = typeFullName.Substring(0, indexOfFirstOpeningBracket);
          }

          sb.Append(typeFullName);
          sb.Append(ProcessGenericArguments(genericArgOrParam));
          sb.Append(arrayStr);
        }
        else
        {
          sb.Append(typeFullName);
        }

        sb.Append(']');

        first = false;
      }

      sb.Append(']');

      return sb.ToString();
    }

    private static string GetTypeFullName(TypeReference type)
    {
      if (type.FullName == null)
      {
        bool isGenericParameter = false;
        TypeReference tmpType = null;

        if (type is ArrayType)
        {
          tmpType = ((ArrayType)type).ElementType;

          while (tmpType is ArrayType)
          {
            tmpType = ((ArrayType)tmpType).ElementType;
          }

          while (tmpType is PointerType)
          {
            tmpType = ((PointerType)tmpType).ElementType;
          }

          isGenericParameter = tmpType is GenericParameter;
        }
        else if (type is ByReferenceType)
        {
          isGenericParameter = ((ByReferenceType)type).ElementType is GenericParameter;
        }
        else if (type is PointerType)
        {
          tmpType = ((PointerType)type).ElementType;

          while (tmpType is PointerType)
          {
            tmpType = ((PointerType)tmpType).ElementType;
          }

          isGenericParameter = tmpType is GenericParameter;
        }
        else
        {
          isGenericParameter = type is GenericParameter;
        }

        if (!isGenericParameter)
        {
          if (tmpType != null && tmpType.DeclaringType != null) // nested
          {
            return GetTypeFullName(tmpType.DeclaringType) + "/" + type.Name;
          }
          
          if (type.DeclaringType != null) // nested
          {
            return GetTypeFullName(type.DeclaringType) + "/" + type.Name;
          }
          
          if (!string.IsNullOrEmpty(type.Namespace))
          {
            return type.Namespace + "." + type.Name;
          }

          return type.Name;
        }
        
        return type.Name;
      }

      return type.FullName;
    }

    /// <summary>
    /// Helper function for GetHumanReadableForm().
    /// </summary>
    /// <param name="typeFullName">CLR's internal representation of a type name.</param>
    /// <returns>
    /// In array[0]: converted type name or null if conversion couldn't be done.
    /// In array[1]: converted type name or null if conversion couldn't be done (without array strings being reversed).
    /// </returns>
    private static string[] GetHumanReadableFormsAux(string typeFullName)
    {
      string[] result = { typeFullName, typeFullName };

      try
      {
        int indexOfGenericSeparator = result[0].IndexOf('`');

        if (indexOfGenericSeparator == -1) // non-generic type
        {
          // get rid of ", mscorlib, Version=2.0..." and such stuff
          int endOfNameIndex = result[0].IndexOf(", ");
          if (endOfNameIndex != -1)
          {
            result[0] = result[0].Substring(0, endOfNameIndex);
            result[1] = result[0];
          }

          // if the type is an array type extract the array string and reverse it
          int indexOfBeginningOfArray = result[0].IndexOf('[');
          if (indexOfBeginningOfArray != -1)
          {
            string arrayStr = result[0].Substring(indexOfBeginningOfArray);

            result[1] = result[0].Substring(0, indexOfBeginningOfArray)
                        + arrayStr.Replace("[,", "[0:,").Replace(",", ",0:");

            result[0] = result[0].Substring(0, indexOfBeginningOfArray)
                      + ReverseArrayString(arrayStr);
          }

          return result;
        }
        else // generic type
        {
          // get rid of ", mscorlib, Version=2.0..." and such stuff
          int indexOfLastSquareBracket = result[0].LastIndexOf(']');
          result[0] = result[0].Substring(0, indexOfLastSquareBracket + 1);
          result[1] = result[0];

          // get an array string (if present)
          int lastIndexOfDoubleSquareBrackets = result[0].LastIndexOf("]]");
          if (lastIndexOfDoubleSquareBrackets == -1)
          {
            return null;
          }
          string arrayStr = result[0].Substring(lastIndexOfDoubleSquareBrackets + 2);

          // we don't want to deal with arrays now, so we extract only the type part of it
          // eg. Imm.SomeClass`1[[System.Int32, ...]][][,,][][]
          //  -> Imm.SomeClass`1[[System.Int32, ...]]
          result[0] = result[0].Substring(0, lastIndexOfDoubleSquareBrackets + 2);
          result[1] = result[0];

          // now we extract the type part of a generic type declaration
          // eg. Imm.SomeGenericClass`2+SomeInnerClass`1+Enumerator[[System.Int32, mscorlib ...], [System.String, ...], [System.Int16, ...]]
          //  -> Imm.SomeGenericClass`2+SomeInnerClass`1+Enumerator
          int indexOfFirstSquareBracket = result[0].IndexOf('[', indexOfGenericSeparator);
          string genericType = result[0].Substring(0, indexOfFirstSquareBracket);

          // then we count the total number of type params
          int totalGenericParamsCount = 0;
          for (int i = 0; i < genericType.Length; i++)
          {
            char ch = genericType[i];

            if (ch == '`')
            {
              int newIndex;

              totalGenericParamsCount += ParseNumber(genericType, i + 1, out newIndex);

              i = newIndex - 1;
            }
          }

          // now each type param will be recursively converted and stored in typeParams array
          string[,] typeParams = ProcessTypeParams(result[0], indexOfFirstSquareBracket, totalGenericParamsCount);
          if (typeParams == null)
          {
            return null;
          }

          // now we have to compose all of the extracted information together
          // to form the final representation of a type
          StringBuilder[] resultTypeNames = ConstructGenericTypeName(genericType, typeParams);

          // and the last but not least: we have to append the array string (if it was present)
          resultTypeNames[0].Append(ReverseArrayString(arrayStr));
          resultTypeNames[1].Append(arrayStr.Replace("[,", "[0:,").Replace(",", ",0:"));

          return new[] { resultTypeNames[0].ToString(), resultTypeNames[1].ToString() };
        }
      }
      catch (Exception)
      {
        // something went wrong, so the given string must've been malformed

        return null;
      }
    }

    /// <summary>
    /// Converts each type param on the given list (it assumes that the list begins at the index beginIndex in typeParamsStr
    /// and that there are exactly typeParamsCount type parameters).
    /// </summary>
    /// <param name="typeParamsStr">
    /// String containing the list of type params (eg. "System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]").
    /// </param>
    /// <param name="beginIndex">
    /// Index in the typeParamsStr which indicates where the list of type params begins.
    /// Eg. for the example given in the comment for typeParamsStr parameter beginIndex will be equal to 34
    /// (index of the first opening square bracket).
    /// </param>
    /// <param name="typeParamsCount">Expected number of type params on the list.</param>
    /// <returns>
    /// Array of converted type params or null if it couldn't be done.
    /// </returns>
    private static string[,] ProcessTypeParams(string typeParamsStr, int beginIndex, int typeParamsCount)
    {
      string[,] result = new string[typeParamsCount, 2];

      // in the following loop we scan the list of type params and extract the strings
      // corresponding to them
      // it's done by counting opening and closing square brackets because there can be types of
      // arbitrary depth
      int index = beginIndex + 1;
      for (int i = 0; i < typeParamsCount; i++)
      {
        int openingBracketIndex = -1;

        // search for the first opening square bracket
        while (index < typeParamsStr.Length && typeParamsStr[index] != '[')
        {
          index++;
        }

        // if we reach the end of a string now, it means it must be malformed
        if (index >= typeParamsStr.Length)
        {
          return null;
        }

        // this will be the index of opening square bracket
        openingBracketIndex = index;

        // skip this openeing bracket because we'll be searching for
        // the matching closing square bracket
        index++;

        // search for the matching closing square bracket
        int closingBracketIndex = -1;
        int currentDepth = 1;
        while (index < typeParamsStr.Length)
        {
          if (typeParamsStr[index] == '[')
          {
            // every opening square bracket increases depth (nesting level) of square brackets
            currentDepth++;
          }
          else if (typeParamsStr[index] == ']')
          {
            // every closing square bracket decreases depth (nesting level) of square brackets
            currentDepth--;

            if (currentDepth == 0)
            {
              // if we reach 0 we now we've found the matching closing square bracket
              break;
            }
          }

          index++;
        }

        // if we reach the end of a string now, it means it must be malformed
        if (index >= typeParamsStr.Length)
        {
          return null;
        }

        // this will be the index of closing square bracket
        closingBracketIndex = index;

        // extract the part of a string corresponding to the i-th type param ...
        string paramTypeName = typeParamsStr.Substring(openingBracketIndex + 1, closingBracketIndex - openingBracketIndex - 1);

        // ... and parse it
        string[] paramTypeNames = GetHumanReadableFormsAux(paramTypeName);
        if (paramTypeNames[0] == null || paramTypeNames[1] == null)
        {
          // couldn't parse the param type
          return null;
        }

        // we've successfully converted the i-th type param
        result[i, 0] = paramTypeNames[0];
        result[i, 1] = paramTypeNames[1];

        index++;
      }

      return result;
    }

    /// <summary>
    /// Composes information extracted from the type params list and the type itself to form the final
    /// representation of a type (without array string).
    /// </summary>
    /// <param name="genericType">
    /// Type part of a generic type declaration.
    /// Eg. Imm.SomeGenericClass`2+SomeInnerClass`1+Enumerator[[System.Int32, mscorlib ...], [System.String, ...], [System.Int16, ...]]
    ///  -> Imm.SomeGenericClass`2+SomeInnerClass`1+Enumerator
    /// </param>
    /// <param name="typeParams">Converted type params of this generic type.</param>
    /// <returns>
    /// StringBuilder containing (almost) final representation of the type (without the trailing array string).
    /// </returns>
    private static StringBuilder[] ConstructGenericTypeName(string genericType, string[,] typeParams)
    {
      StringBuilder[] result = new StringBuilder[] { new StringBuilder(), new StringBuilder() };

      // the following loop scans through the type part of a generic type (without type parameters)
      // and inserts the types params converted in the previous step in appropriate places
      int currentParamIndex = 0;

      for (int i = 0; i < genericType.Length; i++)
      {
        char ch = genericType[i];

        if (ch == '`')
        {
          // we've found the place where we should insert converted type params
          // so the first thing to do is to count the number of type parameters
          // we have to insert here
          int newIndex;
          int paramsCount = ParseNumber(genericType, i + 1, out newIndex);

          // we'll continue scanning the string immediately after the read number
          i = newIndex - 1;

          // construct and append the list of type params
          result[0].Append('<');
          result[1].Append('<');
          for (int j = 0; j < paramsCount; j++)
          {
            result[0].Append(typeParams[currentParamIndex, 0]);
            result[1].Append(typeParams[currentParamIndex, 1]);
            currentParamIndex++;

            if (j < paramsCount - 1)
            {
              result[0].Append(", ");
              result[1].Append(", ");
            }
          }
          result[0].Append('>');
          result[1].Append('>');
        }
        else if (ch == '/')
        {
          // '/' means that we're dealing with an inner class
          result[0].Append('/');
          result[1].Append('/');
        }
        else
        {
          // otherwise we just copy character
          result[0].Append(ch);
          result[1].Append(ch);
        }
      }

      return result;
    }

    /// <summary>
    /// Reads specified string from given position until non-digit character is encountered.
    /// Read sequence of digits is treated as a single number and returned.
    /// The index immediately after the read sequence will be stored in the newIndex out parameter.
    /// </summary>
    /// <param name="text">Text to be read.</param>
    /// <param name="i">Index which the reading should begin from.</param>
    /// <param name="newIndex">Index immediately after the read sequence of digits.</param>
    /// <returns>
    /// Number read from this string (can be 0) if there are no digits from the specified index.
    /// </returns>
    private static int ParseNumber(string text, int i, out int newIndex)
    {
      int result = 0;

      while (i < text.Length && Char.IsDigit(text[i]))
      {
        result = 10 * result + (text[i] - '0');
        i++;
      }

      newIndex = i;

      return result;
    }

    /// <summary>
    /// Reverses CLR's internal representation of arrays.
    /// It's necessary because CLR's internal format of arrays is different than
    /// that used in source code. See the example.
    /// </summary>
    /// <example>
    /// Reverses strings like "[][,,,][][][]" to "[][][][,,,][]".
    /// </example>
    /// <param name="arrayStr"></param>
    /// <returns>
    /// Representation of an array in a format used in source code rather that by CLR.
    /// </returns>
    private static string ReverseArrayString(string arrayStr)
    {
      StringBuilder sb = new StringBuilder();

      for (int i = arrayStr.Length - 1; i >= 0; i--)
      {
        char ch = arrayStr[i];
        if (ch == '[')
        {
          sb.Append(']');
        }
        else if (ch == ']')
        {
          sb.Append('[');
        }
        else
        {
          sb.Append(ch);
        }
      }

      return sb.ToString();
    }

    private static string ConvertToSystemReflectionFormat(string typeFullName)
    {
      bool containsCompilerGenerated = false;
      Match match = CompilerGeneratedStuffRegex.Match(typeFullName);

      if (match.Success)
      {
        if (match.Index > 0)
        {
          typeFullName = CompilerGeneratedStuffRegex.Replace(typeFullName, "/___#${Id}#___");
        }
        else
        {
          typeFullName = CompilerGeneratedStuffRegex.Replace(typeFullName, "___#${Id}#___");
        }

        containsCompilerGenerated = true;
      }

      string result = typeFullName.Replace("<", "[[").Replace(">", "]]").Replace(",", "],[");

      result = result.Replace("[[]]", "<>");

      if (containsCompilerGenerated)
      {
        result = RevCompilerGeneratedStuffRegex.Replace(result, "<${Id}>");
      }

      return result;
    }

    #endregion

    #region Generics helpers

    public static List<string> ExamineGenericParameters(Collection<GenericParameter> genericParameters, TypeReference declaringType, out List<MyGenericParameterInfo> myGenericParameters)
    {
      return ExamineGenericParameters(genericParameters, declaringType, out myGenericParameters, false);
    }

    public static List<string> ExamineGenericParameters(Collection<GenericParameter> genericParameters, TypeReference declaringType, out List<MyGenericParameterInfo> myGenericParameters, bool returnAllGenericParametersNames)
    {
      Debug.Assert(genericParameters != null && genericParameters.Count > 0, "Impossible!");

      myGenericParameters = new List<MyGenericParameterInfo>();

      int startIndex = declaringType != null ? declaringType.GenericParameters.Count : 0;

      for (int i = startIndex; i < genericParameters.Count; i++)
      {
        myGenericParameters.Add(new MyGenericParameterInfo(genericParameters[i]));
      }

      if (returnAllGenericParametersNames)
      {
        List<string> allGenericParametersNames = null;

        if (genericParameters.Count > 0)
        {
          allGenericParametersNames = new List<string>();

          for (int i = 0; i < genericParameters.Count; i++)
          {
            allGenericParametersNames.Add(genericParameters[i].Name);
          }
        }

        return allGenericParametersNames;
      }

      return null;
    }

    public static string CreateFormalGenericParametersString(List<MyGenericParameterInfo> genericParameters)
    {
      if (genericParameters == null || genericParameters.Count == 0) { return ""; }

      StringBuilder sb = new StringBuilder("<");

      for (int i = 0; i < genericParameters.Count; i++)
      {
        if (i > 0) { sb.Append(", "); }

        sb.Append(genericParameters[i].Name);
      }

      sb.Append('>');

      return sb.ToString();
    }

    #endregion
  }
}
