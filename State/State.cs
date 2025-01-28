using AngleSharp;
using AngleSharp.Io;
using AngleSharp.Scripting;
using Constellations;
using DotJS;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Vibe
{
    public class State
    {
        public string Id { get; set; }
        public IBrowsingContext Context {get;set;}
        public JS js { get; set; }
        public Constellation InteropConstellation { get; set; }
        public dynamic[] Components { get; set; }
        public ConcurrentBag<object> UserComponents { get; set; } = [];
        public bool Changed() => UserComponents.Where(u => (u as CsxNode).HasChanges == true).Count() > 0;
        public ScriptingService Scripts { get; set; }
        public XUser XUser { get; set; }
        public CsxDocument Document { get; set; }

        public State(Func<CsxNode> initialize)
        {
            Context = BrowsingContext.New();
            CsxDocument Doc = new CsxDocument(Context, initialize());
            Doc.Root.SetDocument(Doc);
            Document = Doc;
        }
        public State(Func<Task<CsxNode>> initialize)
        {
            Context = BrowsingContext.New();
            CsxDocument Doc = new CsxDocument(Context, Task.Run( async () => await initialize()).Result);
            Doc.Root.SetDocument(Doc);
            Document = Doc;
        }
        public State(CsxNode node)
        {
            Context = BrowsingContext.New();
            CsxDocument Doc = new(Context, node);
            Doc.Root.SetDocument(Doc);
            Document = Doc;
        }
        public State UseJs(Constellation constellation){
            InteropConstellation = constellation;
            js = new JS(XUser.Id, constellation, Guid.NewGuid().ToString());
            return this;
        }

        public State SetUser(XUser user)
        {
            XUser = user;
            Id = user.Id;
            return this;
        }
        public void UpdateComponent(dynamic component)
        {
           var components = UserComponents.Where(c => (c as CsxNode).Xid != (component as CsxNode).Xid).ToList();
           UserComponents = new ConcurrentBag<dynamic>(components);
           UserComponents.Add(component);
        }
    }
    public class ScriptingService : IScriptingService
    {
        public Scripting Scripting { get; set; }
        public dynamic Scope { get; set; }
        /// <summary>
        /// Initializes a new ScriptingService that can be used to evaluate V8Script
        /// </summary>
        /// <param name="context"></param>
        public ScriptingService(IBrowsingContext context)
        {
            Scope["Hey"]= (Func<Task<dynamic>>)(() => { return (dynamic)"HEY!"; });
            Scope["window"] = context;
            Scripting = new Scripting(Scope);

            var package = JsonDocument.Parse(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "package.json"));
        }
        public async Task EvaluateScriptAsync(IResponse response, ScriptOptions options, CancellationToken cancel)
        {
        }
        public async Task<object> Eval(string script)
        {
            var result = "";// JSEngine.Evaluate(JSEngine.Compile(script));
            return result;
        }
        public bool SupportsType(string mimeType)
        {
            return MimeTypeNames.IsJavaScript(mimeType);    
        }
    }
}
