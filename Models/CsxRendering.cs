using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            if (ShouldRender && document != null)
            {
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

                // Add more attribute conversions as needed
                foreach (var attribute in this.Attributes)
                {
                    switch (attribute.Value)
                    {
                        case Action:
                            if(typeof(string).IsAssignableFrom(attribute.Value().GetType()))
                                transformedElement.SetAttribute(attribute.Key, attribute.Value() as string);
                            break;
                        case string:
                            if(typeof(string).IsAssignableFrom(attribute.Value.GetType()))
                                transformedElement.SetAttribute(attribute.Key, (attribute.Value as string));
                            break;
                        default:
                            break;
                    }
                    if (attribute.Key.StartsWith("@On"))
                    {
                        var eventName = attribute.Key.Replace("@On", "");
                        off(eventName, attribute.Value);
                        on(eventName, attribute.Value);
                        void Handle(object sender, Event e)
                        {
                            emit(eventName, sender, e);
                        }
                        transformedElement.RemoveEventListener(eventName, Handle);
                        transformedElement.AddEventListener(eventName, Handle);
                    }
                }
                var transformedNodes = new List<object?>();
                foreach(var c in ChildNodes)
                {
                    switch (c)
                    {
                        case Action:
                            var node = (c as Action).DynamicInvoke();
                            if (node is CsxNode)
                            {
                                transformedElement.Append((node as CsxNode).SetContext(Context).SetDocument(document).SetParent(this).Render().Element);
                            }
                            if (node is IEnumerable<CsxNode> xnodes)
                            {
                                foreach (var x in xnodes)
                                {
                                    transformedElement.Append(x.SetContext(Context).SetDocument(document).SetParent(this).Render().Element);
                                }
                            }
                            if (node is string) transformedElement.Append(document.CreateTextNode(node as string?? ""));
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
                    document._innerDocument.RemoveChild(document._innerDocument.FirstChild);
                    document._innerDocument.AppendChild(transformedElement);
                }
                else
                {
                    if((Parent as CsxNode).Element != null)
                    {
                        var node = (Parent as CsxNode)?.Element?.Children.FirstOrDefault(p => p.GetAttribute($"xid") == Xid);
                        if (node != null) (Parent as CsxNode).Element.RemoveChild(node);
                        Parent.Element.AppendChild(transformedElement);
                    }
                }
                Element = transformedElement;
            }
            return this;
        }
    }
}
