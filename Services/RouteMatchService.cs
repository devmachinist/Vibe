using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Vibe;
public class RouteMatchService
{
    public List<RouteMatch> Routes { get; set; } = new List<RouteMatch>();
    public void Map(string route, Action<Action, XServerContext> view, string method = "GET")
    {
        Routes.Add(new RouteMatch
        {
            Method = method,
            Route = route,
            View = view,
        });
    }
    /// <summary>
    /// Parses a route to check if it matches a pattern with placeholders and wildcards.
    /// </summary>
    /// <param name="input">The input route string to test.</param>
    /// <param name="pattern">The route pattern string with placeholders (e.g., {itemType}, {id}) and wildcards (*, ?).</param>
    /// <param name="parameters">A dictionary to hold extracted placeholder values.</param>
    /// <returns>True if the input matches the pattern; otherwise, false.</returns>
    public static bool MatchRoute(string input, string pattern, out Dictionary<string, string> parameters)
    {
        parameters = new Dictionary<string, string>();
        Console.WriteLine(pattern);

        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern))
            return false;

        // Convert the route pattern into a regular expression
        string regexPattern = Regex.Replace(pattern, @"\{(\w+)\}", @"(?<$1>[^/]+)");
        regexPattern = regexPattern.Replace("/*/", ";;@;;");
        regexPattern = regexPattern.Replace("/*", ";;@;");
        // Handle wildcards (* and ?) in the pattern
         regexPattern = regexPattern
            .Replace(";;@;;", "/[^/]*/");
         regexPattern = regexPattern
            .Replace(";;@;", "/[^/]*");

        // Add start (^) and end ($) anchors for exact matching
        regexPattern = $"^{regexPattern}$";

        // Perform the regex match
        Match match = Regex.Match(input, regexPattern);
        if (!match.Success)
            return false;

        // Extract placeholder values
        foreach (var groupName in Regex.Match(input, regexPattern).Groups.Keys)
        {
            Console.WriteLine(groupName);
            if (match.Groups[groupName].Success && !int.TryParse(groupName, out _)) // Exclude numeric group names
            {
                parameters[groupName.Replace("/","")] = match.Groups[groupName].Value;
            }
        }

        return true;
    }

    public void Run(Func<Task> serveNext, XServerContext xServerContext)
    {
        foreach(var route in Routes){
            if(MatchRoute(xServerContext.Request.Url.AbsolutePath, route.Route, out var parameters)){
                Console.WriteLine(xServerContext.Request.HttpMethod);
                if(xServerContext.Request.HttpMethod == route.Method)
                {
                    if(route.Method != "GET"){
                        var request = xServerContext.Request;
                        Stream body = request.InputStream;
                        System.Text.Encoding encoding = request.ContentEncoding;
                        StreamReader reader = new StreamReader(body, encoding);
                        if (request.ContentType != null)
                        {
                            Console.WriteLine("Client data content type {0}", request.ContentType);
                        }
                        
                        Console.WriteLine("Client data content length {0}", request.ContentLength64);
                        Console.WriteLine("Start of client data:");

                            // Convert the data to a string and display it on the console.
                        xServerContext.Body = reader.ReadToEnd();
                    }
                    xServerContext.Params = parameters;
                    route.View(()=> serveNext(), xServerContext);
                }
            }
        }
    }

    public class RouteMatch
    {
        public string Method {get;set;} = "GET";
        public string Route { get; set; }
        public Action<Action, XServerContext> View { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }
}
