//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//
//
// Use of this source code is subject to the terms of the Microsoft
// premium shared source license agreement under which you licensed
// this source code. If you did not accept the terms of the license
// agreement, you are not authorized to use this source code.
// For the terms of the license, please see the license agreement
// signed by you and Microsoft.
// THE SOURCE CODE IS PROVIDED "AS IS", WITH NO WARRANTIES OR INDEMNITIES.
//
using System;
using System.Collections.Generic;
using System.Text;

namespace ScidCruncher
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DocumentationAttribute : System.Attribute
    {
        public DocumentationAttribute(string documentation)
        {
            this.Documentation = documentation;
        }

        public string Documentation { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AbbreviationAttribute : System.Attribute
    {
        public AbbreviationAttribute(string abbreviation)
        {
            this.Abbreviation = abbreviation;
        }

        public string Abbreviation { get; private set; }
    }
}
