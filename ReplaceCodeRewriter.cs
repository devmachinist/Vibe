﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Text.RegularExpressions;
namespace Xavier
{
    public static partial class RW
    {
        // Class to evaluate the C# code
        public class ReplaceCodeRewriter : CSharpSyntaxRewriter
        {
            public object GUID { get; private set; }

            public string VisitXavier(SyntaxNode node, string name,Assembly assembly)
            {
                string codeBlock = node.ToString();

                // Find code block using regular expression

                codeBlock = "using System;" +
                    "using System.Collections.Generic;" +
                    "using System.Diagnostics;" +
                    "using System.Text;" +
                    "using System.Threading.Tasks;" +
                    "using System.Collections;" +
                    "using System.Collections.ObjectModel;" +
                    "using System.ComponentModel;" +
                    "using System.Text.Json;"+
                    "using System.Text.Json.Serialization;"+
                $"namespace {assembly.GetName().Name} {{" +
                    $" public partial class {name} : XavierNode{{";

                            codeBlock = codeBlock.Replace("}x", " } catch(Exception ex){" +
                                "Debug.WriteLine(ex);" +
                                "return ex.ToString();" +
                                "}" +
                                " return Return;} " +
                                "public string Main(string[] args){ " +
                                "Type s = Execute();" +
                                "return s;" +
                                " } " +
                                " } " +
                                " }");
                            // Evaluate the code block
                            if (codeBlock != null)
                            {
                                codeBlock = ExtractAtVariables(codeBlock);
                               // var thisnode = RunCSharpAssembly(xavier,codeBlock);
                                return "";
                            }
                            return "";
                        }
            }


            public static string Visit(object xavier, SyntaxNode node, Assembly assembly, Memory memory)
            {
            try
            {

                var runner = memory.XavierNodes.First(x => (x as XavierNode).Name == (xavier as XavierNode).Name);
                
                

                string codeBlock = node.ToFullString();


                // Find code block using regular expression
                var regex = new Regex(@"(x{)([\s\S]*)(}x)");

                if (node != null)
                {
                    var matches = regex.Matches(codeBlock);
                    if (matches.Count > 0)
                    {
                        var startIndex = 0;

                        foreach (var match in matches)
                        {
                            var theseProps = (runner as XavierNode).ExtractVariableList(runner, assembly);

                            codeBlock = match.ToString();


                            if (codeBlock.Contains("@foreach"))
                            {
                                codeBlock = ProcessForeachCode(codeBlock);
                            }
                            codeBlock = codeBlock.Replace("x{",
                                "using System;" +
                                "using System.Collections.Generic;" +
                                "using System.Diagnostics;" +
                                "using System.Text;" +
                                "using System.Threading.Tasks;" +
                                "using System.Collections;" +
                                "using System.Collections.ObjectModel;" +
                                "using System.ComponentModel;" +
                                $"using {assembly.GetName().Name};"+
                                $"namespace {assembly.GetName().Name} {{"+
                                $"public class {(xavier as XavierNode).Name}_X : {(xavier as XavierNode).Name} {{ {theseProps}"+
                                $" public string Execute(){{ " +
                                " try{" +
                                " ");
                            codeBlock = codeBlock.Replace("}x", " } catch(Exception ex){" +
                                "Debug.WriteLine(ex.Message);" +
                                "return ex.Message;" +
                                "}" +
                                " return \"\";} " +
                                $"public static string Exe(string[] args){{ {(xavier as XavierNode).Name}_X  xav = new {(xavier as XavierNode).Name}_X(); " +
                                "string s = xav.Execute();" +
                                "return s;" +
                                " } " +
                                " } } " )
                                ;
                            // Evaluate the code block
                            if (codeBlock != null)
                            {
                                codeBlock = ExtractAtVariables(codeBlock);
                                var thisnode = RunCSharpAssembly(xavier,codeBlock, assembly);
                                return thisnode;
                            }
                            return "";
                        }
                        return "";
                    }
                    return "";
                }
                return "";
            }
            catch (Exception Ex) { return Ex.Message; };
            }
        }
    }