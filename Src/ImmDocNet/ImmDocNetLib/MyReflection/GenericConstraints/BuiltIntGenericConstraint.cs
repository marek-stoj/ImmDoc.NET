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
using System.Diagnostics;

namespace Imm.ImmDocNetLib.MyReflection.GenericConstraints
{
    enum BuiltInGenericConstraintsTypes
    {
        Class,
        Struct,
        New
    }

    class BuiltInGenericConstraint : GenericConstraint
    {
        private BuiltInGenericConstraintsTypes builtInGenericConstraintsTypes;

        #region Construcotr(s)

        public BuiltInGenericConstraint(BuiltInGenericConstraintsTypes builtInGenericConstraintsTypes)
        {
            this.builtInGenericConstraintsTypes = builtInGenericConstraintsTypes;
        }

        #endregion

        #region GenericConstraint overrides

        public override bool NeedsTypeProcessing
        {
            get { return false; }
        }

        public override string ToString()
        {
            switch (builtInGenericConstraintsTypes)
            {
                case BuiltInGenericConstraintsTypes.Class: { return "class"; }
                case BuiltInGenericConstraintsTypes.Struct: { return "struct"; }
                case BuiltInGenericConstraintsTypes.New: { return "new()"; }

                default:
                {
                    Debug.Assert(false, "Impossible! Unsupported built-in constraint.");

                    return "";
                }
            }
        }

        #endregion
    }
}
