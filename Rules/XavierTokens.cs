using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;


public class XavierTokens
{
    public static implicit operator Dictionary<char, int>(XavierTokens tokens)
    {
        return tokens.tokens;
    }
    private Dictionary<char, int> tokens;

    public XavierTokens()
    {
        tokens = GetBaseTokens();
    }
    /// <summary>
    /// Gets or sets the value associated with the specified key. Which is a count of the token.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>The count of the tokens</returns>

    public int this[char key]
    {
        get
        {
            if (tokens.ContainsKey(key))
            {
                foreach (var kvp in Doubles){
                    if(kvp.Key == key)
                    {
                        tokens[key]++;
                    }
                    else if(kvp.Value == key)
                    {
                        tokens[key]--;
                    }
                }
                return tokens[key];
            }
            else{
                tokens[key] = 0;
                return 0;
            }
        }
        set
        {
            foreach (var kvp in Doubles){
                if(kvp.Key == key || kvp.Value == key)
                {
                    tokens[kvp.Key] = value;
                    tokens[kvp.Value] = value;
                }
            }
        }
    }
    public Dictionary<char, int> GetBaseTokens()
    {
        return new Dictionary<char, int>
        {
            {'{', 0},
            {'}', 0},
            {'(', 0},
            {')', 0},
            {'[', 0},
            {']', 0},
            {'>', 0},
            {'<', 0}
        };
    }
    public Dictionary<char,char> Doubles = new Dictionary<char, char>()
    {
        {'{', '}'},
        {'(', ')'},
        {'[', ']'},
        {'<', '>'},
    };
}
