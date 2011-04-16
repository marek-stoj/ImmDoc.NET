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

namespace Imm.ImmDocNetLib.Documenters
{
    public delegate void DocumenterEventHandler(object sender, EventArgs e);

    public abstract class Documenter
    {
        public event DocumenterEventHandler DirectoryDeleteStarted;
        public event DocumenterEventHandler DirectoryDeleteFinished;
        public event DocumenterEventHandler GeneratingStarted;
        public event DocumenterEventHandler GeneratingFinished;

        protected AssembliesInfo assembliesInfo;

        #region Constructor(s)

        public Documenter(AssembliesInfo assembliesInfo)
        {
            this.assembliesInfo = assembliesInfo;
        }

        #endregion

        #region Public methods

        public abstract bool GenerateDocumentation(string outputDirectory, DocumentationGenerationOptions options);

        public bool GenerateDocumentation(string outputDirectory)
        {
            return GenerateDocumentation(outputDirectory, DocumentationGenerationOptions.None);
        }

        #endregion

        #region Event raising methods

        protected void OnDirectoryDeleteStarted(EventArgs e)
        {
            if (DirectoryDeleteStarted != null)
            {
                DirectoryDeleteStarted(this, e);
            }
        }

        protected void OnDirectoryDeleteFinished(EventArgs e)
        {
            if (DirectoryDeleteFinished != null)
            {
                DirectoryDeleteFinished(this, e);
            }
        }

        protected void OnGeneratingStarted(EventArgs e)
        {
            if (GeneratingStarted != null)
            {
                GeneratingStarted(this, e);
            }
        }

        protected void OnGeneratingFinished(EventArgs e)
        {
            if (GeneratingFinished != null)
            {
                GeneratingFinished(this, e);
            }
        }

        #endregion
    }
}
