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
using Mono.Cecil;

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
  class MyConstructorInfo : MyInvokableMemberInfo
  {
    #region Constructor(s)

    public MyConstructorInfo(MethodDefinition constructorDefinition, MyClassInfo declaringType)
      : base(constructorDefinition, declaringType)
    {
      this.name = declaringType.Name;

      this.CheckSupport(constructorDefinition.Attributes);

      AddParameters(constructorDefinition.Parameters);
    }

    #endregion

    #region Implementation of abstract methods

    protected override void DumpShallow(TextWriter textWriter, string prefix)
    {
#if DEBUG
      textWriter.Write("{0}{1} {2}(", prefix, AttributesString, name);
#endif
    }

    #endregion

    #region ISummarisable members overrides

    public override string DisplayableName
    {
      get
      {
        string result = Utils.GetUnqualifiedName(name.Replace('/', '.'));

        // remove <T1, T2, ... Tn> suffix (if present)
        int index = result.LastIndexOf('<');

        if (index != -1)
        {
          return result.Substring(0, index);
        }
        else
        {
          return result;
        }
      }
    }

    #endregion

    #region MetaClass overrides

    public override string GetMetaName()
    {
      return "Constructor";
    }

    #endregion
  }
}
