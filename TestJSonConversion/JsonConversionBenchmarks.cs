// // Copyright 2020 Energinet DataHub A/S
// //
// // Licensed under the Apache License, Version 2.0 (the "License2");
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Json.Net;
using Newtonsoft.Json;

namespace TestJSonConversion
{
    [MemoryDiagnoser()]
    public class JsonConversionBenchmarks
    {
        private string _rawXml;
        private XmlDocument _document;
        private XmlReader _xmlReader;
        private XslCompiledTransform _compiledTransform;
        private XslCompiledTransform _compiledTransform2;
        private XslCompiledTransform _compiledTransform3;

        [GlobalSetup]
        public void Setup()
        {
            _rawXml = File.ReadAllText(@"data/cimxml.xml");
            _document = new XmlDocument();
            _document.LoadXml(_rawXml);
            _xmlReader = XmlReader.Create(@"data/cimxml.xml");
            _compiledTransform = new XslCompiledTransform();
            _compiledTransform.Load(@"data/stylesheet.xsl", XsltSettings.TrustedXslt, null);
            _compiledTransform2 = new XslCompiledTransform();
            _compiledTransform2.Load(@"data/stylesheet2.xsl");
            _compiledTransform3 = new XslCompiledTransform();
            _compiledTransform3.Load(@"data/stylesheet3.xsl");

        }

        [Benchmark(Baseline = true)]
        public string TestNewtonsoftJson()
        {
            return JsonConvert.SerializeXmlNode(_document, Newtonsoft.Json.Formatting.None, true);
        }

        [Benchmark]
        public string TestXslt()
        {
            using StringWriter writer = new StringWriter();
            _compiledTransform.Transform(_document, null,  writer);
            return writer.ToString();
        }

        // [Benchmark]
        // public string TestXslt2()
        // {
        //     using StringWriter writer = new StringWriter();
        //     _compiledTransform2.Transform(_document, null,  writer);
        //     return writer.ToString();
        // }
        //
        // [Benchmark]
        // public string TestXslt3()
        // {
        //     using StringWriter writer = new StringWriter();
        //     _compiledTransform3.Transform(_xmlReader, null,  writer);
        //     return writer.ToString();
        // }
        //
        // [Benchmark]
        // public string TestUtf8()
        // {
        //     return Utf8Json.JsonSerializer.ToJsonString(_xmlData);
        // }
        //
        [Benchmark]
        public string TestSystemTextJson()
        {
            var xmlData = GetXmlData(XElement.Parse(_rawXml));
           return System.Text.Json.JsonSerializer.Serialize(xmlData);
        }
        //
        // [Benchmark]
        // public string TestJsonNet()
        // {
        //    return JsonNet.Serialize(_xmlData);
        // }

        public static Dictionary<string, object> GetXmlData(XElement xml)
        {
            var attr = xml.Attributes().ToDictionary(d => d.Name.LocalName, d => (object)d.Value);
            if (xml.HasElements) attr.Add("_value", xml.Elements().Select(e => GetXmlData(e)));
            else if (!xml.IsEmpty) attr.Add("_value", xml.Value);

            return new Dictionary<string, object> { { xml.Name.LocalName, attr } };
        }
    }
}