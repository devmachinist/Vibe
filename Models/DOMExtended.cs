using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Constellations;
using DotJS;

namespace Vibe
{
    public partial class CsxDocument
    {
        public ICsxNode GetElementById(string id)
        {
            return QuerySelector("[id='" + id + "']") ?? null;
        }
    }
}