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
using Mono.Cecil;
using System.Diagnostics;

using Imm.ImmDocNetLib.Documenters;

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
    class MyEnumerationInfo : MyClassInfo
    {
        private string underlyingTypeFullName;
        private string underlyingTypeFullNameWithoutRevArrayStrings;

        #region Constructor(s)

        public MyEnumerationInfo(TypeDefinition typeDefinition, string assemblyName)
        {
          Debug.Assert(typeDefinition.IsEnum, "Impossible! Given type is not an enumeration type.");

            this.assemblyName = assemblyName;

            this.Initialize(typeDefinition);
            this.AddMembers(typeDefinition);
            this.CheckSupport(typeDefinition);
        }

        #endregion

        #region Protected helper methods

        protected override void AddConstructor(MethodDefinition constructorDefinition)
        {
            Debug.Assert(false, "Enumerations can't contain constructors.");
        }

        protected override void AddNestedType(TypeDefinition typeDefinition)
        {
            Debug.Assert(false, "Enumerations can't contain nested types.");
        }

        protected override void AddProperty(PropertyDefinition propertyDefinition)
        {
            Debug.Assert(false, "Enumerations can't contain properties.");
        }

        protected override void AddEvent(EventDefinition eventInfo)
        {
            Debug.Assert(false, "Enumerations can't contain events.");
        }

        protected override void AddMethod(MethodDefinition methodDefinition)
        {
            // do nothing: we don't want any methods in enumerations
        }

        protected override void AddField(FieldDefinition fieldDefinition)
        {
            if (fieldDefinition.Name == "value__")
            {
                // skip the built-in field but read its typeDefinition

                string[] readableForms = Tools.GetHumanReadableForms(fieldDefinition.FieldType);
                underlyingTypeFullName = readableForms[0];
                underlyingTypeFullNameWithoutRevArrayStrings = readableForms[1];

                return;
            }

            base.AddField(fieldDefinition);
        }

        #endregion

        #region Public properties

        public override string AttributesString
        {
            get { return base.AttributesString.Replace("sealed ", "").Replace("sealed", "").TrimEnd(); }
        }

        public string UnderlyingTypeFullName
        {
            get { return underlyingTypeFullName; }
        }
        

        #endregion

        #region MetaClass overrides

        public override string GetMetaName()
        {
            return "Enumeration";
        }

        #endregion
    }
}
