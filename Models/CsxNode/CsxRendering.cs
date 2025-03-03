using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Vibe
{
    public partial class CsxNode : IDisposable
    {
        public int index { get; set; }
        public CsxDocument? document { get; set; }
        public XServerContext? Context { get; set; }
        public ObservableCollection<dynamic> Children { get; set; } = new ObservableCollection<dynamic>();
        
        public string? Id { get; private set; }
        public ICsxNode SetIndex(int index)
        {
            this.index = index;
            return this;
        }
        public ICsxNode SetDocument(CsxDocument? doc)
        {
            document = doc;
            return this;
        }
        public ICsxNode SetParent(ICsxNode parent)
        {
            Parent = parent;
            return this;
        }
        public ICsxNode SetContext(XServerContext? context)
        {
            Context = context;
            return this;
        }
        public virtual void OnRender()
        {

        }

        public virtual ICsxNode Render()
        {
            if (ShouldRender && document != null && Properties.ContainsKey("TagName"))
            {
                Rendering = true;
                _handlers.Clear();
                if (FirstRender)
                {
                    OnInitialized();
                }
                FirstRender = false;

                var element = this;
                // Convert values to the Element
                this.SetAttribute("xid", Xid);

                Children.Clear();
                OnRender();
                if (!Context.HasReplied)
                {
                    // Pre-Processor
                    foreach (var child in ChildNodes)
                    {
                        switch (child)
                        {
                            case Action<ICsxNode> action:
                                action(this);
                                break;
                        }
                    }

                    // Add more attribute conversions as needed
                    foreach (var attribute in Attributes)
                    {
                        switch (attribute.Value)
                        {
                            case Delegate att:
                                switch (att)
                                {
                                    case Func<string> s:
                                        element.SetLiveAttribute(attribute.Key, s());
                                        break;
                                    case Func<int> i:
                                        element.SetLiveAttribute(attribute.Key, i().ToString());
                                        break;
                                    case Func<float> f:
                                        element.SetLiveAttribute(attribute.Key, f().ToString());
                                        break;
                                    case Func<double> d:
                                        element.SetLiveAttribute(attribute.Key, d().ToString());
                                        break;
                                    case Func<bool> b:
                                        element.SetLiveAttribute(attribute.Key, b().ToString());
                                        break;
                                    default:
                                        break;
                                }
                                break;

                            case string:
                                element.SetLiveAttribute(attribute.Key, attribute.Value as string);
                                break;
                            case int:
                                element.SetLiveAttribute(attribute.Key, attribute.Value.ToString());
                                break;
                            case float:
                                element.SetLiveAttribute(attribute.Key, attribute.Value.ToString());
                                break;
                            case double:
                                element.SetLiveAttribute(attribute.Key, attribute.Value.ToString());
                                break;
                            case bool:
                                element.SetLiveAttribute(attribute.Key, attribute.Value.ToString());
                                break;
                            default:
                                break;
                        }
                        if (attribute.Key.StartsWith("@On"))
                        {
                            var eventName = attribute.Key.Replace("@On", "");
                            element.on(eventName, attribute.Value);
                            document.AddSessionEvent(Xid, eventName);
                        }
                    }
                    var transformedNodes = new List<object?>(ChildNodes);
                    foreach (var c in ChildNodes)
                    {
                        switch (c)
                        {
                            case Delegate f:
                                switch (f)
                                {
                                    case Action<ICsxNode> action:
                                        break;
                                    case Func<ICsxNode> cn:
                                        element.Children.Add(cn().SetContext(Context).SetDocument(document).SetParent(this).Render());
                                        break;
                                    case Func<IEnumerable<ICsxNode>> xnodes:
                                        foreach (var x in xnodes())
                                        {
                                            element.Children.Add(x.SetContext(Context).SetDocument(document).SetParent(this).Render());
                                        }
                                        break;
                                    case Func<IEnumerable<object>> objects:
                                        foreach(var ob in objects())
                                        {
                                            switch (ob)
                                            {
                                                case ICsxNode ic:
                                                    element.Children.Add(ic.SetContext(Context).SetDocument(document).SetParent(this).Render());
                                                    break;
                                                case IEnumerable<ICsxNode>:
                                                    foreach (var x in c as IEnumerable<ICsxNode>)
                                                    {
                                                        element.Children.Add(x.SetContext(Context).SetDocument(document).SetParent(this).Render());
                                                    }
                                                    break;
                                                case string cs:
                                                    element.Children.Add(cs);
                                                    break;
                                                case bool cb:
                                                    element.Children.Add(cb.ToString());
                                                    break;
                                                case int ci:
                                                    element.Children.Add(ci.ToString());
                                                    break;
                                                case decimal cd:
                                                    element.Children.Add(cd.ToString());
                                                    break;
                                                case float cf:
                                                    element.Children.Add(cf.ToString());
                                                    break;
                                                case long cl:
                                                    element.Children.Add(cl.ToString());
                                                    break;
                                                case Func<ICsxNode> cn:
                                                    element.Children.Add(cn().SetContext(Context).SetDocument(document).SetParent(this).Render());
                                                    break;
                                                case Func<IEnumerable<ICsxNode>> xnodes:
                                                    foreach (var x in xnodes())
                                                    {
                                                        element.Children.Add(x.SetContext(Context).SetDocument(document).SetParent(this).Render());
                                                    }
                                                    break;
                                                case Func<string> s:
                                                    element.Children.Add(s());
                                                    break;
                                                case Func<bool> b:
                                                    element.Children.Add(b().ToString());
                                                    break;
                                                case Func<int> i:
                                                    element.Children.Add(i().ToString());
                                                    break;
                                                case Func<float> fl:
                                                    element.Children.Add(fl().ToString());
                                                    break;
                                                case Func<double> d:
                                                    element.Children.Add(d().ToString());
                                                    break;
                                                case Func<decimal> dec:
                                                    element.Children.Add(dec().ToString());
                                                    break;
                                                case Func<long> l:
                                                    element.Children.Add(l().ToString());
                                                    break;
                                                case Func<dynamic> dn:
                                                    element.Children.Add(dn().SetContext(Context).SetDocument(document).SetParent(this).Render());
                                                    break;
                                            }
                                        }
                                        break;

                                    case Func<string> s:
                                        element.Children.Add(s());
                                        break;
                                    case Func<bool> b:
                                        element.Children.Add(b().ToString());
                                        break;
                                    case Func<int> i:
                                        element.Children.Add(i().ToString());
                                        break;
                                    case Func<float> fl:
                                        element.Children.Add(fl().ToString());
                                        break;
                                    case Func<double> d:
                                        element.Children.Add(d().ToString());
                                        break;
                                    case Func<decimal> dec:
                                        element.Children.Add(dec().ToString());
                                        break;
                                    case Func<long> l:
                                        element.Children.Add(l().ToString());
                                        break;
                                    case Func<dynamic> dn:
                                        element.Children.Add(dn().SetContext(Context).SetDocument(document).SetParent(this).Render());
                                        break;
                                }
                                break;
                            case ICsxNode ic:
                                element.Children.Add(ic.SetContext(Context).SetDocument(document).SetParent(this).Render());
                                break;
                            case IEnumerable<ICsxNode>:
                                foreach (var x in c as IEnumerable<ICsxNode>)
                                {
                                    element.Children.Add(x.SetContext(Context).SetDocument(document).SetParent(this).Render());
                                }
                                break;

                            case IEnumerable<object> objects:
                                foreach(var ob in objects)
                                {
                                    switch (ob)
                                    {
                                        case ICsxNode ic:
                                            element.Children.Add(ic.SetContext(Context).SetDocument(document).SetParent(this).Render());
                                            break;
                                        case IEnumerable<ICsxNode>:
                                            foreach (var x in c as IEnumerable<ICsxNode>)
                                            {
                                                element.Children.Add(x.SetContext(Context).SetDocument(document).SetParent(this).Render());
                                            }
                                            break;
                                        case string cs:
                                            element.Children.Add(cs);
                                            break;
                                        case bool cb:
                                            element.Children.Add(cb.ToString());
                                            break;
                                        case int ci:
                                            element.Children.Add(ci.ToString());
                                            break;
                                        case decimal cd:
                                            element.Children.Add(cd.ToString());
                                            break;
                                        case float cf:
                                            element.Children.Add(cf.ToString());
                                            break;
                                        case long cl:
                                            element.Children.Add(cl.ToString());
                                            break;
                                        case Func<ICsxNode> cn:
                                            element.Children.Add(cn().SetContext(Context).SetDocument(document).SetParent(this).Render());
                                            break;
                                        case Func<IEnumerable<ICsxNode>> xnodes:
                                            foreach (var x in xnodes())
                                            {
                                                element.Children.Add(x.SetContext(Context).SetDocument(document).SetParent(this).Render());
                                            }
                                            break;
                                        case Func<string> s:
                                            element.Children.Add(s());
                                            break;
                                        case Func<bool> b:
                                            element.Children.Add(b().ToString());
                                            break;
                                        case Func<int> i:
                                            element.Children.Add(i().ToString());
                                            break;
                                        case Func<float> fl:
                                            element.Children.Add(fl().ToString());
                                            break;
                                        case Func<double> d:
                                            element.Children.Add(d().ToString());
                                            break;
                                        case Func<decimal> dec:
                                            element.Children.Add(dec().ToString());
                                            break;
                                        case Func<long> l:
                                            element.Children.Add(l().ToString());
                                            break;
                                        case Func<dynamic> dn:
                                            element.Children.Add(dn().SetContext(Context).SetDocument(document).SetParent(this).Render());
                                            break;
                                    }
                                }

                                break;
                            default:
                                switch (c)
                                {
                                    case string cs:
                                        element.Children.Add(cs);
                                        break;
                                    case bool cb:
                                        element.Children.Add(cb.ToString());
                                        break;
                                    case int ci:
                                        element.Children.Add(ci.ToString());
                                        break;
                                    case decimal cd:
                                        element.Children.Add(cd.ToString());
                                        break;
                                    case float cf:
                                        element.Children.Add(cf.ToString());
                                        break;
                                    case long cl:
                                        element.Children.Add(cl.ToString());
                                        break;
                                }
                                break;
                        }
                    }
                }
                Rendering = false;

                OnAfterRender();
                _ = Task.Run(async () => await document.SendUpdateToClient(
                    "Render",
                    Xid,
                    this.ToString(), "", 
                    (Parent is null)? "":Parent.Xid
                    ));
            }
            return this;
        }
    }
}
