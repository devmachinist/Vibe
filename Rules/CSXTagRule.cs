using Amazon.S3.Model;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using AngleSharp.Html.Dom.Events;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using AngleSharp.Dom;

namespace Vibe.Rules
{
    public class CSXTagRule : ISyntaxRule
    {
        public string Name => "CSXTagRule";
        public StringBuilder CodeBuilder {get;set;} = new StringBuilder();
        public int BraceCount {get;set;} = 0;

        public string Apply(string code)
        {
            var rootNodes = ParseJsx(code);
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

        private List<CsxNode> ParseJsx(string code)
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
                    if (elem != null) nodes.Add(elem);
                    else {
                        var peek = index;
                        nodes.Add(new CodeNode { Code = ParseText(input, ref index, stopOnTag) });
                    }
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

        private ElementNode? ParseElement(string input, ref int index)
        {
            int counter = 0 + index;
            if (input[index] != '<') return null;
            index++;

            string tagName = ParseTagName(input, ref index);
            
            var attributes = ParseAttributes(input, ref index);
            if (CheckTagName(tagName))
            {
                index = counter;
                return null;
            }
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
                var peekIndex = 1 + index;
                for (int i = 0; i < input.Length - index + 1; i++)
                {
                    if (input[peekIndex] == ')' 
                    || input[peekIndex] == '('
                    
                    )
                    {
                        index = counter;
                        return null;
                    }

                    if (char.IsWhiteSpace(input[peekIndex]))
                    {
                        peekIndex++;
                        continue;
                    }
                    if (!char.IsWhiteSpace(input[peekIndex]))
                    {
                        break;
                    }
                }
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
        static bool ContainsCasts(string code)
        {
            // Parse the code into a syntax tree
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            // Look for cast expressions
            var castExpressions = root.DescendantNodes().OfType<CastExpressionSyntax>().ToList();

            // Return true if any cast expressions are found
            return castExpressions.Any();
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
        public static bool DoesTypeExist(string? typeName)
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
                    if (input[index] == '{')
                    {
                        index++;
                        int braceCount = 1;
                        int codeStart = index;
                        while (index < input.Length && braceCount > 0)
                        {
                            if (input[index] == '{') braceCount++;
                            if (input[index] == '}') braceCount--;
                            index++;
                        }
                        string codeValue = input.Substring(codeStart, index - codeStart - 1);
                        attributes.Add(new AttributeNode { Name = attrName, IsCode = true, Value = codeValue.Trim() });
                    }
                    else if (input[index] == '"' || input[index] == '\'')
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

        private string ParseText(string input, ref int index, string? stopOnTag)
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
        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
        };
        private string ConvertNodesToCode(List<CsxNode> nodes)
        {
            if (nodes.Count == 1 && nodes[0] is ElementNode e)
            {
                return ConvertElementToCode(e);
            }

            return string.Join("", nodes.Select(ConvertNodeToCode));
        }

        private string ConvertNodeToCode(CsxNode node)
        {
            if (node is ElementNode elem) return ConvertElementToCode(elem);
            if (node is TextNode text) return text.Text;
            if (node is CodeNode code) return code.Code;
            return string.Empty;
        }

        private string ConvertElementToCode(ElementNode element)
        {
            var sb = new StringBuilder();
            var staticAttributes = new List<string>();
            if (char.IsUpper(element.TagName[0]))
            {
                var atts = new List<string>();
                element.Attributes.ForEach((a) =>
                {
                    atts.Add($"{a.Name}={(a.IsCode ? a.Value : "\""+a.Value+"\"")}");
                });

                sb.Append($"{element.TagName}({(atts.Count() > 0? $"new {{ {string.Join(",", atts)} }}":"" )})");
            }
            else
            {
            }

            var dynamicAttributes = new List<string>();
            var NewObject = new List<string>();
            bool CheckKeyWord(string name){
                return CSharpKeywords.Contains(name);
            }

            foreach (var attr in element.Attributes)
            {
                if (attr.IsCode)
                {
                    dynamicAttributes.Add($"SetAttribute(\"{attr.Name}\", {Apply(attr.Value)})");
                    NewObject.Add(@$"{(CheckKeyWord(attr.Name)? @"X_" + attr.Name: attr.Name).Replace("-","_").Replace("@","X_")} = "+Apply(attr.Value));
                }
                else
                {
                    staticAttributes.Add($"SetAttribute(\"{attr.Name}\", $@\"{attr.Value}\")");
                    NewObject.Add($"{(CheckKeyWord(attr.Name)? @"X_" + attr.Name: attr.Name).Replace("-","_").Replace("@","X_")} = $@\"{attr.Value}\"");
                }
            }

            string attrInit = string.Join(",", NewObject);

            if (char.IsUpper(element.TagName[0]))
            {

            }
            else
            {
                sb.Append($"new CsxNode(\"{element.TagName}\", new {{ {attrInit} }})" );
            }
            foreach (var dynamicAttr in dynamicAttributes)
            {
                sb.Append($".{dynamicAttr}");
            }
            foreach (var staticAttr in staticAttributes)
            {
                sb.Append($".{staticAttr}");
            }
            CsxNode? lastNode = null;

            void Comb(ElementNode element){
                foreach (var child in element.Children)
                {
                    var index = element.Children.IndexOf(child);
                    CsxNode? next = element.Children.Count() >= index + 2? element.Children[index + 1]: null;
                    
                    if (child is ElementNode childElement)
                    {
                        sb.Append($".Append({ConvertElementToCode(childElement)}");
                        sb.Append(")");
                    }
                    else if (child is TextNode textNode)
                    {
                        if(lastNode is CodeNode || next is CodeNode){
                            CodeBuilder.Append(textNode.Text);
                            lastNode = child;
                            continue;
                        }
                        if(next is ElementNode && lastNode is ElementNode 
                            || lastNode is TextNode && next is not CodeNode
                            || element.Children.Count() == 1
                            || lastNode is ElementNode && child == element.Children.Last()
                            ){
                            var fragments = ParseTextFragments(textNode.Text);
                            foreach (var fragment in fragments)
                            {
                                sb.Append(fragment.isCode
                                    ? $".Append({fragment.value})"
                                    : $".Append(@\"{EscapeString(fragment.value)}\")");
                            }
                            lastNode = child;
                            continue;
                        }
                        if(next is ElementNode && lastNode is CodeNode
                            || child == element.Children.Last() && lastNode is CodeNode){
                            CodeBuilder.Append(textNode.Text);
                            var fragments = ParseTextFragments(CodeBuilder.ToString());
                            CodeBuilder = new StringBuilder();
                            foreach (var fragment in fragments)
                            {
                                sb.Append(fragment.isCode
                                    ? $".Append({fragment.value})"
                                    : $".Append(@\"{EscapeString(fragment.value)}\")");
                            }
                            lastNode = child;
                            continue;
                        }
                    }
                    else if (child is CodeNode code){
                        CodeBuilder.Append(code.Code);
                        if(next is ElementNode){
                            var fragments = ParseTextFragments(CodeBuilder.ToString());
                            CodeBuilder = new StringBuilder();
                            foreach (var fragment in fragments)
                            {
                                sb.Append(fragment.isCode
                                    ? $".Append({fragment.value})"
                                    : $".Append(@\"{EscapeString(fragment.value)}\")");
                            }
                            lastNode = child;
                            continue;
                        }
                        if(child == element.Children.Last()){
                            var fragments = ParseTextFragments(CodeBuilder.ToString());
                            CodeBuilder = new StringBuilder();
                            foreach (var fragment in fragments)
                            {
                                sb.Append(fragment.isCode
                                    ? $".Append({fragment.value})"
                                    : $".Append(@\"{EscapeString(fragment.value)}\")");
                            }
                            continue;
                        }
                    }
                    lastNode = child;
                }
            }
            Comb(element);

            return sb.ToString();
        }

        private List<(bool isCode, string value)> ParseTextFragments(string text)
        {
            var Tokens = new XavierTokens();
            var fragments = new List<(bool, string)>();
            int index = 0;
            bool IsCode = false;
            while (index < text.Length)
            {
                if (text[index] == '{' )
                {
                    int start = index;
                    index++;
                    int codeStart = index;
                    int braceCount = 1;
                    
                    while (index < text.Length && braceCount > 0)
                    {
                        if (text[index] == '{')
                        {
                            braceCount++;
                        }
                        else if (text[index] == '}')
                        {
                            braceCount--;
                        }

                        if(braceCount == 0){
                            index ++;
                            break;
                        }
                        index++;
                    }
                    string codeExpr = text.Substring(codeStart, index - codeStart - 1);
                    fragments.Add((true, codeExpr));
                }
                else
                {
                    while (index < text.Length && text[index] != '{')
                    {
                        index++;
                    }
                }
            }
            return fragments;
        }

        private string EscapeString(string input)
        {
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        #endregion
    }
}
