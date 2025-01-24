using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq.Expressions;

namespace Vibe.Build
{
    public class CompileCsxTask : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string ProjectDirectory { get; set; }

        [Output]
        public ITaskItem[] GeneratedSyntaxTrees { get; private set; }

        public override bool Execute()
        {
            try
            {
                Console.WriteLine($"Starting compilation of .csx files in {ProjectDirectory}...");

                var scripting = new Vibe.Scripting(new System.Dynamic.ExpandoObject())
                {
                    _projectDirectory = ProjectDirectory
                };

                GeneratedSyntaxTrees = scripting.CompileCsxFiles("");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

    }
}
