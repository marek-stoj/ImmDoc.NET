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

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
  public abstract class MetaClass
  {
    protected string name;
    private string summary = String.Empty;
    private string remarks = String.Empty;
    protected MyClassInfo declaringType;

    #region Public properties

    public string Name
    {
      get { return name; }
    }

    /// <summary>
    /// NOTE: This property is meaningfull (ie. not null) only in MyNestedTypeInfo instances.
    /// </summary>
    public virtual MyClassInfo DeclaringType
    {
      get { return declaringType; }
    }

    public virtual string Summary
    {
      get { return summary; }
      set { summary = value; }
    }

    public virtual string Remarks
    {
      get { return remarks; }
      set { remarks = value; }
    }

    #endregion

    #region Public abstract methods

    public abstract string GetMetaName();

    #endregion

    #region Object overrides

    public override string ToString()
    {
      return name;
    }

    #endregion
  }
}
