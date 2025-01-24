using System.Reflection;

namespace Vibe
{
    public interface IXavierMemory
    {
        bool? AddAuthentication { get; set; }
        string? BaseURI { get; set; }
        string? EFModule { get; set; }
        bool IsSPA { get; set; }
        string? JSModule { get; set; }
        string? StaticFallback { get; set; }
        string? StaticRoot { get; set; }
        string? XavierName { get; set; }
        List<object> CsxNodes { get; set; }

        void Dispose();
        List<Assembly> GetAllAssemblies();
        Task Init(string? root = null, string? destination = null, bool isSPA = true, Assembly? asm = null);
        string? JSAuth();
        Task SearchForCsxNodesAndChildren(string searchDir, bool searchSubdirectories, Assembly assembly);
        void WriteModule();
    }
}