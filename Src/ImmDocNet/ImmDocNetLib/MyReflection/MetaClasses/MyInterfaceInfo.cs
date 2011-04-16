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
using Mono.Cecil;

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
    class MyInterfaceInfo : MyClassInfo
    {
        #region Constructor(s)

        public MyInterfaceInfo(TypeDefinition typeDefinition, string assemblyName)
        {
          Debug.Assert(typeDefinition.IsInterface, "Impossible! Given type is not an interface type.");

            this.assemblyName = assemblyName;

            this.Initialize(typeDefinition);
            this.AddMembers(typeDefinition);
            this.CheckSupport(typeDefinition);
        }

        #endregion

        #region Protected helper methods

        protected override void AddConstructor(MethodDefinition constructorDefinition)
        {
            Debug.Assert(false, "Interfaces can't contain constructors.");
        }

        protected override void  AddField(FieldDefinition fieldDefinition)
        {
            Debug.Assert(false, "Interfaces can't contain fields.");
        }

        protected override void AddNestedType(TypeDefinition typeDefinition)
        {
            Debug.Assert(false, "Interfaces can't contain nested types.");
        }

        #endregion

        #region Public properties

        public override string AttributesString
        {
            get { return base.AttributesString.Replace("abstract ", "").Replace("abstract", "").TrimEnd(); }
        }

        #endregion

        #region MetaClass overrides

        public override string  GetMetaName()
        {
 	        return "Interface";
        }

        #endregion
    }
}
