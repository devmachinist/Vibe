using System.Collections.Generic;
namespace Vibe.Rules;
public class SyntaxRulesEngine
{
    private readonly List<ISyntaxRule> _rules = new List<ISyntaxRule>();

    /// <summary>
    /// Adds a new rule to the engine's pipeline.
    /// </summary>
    /// <param name="rule">The rule to add.</param>
    public SyntaxRulesEngine AddRule(ISyntaxRule rule)
    {
        _rules.Add(rule);
        return this;
    }

    /// <summary>
    /// Applies all rules in the order they were added to the engine.
    /// </summary>
    /// <param name="code">The initial C# code to process.</param>
    /// <returns>The resulting C# code after all rules have been applied.</returns>
    public string Run(string code)
    {
        foreach (var rule in _rules)
        {
            code = rule.Apply(code);
        }

        return code;
    }
}
