using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Vibe
{
    public static class NodeModules
    {
        private static Dictionary<string, string> Modules { get; set; } = new Dictionary<string, string>();

        static NodeModules()
        {
            LoadModules();
        }

        private static void LoadModules()
        {
            try
            {
                if (File.Exists("package.json"))
                {
                    var packageJsonContent = File.ReadAllText("package.json");
                    using var jsonDoc = JsonDocument.Parse(packageJsonContent);
                    var root = jsonDoc.RootElement;
                    if (root.TryGetProperty("dependencies", out JsonElement dependencies))
                    {
                        foreach (var dependency in dependencies.EnumerateObject())
                        {
                            LoadModuleWithDependencies(dependency.Name);
                        }
                    }
                }
                else
                {
                    throw new FileNotFoundException("package.json not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading modules from package.json: {ex.Message}");
                throw;
            }
        }

        private static void LoadModuleWithDependencies(string moduleName)
        {
            if (Modules.ContainsKey(moduleName))
            {
                Debug.WriteLine(@$"Cannot use more than one version of  ""{moduleName}"" Xavier will use the one that came first in your dependacies listed in your package.json file");
                return; // Module is already loaded
            }

            string modulePath = Path.Combine("node_modules", moduleName);

            if (Directory.Exists(modulePath))
            {
                string mainFilePath = Path.Combine(modulePath, "index.csx");
                string packageFilePath = Path.Combine(modulePath, "package.json");

                if (File.Exists(packageFilePath))
                {
                    var modulePackageContent = File.ReadAllText(packageFilePath);
                    using var moduleJsonDoc = JsonDocument.Parse(modulePackageContent);
                    var moduleRoot = moduleJsonDoc.RootElement;

                    if (moduleRoot.TryGetProperty("main", out JsonElement mainProperty))
                    {
                        mainFilePath = Path.Combine(modulePath, mainProperty.GetString());
                    }

                    if (moduleRoot.TryGetProperty("dependencies", out JsonElement subDependencies))
                    {
                        foreach (var subDependency in subDependencies.EnumerateObject())
                        {
                            LoadModuleWithDependencies(subDependency.Name);
                        }
                    }
                }

                if (File.Exists(mainFilePath))
                {
                    Modules[moduleName] = mainFilePath;
                }
            }
            else
            {
                throw new FileNotFoundException($"Module '{moduleName}' not found in node_modules.");
            }
        }

        public static string GetModulePath(string moduleName)
        {
            if (Modules.TryGetValue(moduleName, out string modulePath))
            {
                return modulePath;
            }
            else
            {
                throw new FileNotFoundException($"Module '{moduleName}' not found.");
            }
        }

        public static async Task<string> LoadModuleScriptAsync(string moduleName)
        {
            string modulePath = GetModulePath(moduleName);
            if (!File.Exists(modulePath)) throw new FileNotFoundException($"Module '{modulePath}' not found.");
            return await File.ReadAllTextAsync(modulePath);
        }

        public static string LoadModuleScript(string moduleName)
        {
            string modulePath = GetModulePath(moduleName);
            if (!File.Exists(modulePath)) throw new FileNotFoundException($"Module '{modulePath}' not found.");
            return File.ReadAllText(modulePath);
        }
    }
}
