using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Reflection;

namespace Vibe
{
    public interface ICsxNode
    {
        object? this[string key] { get; set; }
        Dictionary<string, dynamic> Attributes { get; set; }
        Dictionary<string, dynamic> Properties { get; set; }
        bool Authorize { get; set; }
        string BaseUrl { get; set; }
        List<object> ChildNodes { get; set; }
        ObservableCollection<dynamic> Children { get; set; }
        string Class { get; set; }
        string Code { get; set; }
        string ContentType { get; set; }
        XServerContext? Context { get; set; }
        string dataset { get; set; }
        CsxDocument? document { get; set; }
        bool HasChanges { get; set; }
        string HTML { get; set; }
        string? Id { get; }
        int index { get; set; }
        bool JSClientApi { get; set; }
        string Name { get; set; }
        ICsxNode? Parent { get; set; }
        bool FirstRender { get; set; }
        string Path { get; set; }
        string Route { get; set; }
        string Scripts { get; set; }
        string SessionId { get; set; }
        bool ShouldRender { get; set; }
        State state { get; set; }
        string TagName { get; set; }
        string Xid { get; set; }
        bool Rendering { get; set; }
        public Dictionary<string, string> LiveAttributes { get; set; }

        ICsxNode AddAttribute(string key, dynamic value);
        dynamic AddChild(dynamic child);
        ICsxNode Append(object child);
        void Dispose();
        void emit(string eventName, params object[] args);
        string GenerateJavascriptApi();
        string GetAttribute(string name);
        Dictionary<string, object> GetDynamicProperties();
        Dictionary<string, object> GetProperties();
        void off(string eventName);
        void on(string eventName, object listener);
        void once(string eventName, Action<object[]> listener);
        void OnRender();
        void OnParametersSet();
        void OnInitialized();
        void OnAfterRender();
        ICsxNode Render();
        void ReplaceChild(ICsxNode element, ICsxNode node);
        ICsxNode StageAtt(string name, Delegate value);
        ICsxNode StageAtt(string name, dynamic value);
        ICsxNode SetAttribute(string name, string value);
        ICsxNode RemoveAttribute(string name);
        ICsxNode SetContext(XServerContext? context);
        ICsxNode SetDocument(CsxDocument? doc);
        ICsxNode SetIndex(int index);
        ICsxNode SetParent(ICsxNode parent);
        void SetProperties(Dictionary<string, object> properties);
        ICsxNode SetXid(string id);
        void Toggle(string oldValue, string newValue);
        string ToJson();
        string ToString();
        bool TryGetMember(GetMemberBinder binder, out object result);
        bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out dynamic? result);
        bool TrySetMember(SetMemberBinder binder, object value);
        ICsxNode GetElementById(string id);
    }
}