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
using System.Text;
using System.Diagnostics;
using Imm.ImmDocNetLib.MyReflection.Attributes;
using Imm.ImmDocNetLib.Documenters;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
    abstract class MyInvokableMemberInfo : MetaClass, ISummarisableMember
    {
        private MyInvokableMemberAttributes attributes;
        private List<string> parametersNames;
        private Dictionary<string, MyParameterInfo> parameters;
        private string summary;
        private List<ExceptionDescr> exceptionsDescrs;
        private string example = String.Empty;
        private int indexInOverloadsList = -1;

        #region Constructor(s)

        protected MyInvokableMemberInfo(MethodDefinition methodDefinition, MyClassInfo declaringType)
        {
            this.attributes = GetMyInvokableMemberAttributes(methodDefinition);
            this.declaringType = declaringType;

            this.summary = String.Empty;

            this.parametersNames = new List<string>();
            this.parameters = new Dictionary<string, MyParameterInfo>();

            this.exceptionsDescrs = new List<ExceptionDescr>();
        }

        #endregion

        #region Protected abstract methods

        [Conditional("DEBUG")]
        protected abstract void DumpShallow(TextWriter textWriter, string prefix);

        #endregion

        #region Protected helper methods

        protected void AddParameters(Collection<ParameterDefinition> tmpParameters)
        {
            foreach (ParameterDefinition parameterDefinition in tmpParameters)
            {
                if (parameters.ContainsKey(parameterDefinition.Name))
                {
                    Logger.Warning("Methods can't have more than one parameter with the same name.");
                    return;
                }

                parametersNames.Add(parameterDefinition.Name);
                parameters.Add(parameterDefinition.Name, new MyParameterInfo(parameterDefinition));
            }
        }

        protected void CheckSupport(MethodAttributes methodAttributes)
        {
            string warningTemplate = "Method '" + name + "' has unsupported attribute: '{0}'.";

            // in order to reduce output we warn only about important attributes which are not currently
            // supported:

            //if ((methodAttributes & MethodAttributes.CheckAccessOnOverride) != 0) { Logger.Warning(warningTemplate, "CheckAccessOnOverride"); }
            //if ((methodAttributes & MethodAttributes.FamANDAssem) != 0) { Logger.Warning(warningTemplate, "FamANDAssem"); }
            // TODO: support this: if ((methodAttributes & MethodAttributes.HasSecurity) != 0) { Logger.Warning(warningTemplate, "HasSecurity"); }
            //if ((methodAttributes & MethodAttributes.HideBySig) != 0) { Logger.Warning(warningTemplate, "HideBySig"); }
            //if ((methodAttributes & MethodAttributes.NewSlot) != 0) { Logger.Warning(warningTemplate, "NewSlot"); }
            //if ((methodAttributes & MethodAttributes.PinvokeImpl) != 0) { Logger.Warning(warningTemplate, "PinvokeImpl"); }
            //if ((methodAttributes & MethodAttributes.PrivateScope) != 0) { Logger.Warning(warningTemplate, "PrivateScope"); }
            // TODO: support this: if ((methodAttributes & MethodAttributes.RequireSecObject) != 0) { Logger.Warning(warningTemplate, "RequiresSecObject"); }
            //if ((methodAttributes & MethodAttributes.ReuseSlot) != 0) { Logger.Warning(warningTemplate, "ReuseSlot"); }
            //if ((methodAttributes & MethodAttributes.RTSpecialName) != 0) { Logger.Warning(warningTemplate, "RTSpecialName"); }
            //if ((methodAttributes & MethodAttributes.SpecialName) != 0) { Logger.Warning(warningTemplate, "SpecialName"); }
            // TODO: support this: if ((methodAttributes & MethodAttributes.UnmanagedExport) != 0) { Logger.Warning(warningTemplate, "UnmanagedExport"); }
        }

        #endregion

        #region Private helper methods

        /// <summary>
        /// Returns the string representing attributes contained in the given enumeration.
        /// </summary>
        /// <remarks>
        /// If the invokable member was marked virtual and sealed then this method will omit both of these attributes
        /// (beacuse it's not virtual anymore and the sealed modifier is then meaningless).
        /// </remarks>
        /// <param name="myInvokableMemberAttributes">Enumeration of MyInvokableMemberAttributes.</param>
        /// <returns>String representing attributes contained in the given enumeration.</returns>
        public static string MyInvokableMemberAttributesToString(MyInvokableMemberAttributes myInvokableMemberAttributes)
        {
            StringBuilder sb = new StringBuilder();

            if ((myInvokableMemberAttributes & MyInvokableMemberAttributes.Public) != 0) { sb.Append("public "); }
            if ((myInvokableMemberAttributes & MyInvokableMemberAttributes.Private) != 0) { sb.Append("private "); }

            if ((myInvokableMemberAttributes & MyInvokableMemberAttributes.Protected) != 0 && ((myInvokableMemberAttributes & MyInvokableMemberAttributes.Internal) != 0)) { sb.Append("protected internal "); }
            else if ((myInvokableMemberAttributes & MyInvokableMemberAttributes.Protected) != 0) { sb.Append("protected "); }
            else if ((myInvokableMemberAttributes & MyInvokableMemberAttributes.Internal) != 0) { sb.Append("internal "); }

            if ((myInvokableMemberAttributes & MyInvokableMemberAttributes.Static) != 0) { sb.Append("static "); }
            if ((myInvokableMemberAttributes & MyInvokableMemberAttributes.Extern) != 0) { sb.Append("extern "); }
            if ((myInvokableMemberAttributes & MyInvokableMemberAttributes.Abstract) != 0) { sb.Append("abstract "); }
            if ((myInvokableMemberAttributes & MyInvokableMemberAttributes.Virtual) != 0) { sb.Append("virtual "); }
            if ((myInvokableMemberAttributes & MyInvokableMemberAttributes.Override) != 0) { sb.Append("override "); }
            if ((myInvokableMemberAttributes & MyInvokableMemberAttributes.Sealed) != 0) { sb.Append("sealed "); }

            if (sb.Length > 0)
            {
                sb.Length = sb.Length - 1;
            }

            return sb.ToString();
        }

        public static MyInvokableMemberAttributes GetMyInvokableMemberAttributes(MethodDefinition methodDefinition)
        {
            MyInvokableMemberAttributes myInvokableMemberAttributes = MyInvokableMemberAttributes.None;

            if (methodDefinition.IsAbstract) { myInvokableMemberAttributes |= MyInvokableMemberAttributes.Abstract; }
            if (methodDefinition.IsAssembly) { myInvokableMemberAttributes |= MyInvokableMemberAttributes.Internal; }
            if (methodDefinition.IsFamily) { myInvokableMemberAttributes |= MyInvokableMemberAttributes.Protected; }
            if (methodDefinition.IsFamilyOrAssembly) { myInvokableMemberAttributes |= MyInvokableMemberAttributes.Protected | MyInvokableMemberAttributes.Internal; }
            if (methodDefinition.IsPrivate) { myInvokableMemberAttributes |= MyInvokableMemberAttributes.Private; }
            if (methodDefinition.IsPublic) { myInvokableMemberAttributes |= MyInvokableMemberAttributes.Public; }
            if (methodDefinition.IsStatic) { myInvokableMemberAttributes |= MyInvokableMemberAttributes.Static; }

            if ((methodDefinition.Attributes & MethodAttributes.PInvokeImpl) != 0)
            {
                myInvokableMemberAttributes |= MyInvokableMemberAttributes.Extern;
            }

            if ((methodDefinition.Attributes & MethodAttributes.NewSlot) == MethodAttributes.NewSlot)
            {
                Debug.Assert((methodDefinition.Attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual, "Impossible! If invokable member has NewSlot attribute then it has to have Virtual attribute also.");

                if ((methodDefinition.Attributes & MethodAttributes.Final) == 0 && (methodDefinition.Attributes & MethodAttributes.Abstract) == 0)
                {
                    myInvokableMemberAttributes |= MyInvokableMemberAttributes.Virtual;
                }
            }
            else
            {
                if ((methodDefinition.Attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual)
                {
                    myInvokableMemberAttributes |= MyInvokableMemberAttributes.Override;
                }

                if ((methodDefinition.Attributes & MethodAttributes.Final) == MethodAttributes.Final)
                {
                    myInvokableMemberAttributes |= MyInvokableMemberAttributes.Sealed;
                }
            }

            return myInvokableMemberAttributes;
        }

        #endregion

        #region Public properties

        public string AttributesString
        {
            get { return MyInvokableMemberAttributesToString(attributes); }
        }

        public List<string> ParametersNames
        {
            get { return parametersNames; }
        }

        public Dictionary<string, MyParameterInfo> Parameters
        {
            get { return parameters; }
        }

        public bool IsPublic
        {
            get { return (attributes & MyInvokableMemberAttributes.Public) != 0; }
        }

        public bool IsProtected
        {
            get { return (attributes & MyInvokableMemberAttributes.Protected) != 0; }
        }

        public bool IsInternal
        {
            get { return (attributes & MyInvokableMemberAttributes.Internal) != 0; }
        }

        public bool IsProtectedInternal
        {
            get { return IsProtected && IsInternal; }
        }

        public bool IsPrivate
        {
            get { return (attributes & MyInvokableMemberAttributes.Private) != 0; }
        }

        public bool IsStatic
        {
            get { return (attributes & MyInvokableMemberAttributes.Static) != 0; }
        }

        public bool IsVirtual
        {
            get { return (attributes & MyInvokableMemberAttributes.Virtual) != 0; }
        }

        public bool IsOverride
        {
            get { return (attributes & MyInvokableMemberAttributes.Override) != 0; }
        }

        public bool IsSealed
        {
            get { return (attributes & MyInvokableMemberAttributes.Sealed) != 0; }
        }
        
        public bool IsAbstract
        {
            get { return (attributes & MyInvokableMemberAttributes.Abstract) != 0; }
        }

        public List<ExceptionDescr> ExceptionsDescrs
        {
            get { return exceptionsDescrs; }
        }

        public string Example
        {
            get { return example; }
            set { example = value; }
        }

        public int IndexInOverloadsList
        {
            get { return indexInOverloadsList; }
            set { indexInOverloadsList = value; }
        }

        #endregion

        #region ISummarisableMember Members

        public virtual string DisplayableName
        {
            get { return name; }
        }

        #endregion
    }
}
