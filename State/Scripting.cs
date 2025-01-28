using System.Diagnostics;
using System.Dynamic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Vibe.Rules;

namespace Vibe
{
    public class Scripting
    {
        public dynamic Globals { get; }
        public Dictionary<string, Dictionary<string, object>> ModuleExports { get; }
        public string _projectDirectory;
        public string _nodeModulesPath;

        private static readonly string[] SupportedExtensions = { ".csx", ".js", ".jsx", ".tsx", ".pyx" };
        private readonly CsxParser _csxParser = new();

        public Scripting(ExpandoObject globals)
        {
            Globals = globals;
            ModuleExports = new();
            _projectDirectory = Directory.GetCurrentDirectory();
            _nodeModulesPath = Path.Combine(_projectDirectory, "node_modules");
        }

        public Microsoft.Build.Framework.ITaskItem[] CompileCsxFiles(string outputDllPath)
        {

            var csxFiles = Directory.GetFiles(_projectDirectory, "*.csx", SearchOption.AllDirectories);
            PackageJson packageJson = ProcessPackageJson();
            var genSyntaxTrees = new List<ITaskItem>();

            // Ensure the obj directory exists for generated code files
            string objDir = Path.Combine(_projectDirectory, "obj");
            Directory.CreateDirectory(objDir);

            foreach (var csxFile in csxFiles)
            {
                var scriptCode = File.ReadAllText(csxFile);
                var (usings, code) = ExtractUsings(scriptCode);


                // Determine the class name from the file name, or use "Main" if it's the main entry
                var className = packageJson.main == Path.GetFileName(csxFile)
                    ? "Main"
                    : Regex.Replace(csxFile.Replace($"{_projectDirectory}","").Replace(".csx",""), @"[\\/]","_");

                var processedCode = usings + "\r\n" + "using System;\r\nusing AngleSharp;\r\nusing AngleSharp.Browser;\r\nusing AngleSharp.Dom;\r\nusing Vibe;\r\nusing Microsoft.Extensions.DependencyInjection;\r\nusing System.Dynamic;\r\nusing System.Collections.Generic;\r\n" + _csxParser.ParseCsx(code, className, null, _projectDirectory);

                // Write the processed code to a temporary .cs file
                // Use a unique name to avoid conflicts
                string generatedFileName = Path.Combine(objDir,
                    "Generated_" + className.Replace(".csx",".cs"));

                File.WriteAllText(generatedFileName, processedCode);

                // Create a task item pointing to the generated file
                var taskItem = new TaskItem(generatedFileName);
                genSyntaxTrees.Add(taskItem);
            }

            return genSyntaxTrees.ToArray();
        }

        public (string Usings, string Code) ExtractUsings(string scriptContent)
        {
            if (string.IsNullOrWhiteSpace(scriptContent))
            {
                return (string.Empty, string.Empty);
            }

            // Regex to match top-level using statements that are NOT variable declarations
            var usingRegex = new Regex(@"^\s*using\s+[a-zA-Z0-9_.]+;\s*$", RegexOptions.Multiline);

            var usingStatements = new StringBuilder();
            var matches = usingRegex.Matches(scriptContent);

            foreach (Match match in matches)
            {
                usingStatements.AppendLine(match.Value.Trim());
            }

            // Remove only matched using statements from the script content
            var modifiedContent = usingRegex.Replace(scriptContent, string.Empty);

            // Return the usings and the cleaned code
            return (usingStatements.ToString().Trim(), modifiedContent.Trim());
        }

        public PackageJson? ProcessPackageJson()
        {
            string packageJsonPath = Path.Combine(_projectDirectory, "package.json");
            if (!File.Exists(packageJsonPath)) return null;

            try
            {
                var packageJson = JsonSerializer.Deserialize<PackageJson>(File.ReadAllText(packageJsonPath));
                if (packageJson?.main == null) return null;
                Console.WriteLine($"Main file defined in package.json: {packageJson.main}");
                return packageJson;

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading package.json: {ex.Message}");
                return null;
            }
        }
    }

    public class CsxParser
    {
        public string? _projectDirectory;
        public string? _className; 
        public string ParseCsx(string scriptCode, string className, string? Namespace = null, string? projectdir = null)
        {
            _projectDirectory = projectdir ?? string.Empty;
            _className = className ?? string.Empty;
            return ProcessScript(scriptCode, className, Namespace);
        }

        public string ProcessScript(string scriptCode, string className, string? Namespace)
        {
            // Decide which method (Main or Run) we place code into
            className = className.Replace(".csx", "");
            var methodName = (className == "Main") ? "Main" : "Run";

            // Parse the script into a syntax tree
            var tree = CSharpSyntaxTree.ParseText(scriptCode);
            var rewriter = new RecursiveSpreadSyntaxRewriter();

            var rulesEngine = new SyntaxRulesEngine().AddRule(new CSXTagRule());
            scriptCode = rulesEngine.Run(rewriter.Visit(tree.GetRoot()).ToFullString());
            var (services, strippedServiceCode) = StripServicesBlock(scriptCode);
            scriptCode = strippedServiceCode;

            // Transform the syntax tree


            var exportRegistrations = new List<string>();
            var importVarStatements = new List<string>();
            var exportVarStatements = new List<string>();

            // Process line by line, handling import and export lines
            var (injections, strippedCode) = ExtractInjections(scriptCode);
            scriptCode = strippedCode;
            var lines = scriptCode.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();

                if (IsImportLine(trimmed))
                {
                    // Convert import line into var statements and remove the line
                    var stmts = ConvertImportToVars(trimmed);
                    importVarStatements.AddRange(stmts);
                    lines[i] = "";
                }
                else if (IsExportLine(trimmed))
                {
                    // Export line:
                    // If method with body: remove only 'export' keyword, keep semicolon if present, keep code intact
                    // Add registration line for the method.
                    // If variable or default export: remove 'export' or 'export default' prefix only, keep rest of the line intact, add registration line(s) for the names.
                    if (IsMethodWithBody(trimmed))
                    {
                        var cleaned = RemoveExportKeywordOnly(line);
                        var methodNameFound = ExtractMethodNameFromSignature(cleaned);
                        if (!string.IsNullOrEmpty(methodNameFound))
                            exportRegistrations.Add($"Exports[\"{methodNameFound}\"] = {methodNameFound};");
                        lines[i] = cleaned;
                    }
                    else
                    {
                        // Non-method export
                        var (cleanedLine, exportedNames) = RemoveExportPrefixAndGetNames(line);
                        foreach (var name in exportedNames)
                        {
                            exportRegistrations.Add($"Exports[\"{name}\"] = {name};");
                        }
                        lines[i] = cleanedLine;
                    }
                }
            }

            // Build final code
            var finalSb = new System.Text.StringBuilder();
            finalSb.AppendLine($"namespace {Namespace ?? "GeneratedScripts"}");
            finalSb.AppendLine("{");
            finalSb.AppendLine($"    public class {(className == "Main" ? "Program" : className)}");
            finalSb.AppendLine($"    {{\r\n {string.Join("\r\n",injections.Select(i => $"{i.Replace("@inject", "       [Inject]\r\n        public").Replace(";","{get;set;}")}")) } ");
            finalSb.AppendLine($"        public {(className == "Main" ? "static" : "")} Dictionary<string, object>? Exports {{ get; set; }}");
            finalSb.AppendLine($"        public {(className == "Main" ? "static" : "")} IAdvancedServiceProvider? ServiceProvider {{ get; set; }}");
            finalSb.AppendLine($"        public {(methodName == "Main" ? "static void" : "dynamic")} {methodName}()");
            finalSb.AppendLine("        {");
            if (methodName == "Main")
                finalSb.AppendLine("");
            else
                finalSb.AppendLine("            Exports = new Dictionary<string,object>();");

            finalSb.AppendLine("ServiceProvider = ServiceHub.Build();\r\n"+services);
            finalSb.AppendLine("var ServiceFactory = (DependencyInjectorFactory)ServiceProvider.GetService(typeof(DependencyInjectorFactory));");
            // Add import var statements
            foreach (var stmt in importVarStatements)
            {
                finalSb.AppendLine("            " + stmt);
            }

            // Original lines (minus removed lines)
            foreach (var l in lines)
            {
                if (!string.IsNullOrWhiteSpace(l))
                {
                    finalSb.AppendLine("            " + l.TrimEnd('\r', '\n'));
                }
            }
            // Add export var statements
            foreach (var reg in exportVarStatements)
            {
                if (methodName == "Main")
                    finalSb.AppendLine(" ");
                else
                    finalSb.AppendLine("            " + reg);
            }

            // Add export registrations
            foreach (var reg in exportRegistrations)
            {
                finalSb.AppendLine("            " + reg);
            }

            if (methodName == "Main")
                finalSb.AppendLine(" ");
            else
                finalSb.AppendLine("            return this;");

            finalSb.AppendLine("        }");
            finalSb.AppendLine("    }");
            finalSb.AppendLine("}");
            
            return finalSb.ToString();
        }

        private bool IsImportLine(string line)
        {
            return line.StartsWith("import");
        }

        private bool IsExportLine(string line)
        {
            return line.StartsWith("export");
        }

        private bool IsMethodWithBody(string line)
        {
            // Define a regex pattern to match method declarations with a body
            string pattern = @"^\s*export\s+(?:public|private|protected|internal|static|async|virtual|override|sealed|partial|\s+)*\w+(\<.*?\>)?\s+\w+\s*\(.*?\)\s*\{";

            // Perform a regex match to determine if the line is a method with a body
            return Regex.IsMatch(line, pattern);
        }
        /// <summary>
        /// Extracts @inject statements from a Razor script and separates the rest of the script.
        /// </summary>
        /// <param name="razorScript">The Razor script as a string.</param>
        /// <returns>A tuple containing a list of injection statements and the remaining script.</returns>
        public (List<string> Injections, string RemainingScript) ExtractInjections(string razorScript)
        {
            if (string.IsNullOrWhiteSpace(razorScript))
                return (new List<string>(), "");

            // Regex to match @inject statements
            var injectRegex = new Regex(@"(@inject\s+\S+\s+\S+)", RegexOptions.Compiled);

            var injections = new List<string>();
            var remainingScript = razorScript;

            // Extract all @inject statements
            foreach (Match match in injectRegex.Matches(razorScript))
            {
                injections.Add(match.Value);
                remainingScript = remainingScript.Replace(match.Value, "").Trim();
            }

            return (injections, remainingScript);
        }

        private List<string> ConvertImportToVars(string line)
        {
            var stmts = new List<string>();
            var fromIndex = line.IndexOf("from");
            if (fromIndex == -1) return stmts;
            var importsPart = line.Substring(0, fromIndex).Replace("import", "").Trim();
            var modulePath = line.Substring(fromIndex + 4).Trim(' ', ';', '"');
            if (string.IsNullOrEmpty(modulePath)) modulePath = "Module";
            modulePath = Path.GetFullPath(Path.Combine(_projectDirectory,
                string.Join("\\", Regex.Split(
                                    _className.Replace("_", "/"), @"[\\/]"
                                )
                ).Remove(
                         _className.IndexOf(
                                   _className.Split("_").Last()
                                   )
                         )
                ,
                modulePath.Replace("/","\\")
                
            ));
            Debug.WriteLine( modulePath );
            var moduleName = Regex.Replace(modulePath.Replace(_projectDirectory, "").Replace(".csx",""), @"[\\/]", "_");
            if (string.IsNullOrEmpty(moduleName)) moduleName = "ImportedModule";

            var imports = importsPart.Trim('{', '}', ' ', '\r', '\n', '\t');
            var importName = moduleName.ToLower();
            if (string.IsNullOrEmpty(imports)) return stmts;

            if (imports.StartsWith("* as "))
            {
                var alias = imports.Substring("* as".Length).Trim();
                stmts.Add($"var {alias} = ServiceFactory.Create<{moduleName}>().Run();");
            }
            else
            {
                stmts.Add($"var {importName} = ServiceFactory.Create<{moduleName}>().Run();");
                foreach (var imp in imports.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0))
                {
                    stmts.Add($"var {imp} = ((dynamic){importName}).Exports[\"{imp}\"];");
                }
            }
            return stmts;
        }

        // Method to strip the @Services{} block and return the tuple
        public static (string, string) StripServicesBlock(string input)
        {
            StringBuilder servicesContent = new StringBuilder();
            StringBuilder strippedCode = new StringBuilder();
            bool insideServicesBlock = false;
            int braceDepth = 0;

            for (int i = 0; i < input.Length; i++)
            {
                if (!insideServicesBlock && input.Substring(i).StartsWith("@Services{"))
                {
                    insideServicesBlock = true;
                    braceDepth = 1; // Start of the `@Services{` block
                    i += "@Services{".Length - 1; // Skip to the character after `@Services{`
                    continue;
                }

                if (insideServicesBlock)
                {
                    if (input[i] == '{')
                    {
                        braceDepth++;
                    }
                    else if (input[i] == '}')
                    {
                        braceDepth--;
                        if (braceDepth == 0)
                        {
                            insideServicesBlock = false; // End of the block
                            continue;
                        }
                    }

                    servicesContent.Append(input[i]);
                }
                else
                {
                    strippedCode.Append(input[i]);
                }
            }

            return ("var services = ServiceProvider.ServiceCollection;\r\n" + servicesContent.ToString(), strippedCode.ToString().Trim());
        }
        private (string cleanedLine, List<string> names) RemoveExportPrefixAndGetNames(string line)
        {
            var names = new List<string>();
            string cleaned = line;

            if (line.StartsWith("export default"))
            {
                cleaned = line.Replace("export default", "").TrimStart();
                // Extract a name
                var possibleName = ExtractFirstIdentifier(cleaned);
                if (string.IsNullOrEmpty(possibleName)) possibleName = "DefaultExportVar";
                names.Add(possibleName);
            }
            else if (line.StartsWith("export {"))
            {
                var inner = ExtractBetween(line, '{', '}');
                var listed = inner.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
                names.AddRange(listed);
                cleaned = line.Replace("export {", "{");
                cleaned = cleaned.Replace("}", "}").Trim();
                cleaned = cleaned.Replace("export", "").Trim();
            }
            else
            {
                // just 'export something'
                var idx = line.IndexOf("export");
                cleaned = line.Remove(idx, "export".Length).Trim();
                var possibleName = ExtractFirstIdentifier(cleaned);
                if (string.IsNullOrEmpty(possibleName)) possibleName = "ExportedVar";
                names.Add(possibleName);
            }

            return (cleaned, names);
        }

        private string RemoveExportKeywordOnly(string line)
        {
            var idx = line.IndexOf("export");
            if (idx != -1)
            {
                line = line.Remove(idx, "export".Length);
            }
            return line.Trim();
        }

        private string ExtractMethodNameFromSignature(string sig)
        {
            var paren = sig.IndexOf('(');
            if (paren == -1) return "";
            var before = sig.Substring(0, paren).Trim();
            var parts = before.Split(' ', '\t', '\n', '\r').Where(p => p.Length > 0).ToArray();
            return parts.LastOrDefault() ?? "";
        }

        private string RemoveExportKeywordAndSemicolon(string line)
        {
            var noExport = RemoveExportKeywordOnly(line);
            // Do not remove semicolon unless user said so. User says do not remove semicolons.
            // So we won't remove semicolon here. Just return noExport
            return noExport;
        }

        private string ExtractFirstIdentifier(string code)
        {
            var tokens = code.Split(new char[] { ' ', '\t', '\r', '\n', '(', ')', '{', '}', '=', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var ignoreSet = new HashSet<string>(new[] { "async", "const", "var", "default", "public", "async", "Task", "dynamic", "return", "new" });
            foreach (var t in tokens)
            {
                if (!ignoreSet.Contains(t))
                    return t;
            }
            return "";
        }

        private string ExtractBetween(string text, char start, char end)
        {
            var s = text.IndexOf(start);
            if (s == -1) return "";
            var e = text.IndexOf(end, s + 1);
            if (e == -1) return "";
            return text.Substring(s + 1, e - (s + 1));
        }

        // We no longer create separate run/main methods from the code lines, 
        // we just put them at the end.
        private MethodDeclarationSyntax CreateRunMethod(
            List<string> exportRegistrations,
            List<string> topLevelStatements,
            List<string> localVarsOrMethods)
        {
            var body = new List<StatementSyntax>();
            body.Add(SyntaxFactory.ParseStatement("Exports = new Dictionary<string, object>();"));
            foreach (var line in topLevelStatements.Where(l => !string.IsNullOrWhiteSpace(l)))
                body.Add(SyntaxFactory.ParseStatement(line));
            foreach (var line in localVarsOrMethods.Where(l => !string.IsNullOrWhiteSpace(l)))
                body.Add(SyntaxFactory.ParseStatement(line));
            foreach (var line in exportRegistrations)
                body.Add(SyntaxFactory.ParseStatement(line));
            body.Add(SyntaxFactory.ParseStatement("return this;"));

            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName("object"),
                "Run")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithBody(SyntaxFactory.Block(body));
        }

        private MethodDeclarationSyntax CreateMainMethod(
            List<string> topLevelStatements,
            List<string> localVarsOrMethods,
            List<string> exportRegistrations)
        {
            var body = new List<StatementSyntax>();
            body.Add(SyntaxFactory.ParseStatement("Exports = new Dictionary<string, object>();"));
            foreach (var line in topLevelStatements.Where(l => !string.IsNullOrWhiteSpace(l)))
                body.Add(SyntaxFactory.ParseStatement(line));
            foreach (var line in localVarsOrMethods.Where(l => !string.IsNullOrWhiteSpace(l)))
                body.Add(SyntaxFactory.ParseStatement(line));
            foreach (var line in exportRegistrations)
                body.Add(SyntaxFactory.ParseStatement(line));
            body.Add(SyntaxFactory.ParseStatement("return;"));

            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName("void"),
                "Main")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
            .WithBody(SyntaxFactory.Block(body));
        }

        private PropertyDeclarationSyntax CreateExportsProperty()
        {
            return SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.ParseTypeName("Dictionary<string, object>"),
                "Exports")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithAccessorList(SyntaxFactory.AccessorList(
                SyntaxFactory.List(new[]
                {
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                })
            ));
        }
    }


    public class PackageJson
    {
        public string main { get; set; }
        public Dictionary<string, string> dependencies { get; set; }
    }
}
