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
using System.Resources;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Diagnostics;
using Mono.Cecil;

using Imm.ImmDocNetLib.MyReflection.MetaClasses;
using Imm.ImmDocNetLib.Documenters;

namespace Imm.ImmDocNetLib
{
    public class AssembliesInfo
    {
        public static string CSS_FILE_NAME = "Styles.css";

        private string projectName;
        private Dictionary<string, MyAssemblyInfo> assemblies;

        private string projectSummary;
        
        #region Constructor(s)

        public AssembliesInfo(string projectName, string projectSummary)
        {
            this.projectName = projectName;
            this.projectSummary = projectSummary;
        }

        public AssembliesInfo(string projectName)
            : this(projectName, String.Empty)
        {
        }

        #endregion

        #region Populating documentation with assemblies

        public void ReadMyAssemblyInfoFromAssembly(string assemblyPath, IEnumerable<string> excludedNamespaces)
        {
            try
            {
                string assemblyAbsolutePath = Path.GetFullPath(assemblyPath);
                string assemblyName = AssemblyDefinition.ReadAssembly(assemblyAbsolutePath).Name.Name;

                MyAssemblyInfo myAssemblyInfo;

                if (assemblies == null)
                {
                    assemblies = new Dictionary<string, MyAssemblyInfo>();
                }

                if (!assemblies.ContainsKey(assemblyName))
                {
                    myAssemblyInfo = new MyAssemblyInfo(assemblyName, this);
                    assemblies.Add(assemblyName, myAssemblyInfo);
                }
                else
                {
                    myAssemblyInfo = assemblies[assemblyName];
                }

                myAssemblyInfo.ReadAssembly(assemblyAbsolutePath, excludedNamespaces);
            }
            catch (Exception exc)
            {
              #if DEBUG

                Logger.Warning("Couldn't process assembly '{0}'.\n{1}\n{2}", assemblyPath, exc.Message, Utils.SimplifyStackTrace(exc.StackTrace));

              #else

                        Logger.Warning("Couldn't process assembly '{0}' ({1}).", assemblyPath, exc.Message);

              #endif
            }
        }

        public void ReadMyAssemblyInfoFromXmlDocumentation(string xmlDocPath)
        {
            string xmlDocAbsolutePath = Path.GetFullPath(xmlDocPath);
            string assemblyName = Utils.GetAssemblyNameFromXmlDocumentation(xmlDocAbsolutePath);
            
            if (assemblyName == null)
            {
                Logger.Warning("Wrong format of XML documentation file: " + xmlDocPath);

                return;
            }

            if (assemblies == null)
            {
                assemblies = new Dictionary<string, MyAssemblyInfo>();
            }

            if (assemblies.ContainsKey(assemblyName))
            {
                MyAssemblyInfo myAssemblyInfo = assemblies[assemblyName];

                myAssemblyInfo.ReadXmlDocumentation(xmlDocAbsolutePath);
            }
            else
            {
                Logger.Warning("(XML) Couldn't find assembly '{0}'.", assemblyName);
            }
        }

        public void ReadAdditionalDocumentation(string xmlDocPath)
        {
            string xmlDocAbsolutePath = Path.GetFullPath(xmlDocPath);
            XmlDocument xmlDoc = new XmlDocument();

            try
            {
                xmlDoc.Load(xmlDocAbsolutePath);
            }
            catch (XmlException)
            {
                Logger.Warning("Couldn't load file with additional documentation: '{0}'.", xmlDocAbsolutePath);
                return;
            }

            XmlNodeList assemblyNodes = xmlDoc.GetElementsByTagName("assembly");
            foreach (XmlNode assemblyNode in assemblyNodes)
            {
                XmlAttribute assemblyNameAttribute = assemblyNode.Attributes["name"];
                if (assemblyNameAttribute == null)
                {
                    Logger.Warning("Assembly element in additional documentation file doesn't contain required 'name' attribute.");
                    continue;
                }
                string assemblyName = assemblyNameAttribute.Value;

                string assemblySummary = "";
                XmlNodeList assemblySummaryNodes = assemblyNode.SelectNodes("summary");
                foreach (XmlNode assemblySummaryNode in assemblySummaryNodes)
                {
                    assemblySummary += assemblySummaryNode.InnerXml;
                }

                MyAssemblyInfo myAssemblyInfo = FindAssembly(assemblyName);
                if (myAssemblyInfo == null)
                {
                    Logger.Warning("Assembly named '{0}' couldn't be found.", assemblyName);
                    continue;
                }

                myAssemblyInfo.Summary = assemblySummary;

                XmlNodeList namespaceNodes = assemblyNode.SelectNodes("namespace");
                foreach (XmlNode namespaceNode in namespaceNodes)
                {
                    XmlAttribute namespaceNameAttribute = namespaceNode.Attributes["name"];
                    if (namespaceNameAttribute == null)
                    {
                        Logger.Warning("Namespace element in additional documentation file doesn't contain required 'name' attribute.");
                        continue;
                    }
                    string namespaceName = namespaceNameAttribute.Value;

                    string namespaceSummary = "";
                    XmlNodeList namespaceSummaryNodes = namespaceNode.SelectNodes("summary");
                    foreach (XmlNode namespaceSummaryNode in namespaceSummaryNodes)
                    {
                        namespaceSummary += namespaceSummaryNode.InnerXml;
                    }

                    MyNamespaceInfo myNamespaceInfo = myAssemblyInfo.FindNamespace(namespaceName);
                    if (myNamespaceInfo == null)
                    {
                        Logger.Warning("Namespace named '{0}' couldn't be found in assembly '{1}' (or has been omitted due to lack of exported types).", namespaceName, assemblyName);
                        continue;
                    }

                    myNamespaceInfo.Summary = namespaceSummary;
                }
            }
        }

        #endregion

        #region Internal methods

        internal MyAssemblyInfo FindAssembly(string assemblyName)
        {
            if (AssembliesCount == 0 || !assemblies.ContainsKey(assemblyName))
            {
                return null;
            }

            return assemblies[assemblyName];
        }

        internal MyNamespaceInfo FindNamespace(string assemblyName, string namezpace)
        {
            MyAssemblyInfo myAssemblyInfo = FindAssembly(assemblyName);

            if (myAssemblyInfo == null)
            {
                return null;
            }

            return myAssemblyInfo.FindNamespace(namezpace);
        }

        internal MyClassInfo FindNamespaceMember(string assemblyName, string namezpace, NamespaceMembersGroups namespaceMemberGroupType, string memberName)
        {
            MyNamespaceInfo myNamespaceInfo = FindNamespace(assemblyName, namezpace);

            if (myNamespaceInfo == null)
            {
                return null;
            }

            return myNamespaceInfo.FindMember(namespaceMemberGroupType, memberName);
        }

        internal List<MyNamespaceInfo> FindNamespaces(string namezpace)
        {
            List<MyNamespaceInfo> namespaces = new List<MyNamespaceInfo>();

            foreach (MyAssemblyInfo myAssemblyInfo in assemblies.Values)
            {
                MyNamespaceInfo myNamespaceInfo = myAssemblyInfo.FindNamespace(namezpace);

                if (myNamespaceInfo != null)
                {
                    namespaces.Add(myNamespaceInfo);
                }
            }

            return namespaces;
        }

        internal MetaClass FindMember(string xmlMemberId)
        {
            char memberType = xmlMemberId[0];
            
            xmlMemberId = xmlMemberId.Substring(2);

            switch (memberType)
            {
                case 'T':
                {
                    foreach (MyAssemblyInfo myAssemblyInfo in assemblies.Values)
                    {
                        MyClassInfo myClassInfo = myAssemblyInfo.FindNamespaceMember(xmlMemberId);

                        if (myClassInfo != null)
                        {
                            return myClassInfo;
                        }
                    }

                    break;
                }

                case 'M':
                {
                    foreach (MyAssemblyInfo myAssemblyInfo in assemblies.Values)
                    {
                        MyInvokableMemberInfo myInvokableMemberInfo = myAssemblyInfo.FindMethodOrConstructor(xmlMemberId, true);

                        if (myInvokableMemberInfo != null)
                        {
                            return myInvokableMemberInfo;
                        }
                    }

                    break;
                }

                case 'F':
                {
                    foreach (MyAssemblyInfo myAssemblyInfo in assemblies.Values)
                    {
                        MyFieldInfo myFieldInfo = myAssemblyInfo.FindField(xmlMemberId, true);

                        if (myFieldInfo != null)
                        {
                            return myFieldInfo;
                        }
                    }

                    break;
                }

                case 'P':
                {
                    foreach (MyAssemblyInfo myAssemblyInfo in assemblies.Values)
                    {
                        MyPropertyInfo myPropertyInfo = myAssemblyInfo.FindProperty(xmlMemberId, true);

                        if (myPropertyInfo != null)
                        {
                            return myPropertyInfo;
                        }
                    }

                    break;
                }

                case 'E':
                {
                    foreach (MyAssemblyInfo myAssemblyInfo in assemblies.Values)
                    {
                        MyEventInfo myEventInfo = myAssemblyInfo.FindEvent(xmlMemberId, true);

                        if (myEventInfo != null)
                        {
                            return myEventInfo;
                        }
                    }

                    break;
                }

                case 'N':
                {
                    foreach (MyAssemblyInfo myAssemblyInfo in assemblies.Values)
                    {
                        MyNamespaceInfo myNamespaceInfo = myAssemblyInfo.FindNamespace(xmlMemberId);

                        if (myNamespaceInfo != null)
                        {
                            return myNamespaceInfo;
                        }
                    }

                    break;
                }
            }

            return null;
        }

        #endregion

        #region Public properties

        public string ProjectName
        {
            get { return projectName; }
            set { projectName = value; }
        }

        public string ProjectSummary
        {
            get { return projectSummary; }
        }

        public int AssembliesCount
        {
            get { return assemblies == null ? 0 : assemblies.Count; }
        }

        #endregion

        #region Internal properties

        internal Dictionary<string, MyAssemblyInfo>.ValueCollection Assemblies
        {
            get
            {
                if (assemblies == null)
                {
                    return null;
                }

                return assemblies.Values;
            }
        }

        #endregion

        #region Enumeration

        internal IEnumerator<ISummarisableMember> GetEnumerator()
        {
            List<string> sortedKeys = new List<string>();

            if (assemblies != null)
            {
                sortedKeys.AddRange(assemblies.Keys);
            }

            sortedKeys.Sort();

            foreach (string key in sortedKeys)
            {
                yield return (ISummarisableMember)assemblies[key];
            }
        }

        #endregion
    }
}
