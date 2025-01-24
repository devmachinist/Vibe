using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;

namespace Vibe
{
    public interface ICsxNode
    {
        bool Authorize { get; set; }
        string BaseUrl { get; }
        string Code { get; set; }
        string ContentType { get; set; }
        string dataset { get; set; }
        string HTML { get; set; }
        bool JSClientApi { get; set; }
        string Name { get; set; }
        string Path { get; set; }
        List<PropertyInfo> Properties { get; set; }
        string Route { get; set; }
        string Scripts { get; set; }
        bool ShouldRender { get; set; }
        Type[] Types { get; set; }
        Assembly XAssembly { get; set; }
        string Xid { get; set; }

        string ClassBody(IXavierMemory? memory) ;
        string Content(IXavierMemory? memory);
        string ExtractAtLast(string code);
        string ExtractPropertyDeclaration(object xavier, PropertyInfo propertyInfo, Assembly assembly);
        List<ScriptVariable> ExtractScriptVariables();
        string ExtractVariableList(object xavier, Assembly assembly);
        string GenerateJavascriptApi();
        string GenerateJavaScriptClass(object node, Type cSharpClass, IXavierMemory memory);
        Assembly GetAssembly();
        void GetPropertiesFromClasses();
        void RemoveDuplicates();
    }
}