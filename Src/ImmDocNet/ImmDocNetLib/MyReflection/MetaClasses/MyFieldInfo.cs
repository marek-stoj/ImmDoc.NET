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
    internal class MyFieldInfo : MetaClass, ISummarisableMember
    {
        private string typeFullName;
        private string typeFullNameWithoutRevArrayStrings;
        private MyFieldAttributes attributes;
        private string defaultValue;
        private string example = String.Empty;

        #region Constructor(s)

        /// <summary>
        /// Thiashd iahsd haishd iashd ihasih iqwhei hqie hwqih eoqwh eoqwihe oashd   
        /// oasihd o oiahsd oihasd oihawoidh aowihd oaihd oiahsoid had asd .as.d as. d  
        /// as.d a sd as.
        /// </summary>
        /// <param name="fieldDefinition"></param>
        /// <param name="declaringType"></param>
        public MyFieldInfo(FieldDefinition fieldDefinition, MyClassInfo declaringType)
            : base()
        {
            this.name = fieldDefinition.Name;

            string[] readableForms = Tools.GetHumanReadableForms(fieldDefinition.FieldType);

            this.typeFullName = readableForms[0];
            this.typeFullNameWithoutRevArrayStrings = readableForms[1];
            this.attributes = GetMyFieldAttributes(fieldDefinition);
            this.declaringType = declaringType;

            if ((fieldDefinition.Attributes & FieldAttributes.HasDefault) != 0)
            {
                try
                {
                    object rawConstant = fieldDefinition.Constant;

                    defaultValue = rawConstant == null ? null : rawConstant.ToString();
                }
                catch (Exception)
                {
                    Logger.Warning("Couldn't obtain default value for field '{0}'.", name);
                }
            }

            this.CheckSupport(fieldDefinition.Attributes);
        }

        #endregion

        #region Public properties

        public string TypeFullName
        {
            get { return typeFullName; }
        }

        public string AttributesString
        {
            get { return MyFieldAttributesToString(attributes); }
        }

        public string ConstantValue
        {
            get { return defaultValue; }
        }

        public bool IsPublic
        {
            get { return (attributes & MyFieldAttributes.Public) != 0; }
        }

        public bool IsProtected
        {
            get { return (attributes & MyFieldAttributes.Protected) != 0; }
        }

        public bool IsInternal
        {
            get { return (attributes & MyFieldAttributes.Internal) != 0; }
        }

        public bool IsPrivate
        {
            get { return (attributes & MyFieldAttributes.Private) != 0; }
        }

        public bool IsProtectedInternal
        {
            get { return IsProtected && IsInternal; }
        }

        public bool IsConst
        {
            get { return (attributes & MyFieldAttributes.Const) != 0; }
        }

        public bool IsStatic
        {
            get { return (attributes & MyFieldAttributes.Static) != 0; }
        }

        public string Example
        {
            get { return example; }
            set { example = value; }
        }

        #endregion

        #region Private helper methods

        private void CheckSupport(FieldAttributes fieldAttributes)
        {
            string warningTemplate = "Field '" + name + "' has unsupported attribute: '{0}'.";

            // in order to reduce output we warn only about important attributes which are not currently
            // supported:

            //if ((fieldAttributes & FieldAttributes.FamANDAssem) != 0) { Logger.Warning(warningTemplate, "FamANDAssem"); }
            // TODO: support this: if ((fieldAttributes & FieldAttributes.HasFieldMarshal) != 0) { Logger.Warning(warningTemplate, "HasFieldMarshal"); }
            //if ((fieldAttributes & FieldAttributes.FamANDAssem) != 0) { Logger.Warning(warningTemplate, "FamANDAssem"); }
            //if ((fieldAttributes & FieldAttributes.HasFieldRVA) != 0) { Logger.Warning(warningTemplate, "HasFieldRVA"); }
            // TODO: support this: if ((fieldAttributes & FieldAttributes.NotSerialized) != 0) { Logger.Warning(warningTemplate, "NotSerialized"); }
            // TODO: support this: if ((fieldAttributes & FieldAttributes.PinvokeImpl) != 0) { Logger.Warning(warningTemplate, "PinvokeImpl"); }
            //if ((fieldAttributes & FieldAttributes.PrivateScope) != 0) { Logger.Warning(warningTemplate, "PrivateScope"); }
            //if ((fieldAttributes & FieldAttributes.RTSpecialName) != 0) { Logger.Warning(warningTemplate, "RTSpecialName"); }
            //if ((fieldAttributes & FieldAttributes.SpecialName) != 0) { Logger.Warning(warningTemplate, "SpecialName"); }
        }

        private static MyFieldAttributes GetMyFieldAttributes(FieldDefinition fieldDefinition)
        {
            MyFieldAttributes myFieldAttributes = MyFieldAttributes.None;

            if (fieldDefinition.IsPublic) { myFieldAttributes |= MyFieldAttributes.Public; }
            if (fieldDefinition.IsFamily) { myFieldAttributes |= MyFieldAttributes.Protected; }
            if (fieldDefinition.IsPrivate) { myFieldAttributes |= MyFieldAttributes.Private; }
            if (fieldDefinition.IsAssembly) { myFieldAttributes |= MyFieldAttributes.Internal; }
            if (fieldDefinition.IsFamilyOrAssembly) { myFieldAttributes |= MyFieldAttributes.Protected | MyFieldAttributes.Internal; }

            if (fieldDefinition.IsLiteral) { myFieldAttributes |= MyFieldAttributes.Const; }
            else if (fieldDefinition.IsStatic) { myFieldAttributes |= MyFieldAttributes.Static; }

            if (fieldDefinition.IsInitOnly) { myFieldAttributes |= MyFieldAttributes.ReadOnly; }

            return myFieldAttributes;
        }

        private static string MyFieldAttributesToString(MyFieldAttributes myFieldAttributes)
        {
            StringBuilder sb = new StringBuilder();

            if ((myFieldAttributes & MyFieldAttributes.Public) != 0) { sb.Append("public "); }
            if ((myFieldAttributes & MyFieldAttributes.Private) != 0) { sb.Append("private "); }
            
            if ((myFieldAttributes & MyFieldAttributes.Protected) != 0 && (myFieldAttributes & MyFieldAttributes.Internal) != 0) { sb.Append("protected internal "); }
            else if ((myFieldAttributes & MyFieldAttributes.Protected) != 0) { sb.Append("protected "); }
            else if ((myFieldAttributes & MyFieldAttributes.Internal) != 0) { sb.Append("internal "); }

            if ((myFieldAttributes & MyFieldAttributes.Const) != 0) { sb.Append("const "); }
            else if ((myFieldAttributes & MyFieldAttributes.Static) != 0) { sb.Append("static "); }

            if ((myFieldAttributes & MyFieldAttributes.ReadOnly) != 0) { sb.Append("readonly "); }

            if (sb.Length > 0)
            {
                sb.Length = sb.Length - 1;
            }

            return sb.ToString();
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
            return "Field";
        }

        #endregion
    }
}
