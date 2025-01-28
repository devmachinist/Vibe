using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System.Buffers;

namespace Vibe
{
    public static partial class RW
    {
        // Add the nested CsxNode to the parent one.
        //Merge the two types and string of c# script
        public static SyntaxList<MemberDeclarationSyntax> MergeTypesAndScript(Type type1, Type type2, string cSharpScript)
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText(type1.ToString());
            var root1 = syntaxTree1.GetRoot();
            var typeDeclaration1 = root1.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>().Single();

            var syntaxTree2 = CSharpSyntaxTree.ParseText(type2.ToString());
            var root2 = syntaxTree2.GetRoot();
            var typeDeclaration2 = root2.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>().Single();

            var syntaxTree3 = CSharpSyntaxTree.ParseText(cSharpScript);
            var root3 = syntaxTree3.GetRoot();
            var syntaxNodes = root3.DescendantNodesAndSelf();

            var mergedMembers = SyntaxFactory.List<MemberDeclarationSyntax>();
            mergedMembers = mergedMembers.Add(typeDeclaration1);
            mergedMembers = mergedMembers.Add(typeDeclaration2);
            mergedMembers = mergedMembers.Add(syntaxNodes.OfType<TypeDeclarationSyntax>().Single());

            return mergedMembers;
        }
        public static string CreateNewXavierFromScript(string newClassName, Type inheritedType, string classcode)
        {
            string codeLines =
                $@"public class {newClassName} : {inheritedType.Name}
        {{
            {classcode}
        }}";

            // Create a CodeSnippet for the code
            return codeLines;
            // Compile the CodeSnippet into a class

            // Register the newly created class
        }
        public static string ClearSlashes(string s)
        {   
            s = s?.Replace("\\", "/");
            return s?? "''";
        }
        

        public static SyntaxNode VisitClassDeclaration(SyntaxNode node)
        {
            return node;
        }
        public static SyntaxNode CreateType(string typeName, string baseType = "CsxNode")
        {

            // Create a new class named TypeName
            ClassDeclarationSyntax classType = SyntaxFactory.ClassDeclaration(typeName)
                                     .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));

            // If a base type was provided, set it
            if (!string.IsNullOrEmpty(baseType))
            {
                classType = classType.WithBaseList(SyntaxFactory.BaseList(
                    SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                        SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(baseType)))));
            }

            // Return the modified class type
            return classType;
        }
        public static string ExtractNameWithNoExtension(string fileName)
            {
                int startIndex = 0;
                int endIndex = fileName.LastIndexOf('.');
                string nameWithNoExtension = fileName.Substring(startIndex, endIndex - startIndex);

                return nameWithNoExtension;
            }

        public static async Task StaticSiteTransformer(string StaticSiteRoot)
        {
            await Task.Run( () =>
            {

                string newdir = Environment.CurrentDirectory + "/" + StaticSiteRoot;
                if (!Directory.Exists(newdir))
                {
                    Directory.CreateDirectory(newdir);
                }
                DirectoryInfo di = new DirectoryInfo(Environment.CurrentDirectory);

                FileInfo[] fi = di.GetFiles("*.xavier", SearchOption.AllDirectories);
                FileInfo[] fiL = di.GetFiles("*.xavier.cs", SearchOption.AllDirectories);

                foreach (FileInfo fiTemp in fi)
                {
                   // await WriteFile(object xavier, File.ReadAllText(fiTemp.FullName), di.FullName + "/" + StaticSiteRoot + "/" + fiTemp.Name.Replace(".xavier", ".cx"));
                }
            });
        }
        public static string WriteVirtualFile(object Xavier, string input,Assembly assembly, IXavierMemory memory)
        {
            var source = (Xavier as CsxNode).ExtractAtLast(EvaluateCSharpCode(Xavier, EvaluateJavaScript(input), assembly, memory));
            source = source.Replace("-{{", "{{");
            source = source.Replace("}}-", "}}");
            
            bool check = true;
            
            if (check)
            {
                return source;
            }
            return source;
        }
        public static async Task<bool> WriteFile(object xavier, string input, string destination, Assembly assembly, Memory memory)
        {
            var source = EvaluateCSharpCode(xavier,EvaluateJavaScript(input), assembly, memory);

            bool check = File.Exists(destination);
            if (check)
            {
                if (File.ReadAllText(destination) != source)
                {
                    Console.WriteLine($"Xavier detected changes: Streaming to {destination}");
                    await File.WriteAllTextAsync(destination, source);
                }

                return true;
            }
            else
            {
                await File.WriteAllTextAsync(destination, source);
                return false;
            }
        }
        public static string? ExtractClassBody(string input)
        {
            int startIndex = input.IndexOf("@class{");
            if (startIndex == -1)
            {
                return null; // No @class{ found
            }

            startIndex += "@class{".Length;
            int currentIndex = startIndex;
            int braceCount = 1;
            StringBuilder result = new StringBuilder();
            bool inString = false;
            char stringDelimiter = '\0';
            bool inSingleLineComment = false;
            bool inMultiLineComment = false;

            while (currentIndex < input.Length)
            {
                char currentChar = input[currentIndex];
                char nextChar = currentIndex + 1 < input.Length ? input[currentIndex + 1] : '\0';

                // Handle single-line comments
                if (inSingleLineComment)
                {
                    if (currentChar == '\n')
                    {
                        inSingleLineComment = false;
                    }
                    result.Append(currentChar);
                }
                // Handle multi-line comments
                else if (inMultiLineComment)
                {
                    if (currentChar == '*' && nextChar == '/')
                    {
                        inMultiLineComment = false;
                        result.Append(currentChar).Append(nextChar);
                        currentIndex++; // Skip the next char
                    }
                    else
                    {
                        result.Append(currentChar);
                    }
                }
                // Handle strings
                else if (inString)
                {
                    result.Append(currentChar);
                    if (currentChar == stringDelimiter && input[currentIndex - 1] != '\\') // End of string
                    {
                        inString = false;
                    }
                }
                // Detect the start of a string
                else if (currentChar == '\'' || currentChar == '"' || currentChar == '`')
                {
                    inString = true;
                    stringDelimiter = currentChar;
                    result.Append(currentChar);
                }
                // Detect single-line comment start
                else if (currentChar == '/' && nextChar == '/')
                {
                    inSingleLineComment = true;
                    result.Append(currentChar).Append(nextChar);
                    currentIndex++; // Skip the next char
                }
                // Detect multi-line comment start
                else if (currentChar == '/' && nextChar == '*')
                {
                    inMultiLineComment = true;
                    result.Append(currentChar).Append(nextChar);
                    currentIndex++; // Skip the next char
                }
                // Handle braces
                else if (currentChar == '{')
                {
                    braceCount++;
                    result.Append(currentChar);
                }
                else if (currentChar == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        return result.ToString();
                    }
                    result.Append(currentChar);
                }
                else
                {
                    result.Append(currentChar);
                }

                currentIndex++;
            }

            throw new InvalidOperationException("Unmatched braces in the @class{ block.");
        }
        public static string EvaluateHTML(string input)
        {
            var regex = new Regex(@"<(.|\n)*?>");

            MatchCollection matches = regex.Matches(input);
            if (matches != null && matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    
                }
            }
            return input;
        }
        public static string EvaluateJavaScript(string source)
        {
            var regex = new Regex(@"({{)([\s\S]*)(}})");
            source = source.Replace("@@","&#64;");
            string input = source;

            MatchCollection matches = regex.Matches(input);
            if (matches != null && matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    string codeToEvaluate = match.Groups[1].Value; 
                    string scriptTag = "<script>" + codeToEvaluate + "</script>";
                    input = input.Replace("{{", "<script> ");
                    input = input.Replace("}}", " </script>");
                    input = input.Replace(match.Groups[1].Value, codeToEvaluate);

                }
            }
            return input;
        }
        public static string EvaluateCSharpCode(object xavier, string source, Assembly assembly, IXavierMemory memory)
        {
            try
            {
                List<string> Blocks = new List<string>();
            source = source.Replace("@@","&#64;");
                var eval = source;
                var startIndex = 0;

                while (source.IndexOf("x{", startIndex) >= 0)
                {
                    int start = eval.IndexOf("x{", startIndex);
                    int end = eval.IndexOf("}x", startIndex) + 2;
                    Blocks.Add(eval.Substring(start, end - start));
                    startIndex = end;
                }
                foreach (var block in Blocks)
                {
                    var tree = CSharpSyntaxTree.ParseText(block);
                    // Use the CSharpSyntaxRewriter to evaluate C# code within a block
                    var rewriter = new ReplaceCodeRewriter();
                    var newRoot = Visit(xavier, tree.GetRoot(), assembly, memory);
                    source = source.Replace(block, newRoot.ToString());

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return source;
        }
        public static string EvaluateXavierCode(string source)
        {
            List<string> Blocks = new List<string>();
            var eval = source;
            var startIndex = 0;

            while (source.IndexOf("@class{", startIndex) >= 0)
            {
                int start = eval.IndexOf("@class{", startIndex);
                int end = eval.LastIndexOf("}", startIndex) + 2;
                Blocks.Add(eval.Substring(start, end - start));
                startIndex = end;
            }
            foreach (var block in Blocks)
            {
                var tree = CSharpSyntaxTree.ParseText(block);
                // Use the CSharpSyntaxRewriter to evaluate C# code within a block
                var rewriter = new ReplaceCodeRewriter();
                var newRoot = rewriter.Visit(tree.GetRoot());
                source = source.Replace(block, newRoot.ToString());
            }
            return source;
        }

        public static string CreateCsxNode(string code)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            string assemblyName = Path.GetRandomFileName();
            MetadataReference[] references = new MetadataReference[]
            {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);
                    foreach (Diagnostic diagnostic in failures)
                    {
                        Console.Error.WriteLine(" {0}: {1}", diagnostic.Id, diagnostic.Descriptor);
                    }
                }
                else
                {
                    string GUID = Guid.NewGuid().ToString();
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());
                    var obj = assembly.CreateInstance("Program" + GUID);
                    Type type = assembly.GetType("Xav");
                    MethodInfo mi = type.GetMethod("Exe");
                    if (mi != null)
                    {
                        var k = (string)mi.Invoke(obj, new object[] { new string[] { } })?.ToString()?? " ";
                        
                        return k?? "";
                    }
                }
                ms.Close();
            }
            return "";
        }

        public static string RunCSharpAssembly(object xavier, string code, Assembly assembly)
        {
            var codeResponse = "";
            try
            {
                var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

                SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                string assemblyName = Path.GetRandomFileName();
                MetadataReference[] references = new MetadataReference[]
                {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.dll")),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Core.dll")),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(assembly.Location), "Xavier.dll")),
            MetadataReference.CreateFromFile(assembly.Location)
            };
                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: new[] { tree },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
                using (var ms = new MemoryStream())
                {
                    EmitResult result = compilation.Emit(ms);
                    if (!result.Success)
                    {
                        IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error);
                        foreach (Diagnostic diagnostic in failures)
                        {
                            Console.Error.WriteLine(" {0}: {1}", diagnostic.Id, diagnostic.GetMessage() + " This is where");
                        }
                    }
                    else
                    {
                        string GUID = Guid.NewGuid().ToString();
                        ms.Seek(0, SeekOrigin.Begin);
                        Assembly sembly = Assembly.Load(ms.ToArray());
                        Type type = sembly.GetType(assembly.GetName().Name.Split(",")[0] + "." + xavier.GetType().GetProperty("Name").GetValue(xavier).ToString()+"_X");

                        MethodInfo mi = type.GetMethod("Exe");

                        object instance = Activator.CreateInstance(type);
                        
                        if (mi != null)
                        {
                            var k = (string)mi.Invoke(instance, new object[] { new string[] { } })?.ToString() ?? " ";
                            
                            codeResponse = k ?? "";
                        }
                        else
                        {
                            Console.WriteLine("No Exe detected");
                        }
                    }
                    ms.Close();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString() + " Problem from RunCSAssembly");
            }
            GC.Collect();
            return codeResponse;
        }
        public static string ToCSharpString(SemanticModel semanticModel)
        {
            return SyntaxFactory
                .SyntaxTree(semanticModel.SyntaxTree.GetRoot())
                .GetText()
                .ToString();
        }
        public static string SemanticModelToString(SemanticModel semanticModel)
        {
            string sb = "";

            //for(int I = 0; I < semanticModel.GetOperation().Syntax.SyntaxTree.Length; I++)
            //{
            //    SyntaxNode NODE = semanticModel.GetOperation().Syntax.SyntaxTree[I];
            //    sb += NODE.Kind().ToString();
            //}

            return sb.ToString();
        }
        public static string ExtractAtVariables(string code)
        {
            Regex regex = new Regex(@"(@\w+[-+]{2}|@\w+)");
            MatchCollection matches = regex.Matches(code);

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    code = code.Replace(match.Value, "{" + match.Groups[1].Value.Replace("@","") + "}");
                }
                return code;
            }
            return code;
        }

        public static List<object> CombineInstances(List<object> instances)
        {
            var combinedInstances = new List<object>();

            foreach (var instance in instances)
            {
                if (combinedInstances.Any(x => (x as CsxNode).Name == (instance as CsxNode).Name))
                {
                    var existingInstance = combinedInstances.First(x => (x as CsxNode).Name == (instance as CsxNode).Name);
                    foreach (var property in instance.GetType().GetProperties())
                    {
                        if (property.CanWrite)
                            property.SetValue(existingInstance, property.GetValue(instance));
                    }
                    combinedInstances.Add(existingInstance);
                }
                else
                {
                    combinedInstances.Add(instance);
                }
            }

            return combinedInstances;
        }

        public static string[] ExtractVariables(string codeString)
        {
            string[] variableList = Regex.Matches(codeString, @"var\s+\w+\s*\=.*;")
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .ToArray();
            return variableList;
        }
        public static string ProcessForeachCode(string input)
        {
            //declare variables
            List<string> variables = new List<string>();
            List<string> assignments = new List<string>();

            
            //process input
            var regex = new Regex(@"(@foreach|@if|@switch|@case)\((.*)\)(\{[\s\S]*?\}|<div[\s\S]*?\/div>)");
            var matches = regex.Matches(input);
            foreach (Match match in matches)
            {
                //get inside the brackets
               string lines = match.Groups[3].Value;
                var VariablesToTrack = ExtractVariables(lines);
                var ElementsToTrack = EvaluateHTML(lines);
                if (ElementsToTrack != null)
                {
                        input = input
                        .Replace(ElementsToTrack
                        .Substring(1, ElementsToTrack.Length -1 ), $"var s = $@\"{ElementsToTrack.Substring(1, ElementsToTrack.Length -2)}\"; item.AppendLine(s);"+"}"+" return item.ToString();");
                    //   OUPUT OF THE BASIC FOREACH
                    //
                }
                

                foreach(var variable in VariablesToTrack)
                {
                    input = input.Replace(variable, "");
                }
                    //parse line for variables
                    
                var matchesVar = Regex.Matches(lines, @"^\s*(private|public|internal|protected|static)?\s*([A-Za-z_]\w*)\s+(\w+)\s*(=\s*(\w+(\s*\|\s*\w+)*))?;");
                    foreach (Match varMatch in matchesVar)
                    {
                    //add variables to list

                        variables.Add(varMatch.Value);
                    }
            }

            //create variables
            StringBuilder output = new StringBuilder();
            string outs = "";
            for (int o = 0; o < variables.Count(); o++)
            {
                //calculate variable name
                string variableName = variables[o].Replace("@foreach", "foreach");
            }
            //Console.WriteLine(output.ToString());

            ////open method

            ////add original code

            var transient = input.Replace("@foreach", "foreach");
            var transient2 = transient.Replace("x{", "x{ StringBuilder item = new StringBuilder(); ");
//            var transient3 = transient2.Replace("}x", "");
            output.AppendLine(transient2.Substring( 0 , transient2.Length ) );
            ////close method


            ////close namespace
            

            //return the parsed code

            GC.Collect();
            return output.ToString();
        }
    }
}