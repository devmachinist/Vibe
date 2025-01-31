using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using AngleSharp.Html.Dom;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Vibe
{
    public partial class CsxNode
    {
        public CsxDocument? document { get; set; }
        public XServerContext? Context { get; set; }
        
        public string? Id { get; private set; }
        public CsxNode SetDocument(CsxDocument? doc)
        {
            document = doc;
            return this;
        }
        public CsxNode SetParent(CsxNode parent)
        {
            Parent = parent;
            return this;
        }
        public CsxNode SetContext(XServerContext? context)
        {
            Context = context;
            return this;
        }
        public virtual void OnRender()
        {

        }

        public virtual CsxNode Render()
        {
            if (ShouldRender && document != null && _properties.ContainsKey("TagName"))
            {
                _handlers.Clear();
                OnRender();
                // Pre-Processor
                foreach(var child in ChildNodes){
                    switch(child){
                        case Action<CsxNode> action:
                            action.DynamicInvoke(this);
                            break;
                    }
                }
                IElement transformedElement = document.CreateElement(string.IsNullOrEmpty(_properties["TagName"] as string)? "html": _properties["TagName"] as string);
                // Convert values to the Element
                transformedElement.SetAttribute("xid", this.Xid);
                Element = transformedElement;
                try
                {
                    (Parent as CsxNode)?.Element.ReplaceChild(transformedElement, Element);
                }
                catch (AngleSharp.Dom.DomException dx)
                {
                    
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
                                        transformedElement.SetAttribute(attribute.Key, s());
                                    break;
                                case Func<int> i:
                                    transformedElement.SetAttribute(attribute.Key, i().ToString());
                                    break;
                                case Func<float> f:
                                    transformedElement.SetAttribute(attribute.Key, f().ToString());
                                    break;
                                case Func<double> d:
                                    transformedElement.SetAttribute(attribute.Key, d().ToString());
                                    break;
                                case Func<bool> b:
                                    transformedElement.SetAttribute(attribute.Key, b().ToString());
                                    break;
                                default:
                                    break;
                            }
                            break;

                        case string:
                            if(typeof(string).IsAssignableFrom(attribute.Value.GetType()))
                                transformedElement.SetAttribute(attribute.Key, (attribute.Value as string));
                            break;
                        case int:
                            transformedElement.SetAttribute(attribute.Key, attribute.Value.ToString());
                            break;
                        case float:
                            transformedElement.SetAttribute(attribute.Key, attribute.Value.ToString());
                            break;
                        case double:
                            transformedElement.SetAttribute(attribute.Key, attribute.Value.ToString());
                            break;
                        case bool:
                            transformedElement.SetAttribute(attribute.Key, attribute.Value.ToString());
                            break;
                        default:
                            break;
                    }
                    if (attribute.Key.StartsWith("@On"))
                    {
                        void HandleEvent(object sender, Event e)
                        {
                            if(document == null)
                            {
                                _handlers.Clear();
                            }
                            else
                            {
                                emit(e.Type, sender, e);
                            }
                        }

                        var eventName = attribute.Key.Replace("@On", "");
                        off(eventName);
                        on(eventName, attribute.Value);
                        if(Element is not null) Element.RemoveEventListener(eventName, HandleEvent);
                        transformedElement.RemoveEventListener(eventName, HandleEvent);
                        transformedElement.AddEventListener(eventName, HandleEvent);
                    }
                }
                var transformedNodes = new List<object?>(ChildNodes);
                foreach(var c in ChildNodes)
                {
                    switch (c)
                    {
                        case Delegate f:
                            switch (f)
                            {
                                case Action<CsxNode> action:
                                    break;
                                case Func<CsxNode> cn:
                                    transformedElement.Append(cn().SetContext(Context).SetDocument(document).SetParent(this).Render().Element);
                                    break;
                                case Func<IEnumerable<CsxNode>> xnodes:
                                    foreach (var x in xnodes())
                                    {
                                        transformedElement.Append(x.SetContext(Context).SetDocument(document).SetParent(this).Render().Element);
                                    }
                                    break;
                                case Func<string> s:
                                    transformedElement.Append(document.CreateTextNode(s()));
                                    break;
                                case Func<bool> b:
                                    transformedElement.Append(document.CreateTextNode(b().ToString()));
                                    break;
                                case Func<int> i:
                                    transformedElement.Append(document.CreateTextNode(i().ToString()));
                                    break;
                                case Func<float> fl:
                                    transformedElement.Append(document.CreateTextNode(fl().ToString()));
                                    break;
                                case Func<double> d:
                                    transformedElement.Append(document.CreateTextNode(d().ToString()));
                                    break;
                                case Func<decimal> dec:
                                    transformedElement.Append(document.CreateTextNode(dec().ToString()));
                                    break;
                                case Func<long> l:
                                    transformedElement.Append(document.CreateTextNode(l().ToString()));
                                    break;
                                case Func<dynamic> dn:
                                    transformedElement.Append(dn().SetContext(Context).SetDocument(document).SetParent(this).Render().Element);
                                    break;
                            }
                            break;
                        case CsxNode:
                            transformedElement.Append((c as CsxNode).SetContext(Context).SetDocument(document).SetParent(this).Render().Element);
                            break;
                        case IEnumerable<CsxNode>:
                            foreach(var x in c as IEnumerable<CsxNode>)
                            {
                                transformedElement.Append(x.SetContext(Context).SetDocument(document).SetParent(this).Render().Element);
                            }
                            break;
                        default:
                            transformedElement.Append(document.CreateTextNode(c as string?? ""));
                            break;
                    }
                }
                if (Parent as CsxNode == null)
                {
                    document.DocumentElement.Remove();
                    document.AppendChild(transformedElement);
                }
                else
                {
                    if((Parent as CsxNode).Element != null)
                    {
                        var node = (Parent as CsxNode).Element.Children.FirstOrDefault(p => p.GetAttribute($"xid") == Xid);
                        if (node != null)
                        {
                            
                            (Parent as CsxNode).Element.ReplaceChild(transformedElement, node);
                        }
                        else Parent.Element.AppendChild(transformedElement);
                    }
                }
            }
            return this;
        }
    }
}
