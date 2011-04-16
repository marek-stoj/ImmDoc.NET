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
using System.Diagnostics;

using Imm.ImmDocNetLib.MyReflection.MetaClasses;

namespace Imm.ImmDocNetLib.Documenters.HTMLDocumenter
{
    [Flags]
    enum IconsTypes : long
    {
        None                            = 0x1,
        Assembly                        = 0x2,
        Namespace                       = 0x4,
        PublicClass                     = 0x8,
        PublicStructure                 = 0x10,
        PublicInterface                 = 0x20,
        PublicDelegate                  = 0x40,
        PublicEnumeration               = 0x80,
        PublicField                     = 0x100,
        PublicMethod                    = 0x200,
        PublicProperty                  = 0x400,
        PublicEvent                     = 0x800,
        ProtectedClass                  = 0x1000,
        ProtectedStructure              = 0x2000,
        ProtectedInterface              = 0x4000,
        ProtectedDelegate               = 0x8000,
        ProtectedEnumeration            = 0x10000,
        ProtectedField                  = 0x20000,
        ProtectedMethod                 = 0x40000,
        ProtectedProperty               = 0x80000,
        ProtectedEvent                  = 0x100000,
        InternalClass                   = 0x200000,
        InternalStructure               = 0x400000,
        InternalInterface               = 0x800000,
        InternalDelegate                = 0x1000000,
        InternalEnumeration             = 0x2000000,
        InternalField                   = 0x4000000,
        InternalMethod                  = 0x8000000,
        InternalProperty                = 0x10000000,
        InternalEvent                   = 0x20000000,
        PrivateClass                    = 0x40000000,
        PrivateStructure                = 0x80000000,
        PrivateInterface                = 0x100000000,
        PrivateDelegate                 = 0x200000000,
        PrivateEnumeration              = 0x400000000,
        PrivateField                    = 0x800000000,
        PrivateMethod                   = 0x1000000000,
        PrivateProperty                 = 0x2000000000,
        PrivateEvent                    = 0x4000000000,
        ProtectedInternalClass          = 0x8000000000,
        ProtectedInternalStructure      = 0x10000000000,
        ProtectedInternalInterface      = 0x20000000000,
        ProtectedInternalDelegate       = 0x40000000000,
        ProtectedInternalEnumeration    = 0x80000000000,
        ProtectedInternalField          = 0x100000000000,
        ProtectedInternalMethod         = 0x200000000000,
        ProtectedInternalProperty       = 0x400000000000,
        ProtectedInternalEvent          = 0x800000000000,
        EnumField                       = 0x1000000000000,
        Static                          = 0x2000000000000,
        Virtual                         = 0x4000000000000,
        Abstract                        = 0x8000000000000
    }

    static class Icons
    {
        public static string[] FILES_NAMES = new string[] { "None.gif",
                                                            "Assembly.gif",
                                                            "Namespace.gif",
                                                            "PublicClass.gif",
                                                            "PublicStructure.gif",
                                                            "PublicInterface.gif",
                                                            "PublicDelegate.gif",
                                                            "PublicEnumeration.gif",
                                                            "PublicField.gif",
                                                            "PublicMethod.gif",
                                                            "PublicProperty.gif",
                                                            "PublicEvent.gif",
                                                            "ProtectedClass.gif",
                                                            "ProtectedStructure.gif",
                                                            "ProtectedInterface.gif",
                                                            "ProtectedDelegate.gif",
                                                            "ProtectedEnumeration.gif",
                                                            "ProtectedField.gif",
                                                            "ProtectedMethod.gif",
                                                            "ProtectedProperty.gif",
                                                            "ProtectedEvent.gif",
                                                            "InternalClass.gif",
                                                            "InternalStructure.gif",
                                                            "InternalInterface.gif",
                                                            "InternalDelegate.gif",
                                                            "InternalEnumeration.gif",
                                                            "InternalField.gif",
                                                            "InternalMethod.gif",
                                                            "InternalProperty.gif",
                                                            "InternalEvent.gif",
                                                            "PrivateClass.gif",
                                                            "PrivateStructure.gif",
                                                            "PrivateInterface.gif",
                                                            "PrivateDelegate.gif",
                                                            "PrivateEnumeration.gif",
                                                            "PrivateField.gif",
                                                            "PrivateMethod.gif",
                                                            "PrivateProperty.gif",
                                                            "PrivateEvent.gif",
                                                            "ProtectedInternalClass.gif",
                                                            "ProtectedInternalStructure.gif",
                                                            "ProtectedInternalInterface.gif",
                                                            "ProtectedInternalDelegate.gif",
                                                            "ProtectedInternalEnumeration.gif",
                                                            "ProtectedInternalField.gif",
                                                            "ProtectedInternalMethod.gif",
                                                            "ProtectedInternalProperty.gif",
                                                            "ProtectedInternalEvent.gif",
                                                            "EnumField.gif",
                                                            "Static.gif",
                                                            "Virtual.gif",
                                                            "Abstract.gif" };

        private static string[] ALT_LABELS = new string[] { "None",
                                                            "Assembly",
                                                            "Namespace",
                                                            "Public Class",
                                                            "Public Structure",
                                                            "Public Interface",
                                                            "Public Delegate",
                                                            "Public Enumeration",
                                                            "Public Field",
                                                            "Public Method",
                                                            "Public Property",
                                                            "Public Event",
                                                            "Protected Class",
                                                            "Protected Structure",
                                                            "Protected Interface",
                                                            "Protected Delegate",
                                                            "Protected Enumeration",
                                                            "Protected Field",
                                                            "Protected Method",
                                                            "Protected Property",
                                                            "Protected Event",
                                                            "Internal Class",
                                                            "Internal Structure",
                                                            "Internal Interface",
                                                            "Internal Delegate",
                                                            "Internal Enumeration",
                                                            "Internal Field",
                                                            "Internal Method",
                                                            "Internal Property",
                                                            "Internal Event",
                                                            "Private Class",
                                                            "Private Structure",
                                                            "Private Interface",
                                                            "Private Delegate",
                                                            "Private Enumeration",
                                                            "Private Field",
                                                            "Private Method",
                                                            "Private Property",
                                                            "Private Event",
                                                            "Protected Internal Class",
                                                            "Protected Internal Structure",
                                                            "Protected Internal Interface",
                                                            "Protected Internal Delegate",
                                                            "Protected Internal Enumeration",
                                                            "Protected Internal Field",
                                                            "Protected Internal Method",
                                                            "Protected Internal Property",
                                                            "Protected Internal Event",
                                                            "Enum Field",
                                                            "Static",
                                                            "Virtual",
                                                            "Abstract" };

        static Icons()
        {

        }

        public static List<string> GetFileNames(IconsTypes iconType)
        {
            return GetIconStrings(iconType, FILES_NAMES);
        }

        public static List<string> GetAltLabels(IconsTypes iconType)
        {
            return GetIconStrings(iconType, ALT_LABELS);
        }

        private static List<string> GetIconStrings(IconsTypes iconType, string[] strings)
        {
            List<string> result = new List<string>();
            long pow2 = 1;
            Type iconsTypesType = typeof(IconsTypes);

            for (int i = 0; ; i++)
            {
                if (!Enum.IsDefined(iconsTypesType, pow2))
                {
                    break;
                }

                IconsTypes tmpIconType = (IconsTypes)pow2;

                if ((iconType & tmpIconType) != 0)
                {
                    result.Add(strings[i]);
                }

                pow2 <<= 1;
            }

            return result;
        }

        public static IconsTypes GetIconType(NamespaceMembersGroups namespaceMembersGroupType)
        {
            switch (namespaceMembersGroupType)
            {
                case NamespaceMembersGroups.PublicClasses: return IconsTypes.PublicClass;
                case NamespaceMembersGroups.PublicStructures: return IconsTypes.PublicStructure;
                case NamespaceMembersGroups.PublicInterfaces: return IconsTypes.PublicInterface;
                case NamespaceMembersGroups.PublicEnumerations: return IconsTypes.PublicEnumeration;
                case NamespaceMembersGroups.PublicDelegates: return IconsTypes.PublicDelegate;
                case NamespaceMembersGroups.ProtectedInternalClasses: return IconsTypes.ProtectedInternalClass;
                case NamespaceMembersGroups.ProtectedInternalStructures: return IconsTypes.ProtectedInternalStructure;
                case NamespaceMembersGroups.ProtectedInternalInterfaces: return IconsTypes.ProtectedInternalInterface;
                case NamespaceMembersGroups.ProtectedInternalEnumerations: return IconsTypes.ProtectedInternalEnumeration;
                case NamespaceMembersGroups.ProtectedInternalDelegates: return IconsTypes.ProtectedInternalDelegate;
                case NamespaceMembersGroups.ProtectedClasses: return IconsTypes.ProtectedClass;
                case NamespaceMembersGroups.ProtectedStructures: return IconsTypes.ProtectedStructure;
                case NamespaceMembersGroups.ProtectedInterfaces: return IconsTypes.ProtectedInterface;
                case NamespaceMembersGroups.ProtectedEnumerations: return IconsTypes.ProtectedEnumeration;
                case NamespaceMembersGroups.ProtectedDelegates: return IconsTypes.ProtectedDelegate;
                case NamespaceMembersGroups.InternalClasses: return IconsTypes.InternalClass;
                case NamespaceMembersGroups.InternalStructures: return IconsTypes.InternalStructure;
                case NamespaceMembersGroups.InternalInterfaces: return IconsTypes.InternalInterface;
                case NamespaceMembersGroups.InternalEnumerations: return IconsTypes.InternalEnumeration;
                case NamespaceMembersGroups.InternalDelegates: return IconsTypes.InternalDelegate;
                case NamespaceMembersGroups.PrivateClasses: return IconsTypes.PrivateClass;
                case NamespaceMembersGroups.PrivateStructures: return IconsTypes.PrivateStructure;
                case NamespaceMembersGroups.PrivateInterfaces: return IconsTypes.PrivateInterface;
                case NamespaceMembersGroups.PrivateEnumerations: return IconsTypes.PrivateEnumeration;
                case NamespaceMembersGroups.PrivateDelegates: return IconsTypes.PrivateDelegate;

                default:
                {
                    Debug.Assert(false, "Impossible! Couldn't return correct type of icon.");

                    return (IconsTypes)0;
                }
            }
        }

        public static IconsTypes GetIconType(ClassMembersGroups classMembersGroupType)
        {
            switch (classMembersGroupType)
            {
                case ClassMembersGroups.PublicClasses: return IconsTypes.PublicClass;
                case ClassMembersGroups.PublicConstructors: return IconsTypes.PublicMethod;
                case ClassMembersGroups.PublicDelegates: return IconsTypes.PublicDelegate;
                case ClassMembersGroups.PublicEnumerations: return IconsTypes.PublicEnumeration;
                case ClassMembersGroups.PublicEvents: return IconsTypes.PublicEvent;
                case ClassMembersGroups.PublicFields: return IconsTypes.PublicField;
                case ClassMembersGroups.PublicInterfaces: return IconsTypes.PublicInterface;
                case ClassMembersGroups.PublicMethodsOverloads: return IconsTypes.PublicMethod;
                case ClassMembersGroups.PublicPropertiesOverloads: return IconsTypes.PublicProperty;
                case ClassMembersGroups.PublicStructures: return IconsTypes.PublicStructure;
                case ClassMembersGroups.ProtectedClasses: return IconsTypes.ProtectedClass;
                case ClassMembersGroups.ProtectedConstructors: return IconsTypes.ProtectedMethod;
                case ClassMembersGroups.ProtectedDelegates: return IconsTypes.ProtectedDelegate;
                case ClassMembersGroups.ProtectedEnumerations: return IconsTypes.ProtectedEnumeration;
                case ClassMembersGroups.ProtectedEvents: return IconsTypes.ProtectedEvent;
                case ClassMembersGroups.ProtectedFields: return IconsTypes.ProtectedField;
                case ClassMembersGroups.ProtectedInterfaces: return IconsTypes.ProtectedInterface;
                case ClassMembersGroups.ProtectedMethodsOverloads: return IconsTypes.ProtectedMethod;
                case ClassMembersGroups.ProtectedPropertiesOverloads: return IconsTypes.ProtectedProperty;
                case ClassMembersGroups.ProtectedStructures: return IconsTypes.ProtectedStructure;
                case ClassMembersGroups.InternalClasses: return IconsTypes.InternalClass;
                case ClassMembersGroups.InternalConstructors: return IconsTypes.InternalMethod;
                case ClassMembersGroups.InternalDelegates: return IconsTypes.InternalDelegate;
                case ClassMembersGroups.InternalEnumerations: return IconsTypes.InternalEnumeration;
                case ClassMembersGroups.InternalEvents: return IconsTypes.InternalEvent;
                case ClassMembersGroups.InternalFields: return IconsTypes.InternalField;
                case ClassMembersGroups.InternalInterfaces: return IconsTypes.InternalInterface;
                case ClassMembersGroups.InternalMethodsOverloads: return IconsTypes.InternalMethod;
                case ClassMembersGroups.InternalPropertiesOverloads: return IconsTypes.InternalProperty;
                case ClassMembersGroups.InternalStructures: return IconsTypes.InternalStructure;
                case ClassMembersGroups.ProtectedInternalClasses: return IconsTypes.ProtectedInternalClass;
                case ClassMembersGroups.ProtectedInternalConstructors: return IconsTypes.ProtectedInternalMethod;
                case ClassMembersGroups.ProtectedInternalDelegates: return IconsTypes.ProtectedInternalDelegate;
                case ClassMembersGroups.ProtectedInternalEnumerations: return IconsTypes.ProtectedInternalEnumeration;
                case ClassMembersGroups.ProtectedInternalEvents: return IconsTypes.ProtectedInternalEvent;
                case ClassMembersGroups.ProtectedInternalFields: return IconsTypes.ProtectedInternalField;
                case ClassMembersGroups.ProtectedInternalInterfaces: return IconsTypes.ProtectedInternalInterface;
                case ClassMembersGroups.ProtectedInternalMethodsOverloads: return IconsTypes.ProtectedInternalMethod;
                case ClassMembersGroups.ProtectedInternalPropertiesOverloads: return IconsTypes.ProtectedInternalProperty;
                case ClassMembersGroups.ProtectedInternalStructures: return IconsTypes.ProtectedInternalStructure;
                case ClassMembersGroups.PrivateClasses: return IconsTypes.PrivateClass;
                case ClassMembersGroups.PrivateConstructors: return IconsTypes.PrivateMethod;
                case ClassMembersGroups.PrivateDelegates: return IconsTypes.PrivateDelegate;
                case ClassMembersGroups.PrivateEnumerations: return IconsTypes.PrivateEnumeration;
                case ClassMembersGroups.PrivateEvents: return IconsTypes.PrivateEvent;
                case ClassMembersGroups.PrivateFields: return IconsTypes.PrivateField;
                case ClassMembersGroups.PrivateInterfaces: return IconsTypes.PrivateInterface;
                case ClassMembersGroups.PrivateMethodsOverloads: return IconsTypes.PrivateMethod;
                case ClassMembersGroups.PrivatePropertiesOverloads: return IconsTypes.PrivateProperty;
                case ClassMembersGroups.PrivateStructures: return IconsTypes.PrivateStructure;

                default:
                {
                    Debug.Assert(false, "Impossible! Couldn't return correct type of icon.");

                    return (IconsTypes)0;
                }
            }
        }
    }
}
