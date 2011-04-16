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

namespace Imm.ImmDocNetLib.MyReflection.MetaClasses
{
    struct ExceptionDescr
    {
        private string typeFullName;
        private MyClassInfo exceptionClassInfo;
        private string condition;

        #region Constructor(s)

        public ExceptionDescr(MyClassInfo exceptionClassInfo, string condition)
        {
            if (exceptionClassInfo == null)
            {
                throw new ArgumentNullException("exceptionClassInfo");
            }

            if (condition == null)
            {
                throw new ArgumentNullException("condition");
            }

            if (exceptionClassInfo.Namespace == null || exceptionClassInfo.Namespace == "")
            {
                this.typeFullName = exceptionClassInfo.Name;
            }
            else
            {
                this.typeFullName = exceptionClassInfo.Namespace + "." + exceptionClassInfo.Name;
            }

            this.exceptionClassInfo = exceptionClassInfo;
            this.condition = condition;
        }

        public ExceptionDescr(string exceptionCref, string condition)
        {
            if (exceptionCref == null)
            {
                throw new ArgumentNullException("exceptionCref");
            }

            if (condition == null)
            {
                throw new ArgumentNullException("condition");
            }

            this.exceptionClassInfo = null;
            this.typeFullName = exceptionCref;
            this.condition = condition;
        }

        #endregion

        #region Public properties

        public string TypeFullName
        {
            get { return typeFullName; }
        }

        /// <summary>
        /// Can be null.
        /// </summary>
        public MyClassInfo ExceptionClassInfo
        {
            get { return exceptionClassInfo; }
        }

        public string Condition
        {
            get { return condition; }
        }

        #endregion
    }
}
