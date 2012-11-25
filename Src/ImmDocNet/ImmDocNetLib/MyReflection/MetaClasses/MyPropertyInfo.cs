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
using System.IO;
using System.Diagnostics;
using Imm.ImmDocNetLib.MyReflection.Attributes;
using Imm.ImmDocNetLib.Documenters;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
    class MyPropertyInfo : MetaClass, ISummarisableMember
    {
        private string typeFullName;
        private string typeFullNameWithoutRevArrayStrings;
        private MyPropertyAttributes attributes;
        private MyInvokableMemberAttributes underlyingMethodsAttributes;
        private bool hasGetter;
        private bool hasSetter;
        private List<string> parametersNames;
        private Dictionary<string, MyParameterInfo> parameters;
        private string returnValueSummary = String.Empty;
        private List<ExceptionDescr> exceptionsDescrs;
        private string example = String.Empty;
        private int indexInOverloadsList = -1;

        #region Constructor(s)

        public MyPropertyInfo(PropertyDefinition propertyDefinition, MyClassInfo declaringType)
            : base()
        {
            this.name = propertyDefinition.Name;

            string[] readableForms = Tools.GetHumanReadableForms(propertyDefinition.PropertyType);

            this.typeFullName = readableForms[0];
            this.typeFullNameWithoutRevArrayStrings = readableForms[1];
            this.declaringType = declaringType;

            MethodDefinition getterInfo = propertyDefinition.GetMethod;
            MethodDefinition setterInfo = propertyDefinition.SetMethod;

            this.hasGetter = getterInfo != null;
            this.hasSetter = setterInfo != null;

            MethodDefinition getterOrSetterInfo = getterInfo != null ? getterInfo : setterInfo;
            Debug.Assert(getterOrSetterInfo != null, "Impossible! Property must have either getter or setter or both.");

            this.attributes = GetMyPropertyAttributes(propertyDefinition);
            this.underlyingMethodsAttributes = GetMyInvokableMemberAttributes(getterOrSetterInfo);

            this.parametersNames = new List<string>();
            this.parameters = new Dictionary<string, MyParameterInfo>();

            this.exceptionsDescrs = new List<ExceptionDescr>();

            AddParameters(getterInfo, setterInfo);

            this.CheckSupport(propertyDefinition.Attributes, getterOrSetterInfo.Attributes);
        }

        #endregion

        #region Private helper methods

        private void CheckSupport(PropertyAttributes propertyAttributes, MethodAttributes methodAttributes)
        {
            string warningTemplate = "Property '" + name + "' has unsupported attribute: '{0}'.";

            // in order to reduce output we warn only about important attributes which are not currently
            // supported:

            // PropertyAttributes
            // TODO: support this: if ((propertyAttributes & PropertyAttributes.HasDefault) != 0) { Logger.Warning(warningTemplate, "HasDefault"); }
            //if ((propertyAttributes & PropertyAttributes.RTSpecialName) != 0) { Logger.Warning(warningTemplate, "RTSpecialName"); }
            //if ((propertyAttributes & PropertyAttributes.SpecialName) != 0) { Logger.Warning(warningTemplate, "SpecialName"); }

            // MethodAttributes
            //if ((methodAttributes & MethodAttributes.CheckAccessOnOverride) != 0) { Logger.Warning(warningTemplate, "CheckAccessOnOverride"); }
            //if ((methodAttributes & MethodAttributes.FamANDAssem) != 0) { Logger.Warning(warningTemplate, "FamANDAssem"); }
            // TODO: support this: if ((methodAttributes & MethodAttributes.HasSecurity) != 0) { Logger.Warning(warningTemplate, "HasSecurity"); }
            //if ((methodAttributes & MethodAttributes.HideBySig) != 0) { Logger.Warning(warningTemplate, "HideBySig"); }
            //if ((methodAttributes & MethodAttributes.NewSlot) != 0) { Logger.Warning(warningTemplate, "NewSlot"); }
            // TODO: support this: if ((methodAttributes & MethodAttributes.PinvokeImpl) != 0) { Logger.Warning(warningTemplate, "PinvokeImpl"); }
            //if ((methodAttributes & MethodAttributes.PrivateScope) != 0) { Logger.Warning(warningTemplate, "PrivateScope"); }
            // TODO: support this: if ((methodAttributes & MethodAttributes.RequireSecObject) != 0) { Logger.Warning(warningTemplate, "RequiresSecObject"); }
            //if ((methodAttributes & MethodAttributes.ReuseSlot) != 0) { Logger.Warning(warningTemplate, "ReuseSlot"); }
            //if ((methodAttributes & MethodAttributes.RTSpecialName) != 0) { Logger.Warning(warningTemplate, "RTSpecialName"); }
            //if ((methodAttributes & MethodAttributes.SpecialName) != 0) { Logger.Warning(warningTemplate, "SpecialName"); }
            // TODO: support this: if ((methodAttributes & MethodAttributes.UnmanagedExport) != 0) { Logger.Warning(warningTemplate, "UnmanagedExport"); }
        }

        private static MyPropertyAttributes GetMyPropertyAttributes(PropertyDefinition propertyDefinition)
        {
            MyPropertyAttributes myPropertyAttributes = MyPropertyAttributes.None;

            return myPropertyAttributes;
        }

        private static MyInvokableMemberAttributes GetMyInvokableMemberAttributes(MethodDefinition methodDefinition)
        {
            return MyInvokableMemberInfo.GetMyInvokableMemberAttributes(methodDefinition);
        }

        private static string MyPropertyAndMyInvokableMemberAttributesToString(MyPropertyAttributes myPropertyAttributes, MyInvokableMemberAttributes myInvokableMemberAttributes)
        {
            // for now only MyInvokableMemberInfo attributes
            return MyInvokableMemberInfo.MyInvokableMemberAttributesToString(myInvokableMemberAttributes);
        }

        private void AddParameters(MethodDefinition getterInfo, MethodDefinition setterInfo)
        {
            Collection<ParameterDefinition> propParameters = null;

            if (getterInfo != null)
            {
                propParameters = getterInfo.Parameters;
            }
            else if (setterInfo != null)
            {
                var tmpParameters = setterInfo.Parameters;

                Debug.Assert(tmpParameters != null && tmpParameters.Count >= 1, "Impossible! Property setter must have at least one parameter.");

                propParameters = new Collection<ParameterDefinition>();

                for (int i = 0; i < tmpParameters.Count - 1; i++)
                {
                    propParameters.Add(tmpParameters[i]);
                }
            }
            else
            {
                Debug.Assert(false, "Impossible! Property must have either getter or setter or both.");
            }

            foreach (ParameterDefinition parameterDefinition in propParameters)
            {
                if (parameters.ContainsKey(parameterDefinition.Name))
                {
                    Logger.Warning("Properties can't have more than one parameter with the same name.");
                    return;
                }

                parametersNames.Add(parameterDefinition.Name);
                parameters.Add(parameterDefinition.Name, new MyParameterInfo(parameterDefinition));
            }
        }

        #endregion

        #region Public properties

        public string TypeFullName
        {
            get { return typeFullName; }
        }

        public string AttributesString
        {
            get { return MyPropertyAndMyInvokableMemberAttributesToString(attributes, underlyingMethodsAttributes); }
        }

        public List<string> ParametersNames
        {
            get { return parametersNames; }
        }

        public Dictionary<string, MyParameterInfo> Parameters
        {
            get { return parameters; }
        }

        public bool HasGetter
        {
            get { return hasGetter; }
        }

        public bool HasSetter
        {
            get { return hasSetter; }
        }

        public bool IsPublic
        {
            get { return (underlyingMethodsAttributes & MyInvokableMemberAttributes.Public) != 0; }
        }

        public bool IsProtected
        {
            get { return (underlyingMethodsAttributes & MyInvokableMemberAttributes.Protected) != 0; }
        }

        public bool IsInternal
        {
            get { return (underlyingMethodsAttributes & MyInvokableMemberAttributes.Internal) != 0; }
        }

        public bool IsPrivate
        {
            get { return (underlyingMethodsAttributes & MyInvokableMemberAttributes.Private) != 0; }
        }

        public bool IsProtectedInternal
        {
            get { return IsProtected && IsInternal; }
        }

        public bool IsStatic
        {
            get { return (underlyingMethodsAttributes & MyInvokableMemberAttributes.Static) != 0; }
        }

        public bool IsAbstract
        {
            get { return (underlyingMethodsAttributes & MyInvokableMemberAttributes.Abstract) != 0; }
        }

        public bool IsVirtual
        {
            get { return (underlyingMethodsAttributes & MyInvokableMemberAttributes.Virtual) != 0; }
        }

        public string ReturnValueSummary
        {
            get { return returnValueSummary; }
            set { returnValueSummary = value; }
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

        public string DisplayableName
        {
            get { return name; }
        }

        #endregion

        #region MetaClass overrides

        public override string GetMetaName()
        {
            return "Property";
        }

        #endregion
    }
}
