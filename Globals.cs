using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ClearScript.V8;
using Microsoft.ClearScript;
using AngleSharp;
using AngleSharp.Dom;

namespace Vibe
{
    public static class XavierGlobal
    {
        public static IXavierMemory Memory { get; set; }
    }
}
