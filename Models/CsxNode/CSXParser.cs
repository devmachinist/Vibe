using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Text.Json;
using System.Diagnostics;

namespace Vibe
{
    public class CSXParser
    {
        public string Name => "CSXTagRule";
        public StringBuilder CodeBuilder {get;set;} = new StringBuilder();
        public int BraceCount {get;set;} = 0;
        public CSXParser()
        {

        }

        /// <summary>
        /// Parses raw html text into CsxNodes for Vibe.
        /// </summary>
        /// <param name="html">An string of raw html</param>
        /// <returns>A string, a ICsxNode, or an IEnumerable<dynamic></returns>
        public IEnumerable<object> Parse(string html)
        {
            var rootNodes = ParseHtml(html);
            return ConvertNodesToCode(rootNodes);
        }

        private abstract class CsxNode { }

        private class ElementNode : CsxNode
        {
            public string TagName { get; set; }
            public List<AttributeNode> Attributes { get; set; } = new List<AttributeNode>();
            public List<CsxNode> Children { get; set; } = new List<CsxNode>();
            public bool SelfClosing { get; set; }
        }

        private class AttributeNode
        {
            public string Name { get; set; }
            public bool IsCode { get; set; }
            public string Value { get; set; }
        }

        private class TextNode : CsxNode
        {
            public string Text { get; set; }
        }
        private class CodeNode : CsxNode
        {
            public string Code { get; set; }
        }

        #region Parsing

        private List<CsxNode> ParseHtml(string code)
        {
            int index = 0;
            return ParseNodes(code, ref index, null);
        }

        private List<CsxNode> ParseNodes(string input, ref int index, string stopOnTag)
        {
            var nodes = new List<CsxNode>();
            while (index < input.Length)
            {
                SkipWhitespace(input, ref index);
                if (index >= input.Length)
                    break;

                if (stopOnTag != null && input.Substring(index).StartsWith("</" + stopOnTag))
                {
                    break;
                }

                if (input[index] == '<' && input[index + 1] != '=')
                {
                    if (index + 1 < input.Length && input[index + 1] == '/')
                    {
                        break;
                    }

                    var elem = ParseElement(input, ref index);
                    nodes.Add(elem);
                }
                else
                {
                    var text = ParseText(input, ref index, stopOnTag);
                    if (!string.IsNullOrEmpty(text))
                    {
                        nodes.Add(new TextNode { Text = text });
                    }
                }
            }
            return nodes;
        }

        private ElementNode ParseElement(string input, ref int index)
        {
            int counter = 0 + index;
            if (input[index] != '<') return null;
            index++;

            string tagName = ParseTagName(input, ref index);
            
            var attributes = ParseAttributes(input, ref index);
            SkipWhitespace(input, ref index);

            bool selfClosing = false;
            if (index < input.Length && input[index] == '/')
            {
                selfClosing = true;
                index++;
            }

            if (index < input.Length
                 && input[index] == '>')
            {
                index++;
            }

            var element = new ElementNode
            {
                TagName = tagName,
                Attributes = attributes,
                SelfClosing = selfClosing
            };

            if (!selfClosing)
            {
                element.Children = ParseNodes(input, ref index, tagName);

                SkipWhitespace(input, ref index);
                if (index < input.Length &&  input[index] == '<')
                {
                    index += 2;
                    string closingTag = ParseTagName(input, ref index);
                    SkipWhitespace(input, ref index);
                    if (index < input.Length && input[index] == '>')
                        index++;
                }
            }
            return element;
        }
        /// <summary>
        /// Checks if the provided tagName corresponds to a type in the currently loaded assemblies.
        /// </summary>
        /// <param name="tagName">The type name to check.</param>
        /// <returns>True if the tagName is a valid type; otherwise, false.</returns>
        public bool CheckTagName(string tagName)
        {   
            var check = DoesTypeExist(tagName);
            if(check == true){
            }
            return check
                    || tagName.Contains('=')
                    || tagName.Contains(',')
                    || tagName.Contains('<')
                    || tagName.Contains('>')
                    || tagName.Contains("/>");
        }
        public static List<string> CantLoad = new List<string>();
        public static bool DoesTypeExist(string typeName)
        {
            if(typeName is null){
                // Cache for loaded assemblies and types
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

                // Directly look for types in the loaded assemblies
                foreach (var assembly in loadedAssemblies)
                {
                        // Check if any type matches the name in the current assembly
                        var type = assembly.GetType(typeName);
                        bool check = false;
                        if (type != null)
                        {
                            return true; // Type found
                        }
                        foreach(var a in assembly.GetReferencedAssemblies().ToList()){
                            try
                            {
                                if(CantLoad.Contains(a.Name)){
                                    continue;
                                }

                                var atype = Assembly.Load(a).GetType(typeName);
                                if (atype != null)
                                {
                                    check = true; // Type found
                                }
                            }
                            catch (ReflectionTypeLoadException ex)
                            {
                                CantLoad.Add(a.Name);
                            }
                        }
                        return check;
                }
            }
            // Type not found in loaded assemblies
            return false;
        }

        private string ParseTagName(string input, ref int index)
        {
            SkipWhitespace(input, ref index);
            int start = index;
            while (index < input.Length){
                if(char.IsWhiteSpace(input[index])
                    || input[index] == '>' 
                    || input[index] == '/'
                    || input[index] == ','){
                        break;
                    }
                
                index++;
            }
            return input.Substring(start, index - start);
        }
        private string ParseTagName(string input,int index)
        {
            SkipWhitespace(input, ref index);
            int start = index;
            while (index < input.Length)
            {
                if(char.IsWhiteSpace(input[index])
                    || input[index] == '>' 
                    || input[index] == '/'
                    || input[index] == ','){
                        break;
                    }
                
                index++;
            }
            return input.Substring(start, index - start);
        }

        private List<AttributeNode> ParseAttributes(string input, ref int index)
        {
            var attributes = new List<AttributeNode>();
            while (index < input.Length)
            {
                SkipWhitespace(input, ref index);
                if (index == input.Length || input[index] == '>' || input[index] == '/')
                    break;

                int start = index;
                while (index < input.Length 
                && !char.IsWhiteSpace(input[index]) 
                && input[index] != '='
                && input[index] != '>'
                )
                {
                    index++;
                }
                string attrName = input.Substring(start, index - start);
                SkipWhitespace(input, ref index);

                if (index < input.Length && input[index] == '=' )
                {
                    index++;
                    SkipWhitespace(input, ref index);
                     if (input[index] == '"' || input[index] == '\'')
                    {
                        char quote = input[index];
                        index++;
                        int valStart = index;
                        while (index < input.Length && input[index] != quote)
                        {
                            index++;
                        }
                        string attrValue = input.Substring(valStart, index - valStart);
                        if (index < input.Length) index++;
                        attributes.Add(new AttributeNode { Name = attrName, IsCode = false, Value = attrValue });
                    }
                }
            }
            return attributes;
        }

        private string ParseText(string input, ref int index, string stopOnTag)
        {
            int start = index;
            if(input[index] == '<') index++;
            int braceCount = 0;
            while (index < input.Length && stopOnTag != null)
            {

                if (input.Substring(index).StartsWith("</" + stopOnTag) || input[index] == '<' && braceCount == 0)
                {
                    break;
                }
                index++;
            }
            while (index < input.Length && stopOnTag == null)
            {

                if (index == input.Length || input[index] == '<' && braceCount == 0){
                    
                        break;
                }
                index++;
            }
            return input.Substring(start, index - start);
        }

        private void SkipWhitespace(string input, ref int index)
        {
            while (index < input.Length && char.IsWhiteSpace(input[index]))
                index++;
        }

        #endregion

        #region Code Generation
        private IEnumerable<object> ConvertNodesToCode(List<CsxNode> nodes)
        {
            return nodes.Select(ConvertNodeToCode);
        }

        private dynamic ConvertNodeToCode(CsxNode node)
        {
            switch (node)
            {
                case ElementNode elem:
                    return ConvertElementToCode(elem);
                case TextNode text:
                    return text.Text;
            }
            return string.Empty;
        }

        private ICsxNode ConvertElementToCode(ElementNode element)
        {
            ICsxNode node = new Vibe.CsxNode(element.TagName);
            var sb = new StringBuilder();
            var staticAttributes = new List<string>();

            var dynamicAttributes = new List<string>();

            foreach (var attr in element.Attributes)
            {
                    node.StageAtt(attr.Name, attr.Value);
            }

            void Comb(ElementNode ele){
                foreach (var child in ele.Children)
                {
                    var index = ele.Children.IndexOf(child);
                    
                    if (child is ElementNode childElement)
                    {
                        node.Append(ConvertElementToCode(childElement));
                    }
                    else if (child is TextNode textNode)
                    {
                        node.Append(textNode.Text);
                    }
                }
            }
            Comb(element);

            return node;
        }
        #endregion
    }
}
