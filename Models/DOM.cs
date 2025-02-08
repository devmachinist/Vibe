using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Constellations;
using DotJS;

namespace Vibe
{
    public partial class CsxDocument
    {
        public ICsxNode Root { get; set; }
        public object LiveRoot { get; private set; }
        public Func<ICsxNode> App { get; set; }
        public ObservableCollection<ICsxNode> LiveNodes { get; set; } = new ObservableCollection<ICsxNode>();
        public JS? JS { get; set; } = null;

        public CsxDocument(Func<ICsxNode> app)
        {
            App = app;
            LiveNodes.CollectionChanged += LiveNodes_CollectionChanged;
        }

        private void LiveNodes_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {

        }

        public List<SessionEvent> SessionEvents { get; set; } = new List<SessionEvent>();
        /// <summary>
        /// This adds a session event. If the document has a connection to the browser it will send an update to make sure all nodes are tracked.
        /// It also allows for one to listen for custom events.
        /// </summary>
        /// <param name="xid">The xid of the component.</param>
        /// <param name="eventName">The event name. ex. "click"</param>
        public void AddSessionEvent(string xid, string eventName)
        {
            if (SessionEvents.FirstOrDefault(e => e.xid == xid && eventName == e.eventName) is null)
            {
                SessionEvents.Add(new SessionEvent(xid, eventName));
                if (JS != null)
                {
                    _ = Task.Run(async () => await JS.InvokeAsync("window.setSessionEvents", [SessionEvents])).Result;
                }
            }
        }
        public class SessionEvent {
            public string xid { get; set; }
            public string eventName { get; set; }
            public SessionEvent(string xid, string eventName)
            {
                this.xid = xid;
                this.eventName = eventName;
            }
        }

        // Function to send updates to the client via JS invocation
        public void Initialize(XServerContext context) {
            Root = App().SetContext(context).SetDocument(this).Render();
            AttachSessionScript(context.XUser.Id);
        }

        public void AttachSessionScript(string id)
        {
            var script = new CsxNode("script");
            script.Append(@"
(function () {
    const windowId = '" + id + @"';
    console.log('Window ID:', windowId);

    const csxScript = document.createElement('script');
    csxScript.src = `/_csx?windowId=${windowId}`;
    csxScript.type = 'text/javascript';
    document.head.appendChild(csxScript);
    window.sessionEvents = " + JsonSerializer.Serialize(SessionEvents) + @";
    window.setSessionEvents = (events) => {
        window.sessionEvents = JSON.parse(events);
    }
})();
");

            // Insert the script at the top of the <head> tag
            var children = Root.Children;
            var head = children.FirstOrDefault(x => x["TagName"] == "head");
            if (head != null)
            {
                head.Append(script).Render();
                Root.Children = new ObservableCollection<dynamic>(children);
            }
        }
        public void Dispose()
        {
            LiveNodes.Clear();
            Root.Dispose();
        }

        public ConcurrentBag<dynamic> LastChanges { get; set; } = new ConcurrentBag<dynamic>();
        // Function to send updates to the client via JS invocation
        public async Task SendUpdateToClient(string action, string? targetXid, string? htmlContent, string? attributeName = "", string? parentXid = "", string? previousSiblingXid = "", string? nextSiblingXid = "")
        {
            // Define the payload to be sent to the client
            var updatePayload = new
            {
                action = action,
                attributeName = attributeName,
                targetXid = targetXid,
                htmlContent = htmlContent,
                parentXid = parentXid,
                previousSiblingXid = previousSiblingXid,
                nextSiblingXid = nextSiblingXid,
            };
            LastChanges.Add(updatePayload);

            // Invoke the JavaScript function that handles DOM updates on the client side
            if (JS != null)
            {
                await JS.InvokeAsync("window.updateClientSideDom", [updatePayload]);
            }
        }

        public void appendChild(dynamic parent, dynamic child)
        {
  
            if (parent == null || child == null)
                throw new InvalidOperationException("Parent or child element is not valid.");
            var p = (parent as CsxNode);
            p.Children.Add(child as CsxNode);
        }

        public void removeChild(dynamic parent, dynamic child)
        {
            if (parent == null || child == null)
                throw new InvalidOperationException("Parent or child element is not valid.");
            var p = (parent as CsxNode);
            p.Children = new ObservableCollection<dynamic>(p.Children.Where(c => (c as CsxNode).Xid != (child as CsxNode).Xid));
        }

        public void SetAttribute(object node, string name, dynamic value)
        {
            var element = node as CsxNode;

            if (element == null)
                throw new InvalidOperationException("Node is not a valid element.");

            element.Attributes[name] = value;
        }

        public string ToHtml()
        {
            return Root.ToString();
        }
        public ICsxNode? QuerySelector(string selector)
        {
            return QuerySelectorAll(selector).FirstOrDefault();
        }

        public List<ICsxNode> QuerySelectorAll(string selector)
        {
            return MatchSelectorRecursive(Root.Children.Where(x => x is ICsxNode).ToList(), selector).ToList();
        }

        private IEnumerable<ICsxNode> MatchSelectorRecursive(IEnumerable<dynamic> nodes, string selector)
        {
            foreach (var n in nodes)
            {
                ICsxNode node = n as ICsxNode;
                if (MatchesSelector(node, selector))
                    yield return node;

                foreach (var child in node.Children.OfType<ICsxNode>())
                    foreach (var match in MatchSelectorRecursive(new[] { child }, selector))
                        yield return match;
            }
        }

        private bool MatchesSelector(ICsxNode node, string selector)
        {
            if (string.IsNullOrWhiteSpace(selector)) return false;

            var parts = selector.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return MatchesComplexSelector(node, parts, parts.Length - 1);
        }

        private bool MatchesComplexSelector(ICsxNode node, string[] parts, int index)
        {
            if (index < 0) return true;

            var part = parts[index];

            if (!MatchesSimpleSelector(node, part)) return false;

            if (index == 0) return true;

            foreach (var parent in GetAncestors(node))
            {
                if (MatchesComplexSelector(parent, parts, index - 1))
                    return true;
            }

            return false;
        }

        private bool MatchesSimpleSelector(ICsxNode node, string selector)
        {
            if (selector.StartsWith("#") && node.LiveAttributes.TryGetValue("id", out var id))
                return id == selector.Substring(1);

            if (selector.StartsWith(".") && node.LiveAttributes.TryGetValue("class", out var classList))
                return classList.Split(' ').Contains(selector.Substring(1));

            if (selector.StartsWith("[") && selector.EndsWith("]"))
            {
                var attrSelector = selector.Trim('[', ']');
                var parts = attrSelector.Split('=');

                if (parts.Length == 2)
                    return node.LiveAttributes.TryGetValue(parts[0], out var attrValue) && attrValue == parts[1].Trim('"').Trim('\'');

                return node.LiveAttributes.ContainsKey(attrSelector);
            }

            return node.TagName.Equals(selector, StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<ICsxNode> GetAncestors(ICsxNode node)
        {
                yield return node.Parent;
        }
    }
}
