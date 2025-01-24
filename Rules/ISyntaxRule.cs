using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Rules
{
    public interface ISyntaxRule
    {
        /// <summary>
        /// A descriptive name or identifier for the rule, which can be used for logging or diagnostics.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Applies the rule to a given C# code string, potentially rewriting or modifying it.
        /// </summary>
        /// <param name="code">The C# code to process.</param>
        /// <returns>The rewritten or unchanged C# code after applying the rule.</returns>
        string Apply(string code);
    }

}
