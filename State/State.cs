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
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public JS js { get; set; }
        public Constellation InteropConstellation { get; set; }
        public dynamic[] Components { get; set; }
        public ConcurrentBag<object> UserComponents { get; set; } = [];
        public bool Changed() => UserComponents.Where(u => (u as CsxNode).HasChanges == true).Count() > 0;
        public XUser XUser { get; set; }
        public CsxDocument Document { get; set; }

        public State(Func<CsxNode> initialize)
        {
            CsxDocument Doc = new CsxDocument(initialize);
            Document = Doc;
        }
        public State(Func<Task<CsxNode>> initialize)
        {
            CsxDocument Doc = new CsxDocument(() => initialize().Result);
            Document = Doc;
        }
        public State(CsxNode node)
        {
            CsxDocument Doc = new(() => node);
            Document = Doc;
        }
        public State UseJs(Constellation constellation){
            InteropConstellation = constellation;
            js = new JS(Id, constellation, Guid.NewGuid().ToString());
            Document.JS = js;
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
}
