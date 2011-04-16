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

namespace Imm.ImmDocNetLib.MyReflection.Attributes
{
    [Flags]
    enum MyInvokableMemberAttributes
    {
        None = 0,
        Abstract = 1,
        Internal = 2,
        Protected = 4,
        Sealed = 8,
        Private = 16,
        Public = 32,
        Static = 64,
        Virtual = 128,
        Extern = 256,
        Override = 512,
        New = 1024 // TODO: not used yet
    }
}
