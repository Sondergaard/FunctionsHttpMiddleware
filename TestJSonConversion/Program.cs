using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using BenchmarkDotNet.Running;
using Json.Net;
using Newtonsoft.Json;

namespace TestJSonConversion
{
    class Program
    {
        static async Task Main(string[] args)
        {
            
        }
    }

    abstract class CimWriter
    {
        
    }

    class CimXmlWriter : CimWriter
    {
        
    }

    class CimJsonWriter : CimWriter
    {
        
    }
}