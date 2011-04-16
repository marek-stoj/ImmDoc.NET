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

namespace Imm.ImmDocNetLib
{
    public static class Logger
    {
        private static List<string> warnings;
        private static List<string> errors;

        static Logger()
        {
            warnings = new List<string>();
            errors = new List<string>();
        }

        public static void Warning(string message)
        {
            warnings.Add(message);
        }

        public static void Warning(string message, params Object[] args)
        {
                Warning(String.Format(message, args));
        }

        public static void Error(string message)
        {
            errors.Add(message);
        }

        public static void Error(string message, params Object[] args)
        {
            Error(String.Format(message, args));
        }

        public static void WriteWarnings(TextWriter textWriter)
        {
            foreach (string message in warnings)
            {
                textWriter.WriteLine("Warning: {0}", message);
            }
        }

        public static void WriteWarnings()
        {
            WriteWarnings(Console.Out);
        }

        public static void WriteErrors(TextWriter textWriter)
        {
            foreach (string message in errors)
            {
                textWriter.WriteLine("Error: {0}", message);
            }
        }

        public static void WriteErrors()
        {
            WriteErrors(Console.Error);
        }

        public static void ClearWarnings()
        {
            warnings.Clear();
        }

        public static void ClearErrors()
        {
            errors.Clear();
        }

        public static int WarningsCount
        {
            get { return warnings.Count; }
        }

        public static int ErrorsCount
        {
            get { return errors.Count; }
        }
    }
}
