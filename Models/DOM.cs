using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Io;
using AngleSharp.Text;
using Constellations;
using DotJS;

namespace Vibe
{
    public class CsxDocument : IHtmlDocument
    {
        public IBrowsingContext BrowsingContext {get;set;}
        public IHtmlDocument _innerDocument;
        public CsxNode Root { get; set; }
        public JS? JS { get; set; } = null;

        public CsxDocument(IBrowsingContext context, dynamic? rootNode)
        {
            BrowsingContext = context;
            Task.Run(async () =>
            {
                Root = rootNode ?? new CsxNode("html");
                if (rootNode is null)
                {
                    Root.AddChild(new CsxNode("head"));
                    Root.AddChild(new CsxNode("body"));
                }

                }).Wait();
        }
            // Function to send updates to the client via JS invocation

        #region Events
        public event DomEventHandler ReadyStateChanged
        {
            add
            {
                _innerDocument.ReadyStateChanged += value;
            }

            remove
            {
                _innerDocument.ReadyStateChanged -= value;
            }
        }

        public event DomEventHandler Aborted
        {
            add
            {
                _innerDocument.Aborted += value;
            }

            remove
            {
                _innerDocument.Aborted -= value;
            }
        }

        public event DomEventHandler Blurred
        {
            add
            {
                _innerDocument.Blurred += value;
            }

            remove
            {
                _innerDocument.Blurred -= value;
            }
        }

        public event DomEventHandler Cancelled
        {
            add
            {
                _innerDocument.Cancelled += value;
            }

            remove
            {
                _innerDocument.Cancelled -= value;
            }
        }

        public event DomEventHandler CanPlay
        {
            add
            {
                _innerDocument.CanPlay += value;
            }

            remove
            {
                _innerDocument.CanPlay -= value;
            }
        }

        public event DomEventHandler CanPlayThrough
        {
            add
            {
                _innerDocument.CanPlayThrough += value;
            }

            remove
            {
                _innerDocument.CanPlayThrough -= value;
            }
        }

        public event DomEventHandler Changed
        {
            add
            {
                _innerDocument.Changed += value;
            }

            remove
            {
                _innerDocument.Changed -= value;
            }
        }

        public event DomEventHandler Clicked
        {
            add
            {
                _innerDocument.Clicked += value;
            }

            remove
            {
                _innerDocument.Clicked -= value;
            }
        }

        public event DomEventHandler CueChanged
        {
            add
            {
                _innerDocument.CueChanged += value;
            }

            remove
            {
                _innerDocument.CueChanged -= value;
            }
        }

        public event DomEventHandler DoubleClick
        {
            add
            {
                _innerDocument.DoubleClick += value;
            }

            remove
            {
                _innerDocument.DoubleClick -= value;
            }
        }

        public event DomEventHandler Drag
        {
            add
            {
                _innerDocument.Drag += value;
            }

            remove
            {
                _innerDocument.Drag -= value;
            }
        }

        public event DomEventHandler DragEnd
        {
            add
            {
                _innerDocument.DragEnd += value;
            }

            remove
            {
                _innerDocument.DragEnd -= value;
            }
        }

        public event DomEventHandler DragEnter
        {
            add
            {
                _innerDocument.DragEnter += value;
            }

            remove
            {
                _innerDocument.DragEnter -= value;
            }
        }

        public event DomEventHandler DragExit
        {
            add
            {
                _innerDocument.DragExit += value;
            }

            remove
            {
                _innerDocument.DragExit -= value;
            }
        }

        public event DomEventHandler DragLeave
        {
            add
            {
                _innerDocument.DragLeave += value;
            }

            remove
            {
                _innerDocument.DragLeave -= value;
            }
        }

        public event DomEventHandler DragOver
        {
            add
            {
                _innerDocument.DragOver += value;
            }

            remove
            {
                _innerDocument.DragOver -= value;
            }
        }

        public event DomEventHandler DragStart
        {
            add
            {
                _innerDocument.DragStart += value;
            }

            remove
            {
                _innerDocument.DragStart -= value;
            }
        }

        public event DomEventHandler Dropped
        {
            add
            {
                _innerDocument.Dropped += value;
            }

            remove
            {
                _innerDocument.Dropped -= value;
            }
        }

        public event DomEventHandler DurationChanged
        {
            add
            {
                _innerDocument.DurationChanged += value;
            }

            remove
            {
                _innerDocument.DurationChanged -= value;
            }
        }

        public event DomEventHandler Emptied
        {
            add
            {
                _innerDocument.Emptied += value;
            }

            remove
            {
                _innerDocument.Emptied -= value;
            }
        }

        public event DomEventHandler Ended
        {
            add
            {
                _innerDocument.Ended += value;
            }

            remove
            {
                _innerDocument.Ended -= value;
            }
        }

        public event DomEventHandler Error
        {
            add
            {
                _innerDocument.Error += value;
            }

            remove
            {
                _innerDocument.Error -= value;
            }
        }

        public event DomEventHandler Focused
        {
            add
            {
                _innerDocument.Focused += value;
            }

            remove
            {
                _innerDocument.Focused -= value;
            }
        }

        public event DomEventHandler Input
        {
            add
            {
                _innerDocument.Input += value;
            }

            remove
            {
                _innerDocument.Input -= value;
            }
        }

        public event DomEventHandler Invalid
        {
            add
            {
                _innerDocument.Invalid += value;
            }

            remove
            {
                _innerDocument.Invalid -= value;
            }
        }

        public event DomEventHandler KeyDown
        {
            add
            {
                _innerDocument.KeyDown += value;
            }

            remove
            {
                _innerDocument.KeyDown -= value;
            }
        }

        public event DomEventHandler KeyPress
        {
            add
            {
                _innerDocument.KeyPress += value;
            }

            remove
            {
                _innerDocument.KeyPress -= value;
            }
        }

        public event DomEventHandler KeyUp
        {
            add
            {
                _innerDocument.KeyUp += value;
            }

            remove
            {
                _innerDocument.KeyUp -= value;
            }
        }

        public event DomEventHandler Loaded
        {
            add
            {
                _innerDocument.Loaded += value;
            }

            remove
            {
                _innerDocument.Loaded -= value;
            }
        }

        public event DomEventHandler LoadedData
        {
            add
            {
                _innerDocument.LoadedData += value;
            }

            remove
            {
                _innerDocument.LoadedData -= value;
            }
        }

        public event DomEventHandler LoadedMetadata
        {
            add
            {
                _innerDocument.LoadedMetadata += value;
            }

            remove
            {
                _innerDocument.LoadedMetadata -= value;
            }
        }

        public event DomEventHandler Loading
        {
            add
            {
                _innerDocument.Loading += value;
            }

            remove
            {
                _innerDocument.Loading -= value;
            }
        }

        public event DomEventHandler MouseDown
        {
            add
            {
                _innerDocument.MouseDown += value;
            }

            remove
            {
                _innerDocument.MouseDown -= value;
            }
        }

        public event DomEventHandler MouseEnter
        {
            add
            {
                _innerDocument.MouseEnter += value;
            }

            remove
            {
                _innerDocument.MouseEnter -= value;
            }
        }

        public event DomEventHandler MouseLeave
        {
            add
            {
                _innerDocument.MouseLeave += value;
            }

            remove
            {
                _innerDocument.MouseLeave -= value;
            }
        }

        public event DomEventHandler MouseMove
        {
            add
            {
                _innerDocument.MouseMove += value;
            }

            remove
            {
                _innerDocument.MouseMove -= value;
            }
        }

        public event DomEventHandler MouseOut
        {
            add
            {
                _innerDocument.MouseOut += value;
            }

            remove
            {
                _innerDocument.MouseOut -= value;
            }
        }

        public event DomEventHandler MouseOver
        {
            add
            {
                _innerDocument.MouseOver += value;
            }

            remove
            {
                _innerDocument.MouseOver -= value;
            }
        }

        public event DomEventHandler MouseUp
        {
            add
            {
                _innerDocument.MouseUp += value;
            }

            remove
            {
                _innerDocument.MouseUp -= value;
            }
        }

        public event DomEventHandler MouseWheel
        {
            add
            {
                _innerDocument.MouseWheel += value;
            }

            remove
            {
                _innerDocument.MouseWheel -= value;
            }
        }

        public event DomEventHandler Paused
        {
            add
            {
                _innerDocument.Paused += value;
            }

            remove
            {
                _innerDocument.Paused -= value;
            }
        }

        public event DomEventHandler Played
        {
            add
            {
                _innerDocument.Played += value;
            }

            remove
            {
                _innerDocument.Played -= value;
            }
        }

        public event DomEventHandler Playing
        {
            add
            {
                _innerDocument.Playing += value;
            }

            remove
            {
                _innerDocument.Playing -= value;
            }
        }

        public event DomEventHandler Progress
        {
            add
            {
                _innerDocument.Progress += value;
            }

            remove
            {
                _innerDocument.Progress -= value;
            }
        }

        public event DomEventHandler RateChanged
        {
            add
            {
                _innerDocument.RateChanged += value;
            }

            remove
            {
                _innerDocument.RateChanged -= value;
            }
        }

        public event DomEventHandler Resetted
        {
            add
            {
                _innerDocument.Resetted += value;
            }

            remove
            {
                _innerDocument.Resetted -= value;
            }
        }

        public event DomEventHandler Resized
        {
            add
            {
                _innerDocument.Resized += value;
            }

            remove
            {
                _innerDocument.Resized -= value;
            }
        }

        public event DomEventHandler Scrolled
        {
            add
            {
                _innerDocument.Scrolled += value;
            }

            remove
            {
                _innerDocument.Scrolled -= value;
            }
        }

        public event DomEventHandler Seeked
        {
            add
            {
                _innerDocument.Seeked += value;
            }

            remove
            {
                _innerDocument.Seeked -= value;
            }
        }

        public event DomEventHandler Seeking
        {
            add
            {
                _innerDocument.Seeking += value;
            }

            remove
            {
                _innerDocument.Seeking -= value;
            }
        }

        public event DomEventHandler Selected
        {
            add
            {
                _innerDocument.Selected += value;
            }

            remove
            {
                _innerDocument.Selected -= value;
            }
        }

        public event DomEventHandler Shown
        {
            add
            {
                _innerDocument.Shown += value;
            }

            remove
            {
                _innerDocument.Shown -= value;
            }
        }

        public event DomEventHandler Stalled
        {
            add
            {
                _innerDocument.Stalled += value;
            }

            remove
            {
                _innerDocument.Stalled -= value;
            }
        }

        public event DomEventHandler Submitted
        {
            add
            {
                _innerDocument.Submitted += value;
            }

            remove
            {
                _innerDocument.Submitted -= value;
            }
        }

        public event DomEventHandler Suspended
        {
            add
            {
                _innerDocument.Suspended += value;
            }

            remove
            {
                _innerDocument.Suspended -= value;
            }
        }

        public event DomEventHandler TimeUpdated
        {
            add
            {
                _innerDocument.TimeUpdated += value;
            }

            remove
            {
                _innerDocument.TimeUpdated -= value;
            }
        }

        public event DomEventHandler Toggled
        {
            add
            {
                _innerDocument.Toggled += value;
            }

            remove
            {
                _innerDocument.Toggled -= value;
            }
        }

        public event DomEventHandler VolumeChanged
        {
            add
            {
                _innerDocument.VolumeChanged += value;
            }

            remove
            {
                _innerDocument.VolumeChanged -= value;
            }
        }

        public event DomEventHandler Waiting
        {
            add
            {
                _innerDocument.Waiting += value;
            }

            remove
            {
                _innerDocument.Waiting -= value;
            }
        }
#endregion

        public IElement DocumentElement => _innerDocument.DocumentElement;
        public void Initialize(XServerContext context){

            var parser = new HtmlParser();
            _innerDocument = (IHtmlDocument)BrowsingContext.OpenNewAsync().Result;
            Location.PathName = context.Request.Url.AbsolutePath;
            Location.Search = context.Request.Url.Query;
            Location.Hash = context.Request.Url.Fragment;
            Location.Href = context.Request.Url.AbsoluteUri;
            Root = Root.SetContext(context).SetDocument(this).Render();
            _innerDocument.DocumentElement.Remove();
            _innerDocument.AppendChild(Root.Element);
            AttachMutationObserver(Root.Element);
        }
        public void AttachMutationObserver(IElement element)
        {
            var observer = new MutationObserver(async (mutations, x) =>
            {
                foreach (var mutation in mutations)
                {
                    // Handle added nodes
                    if (mutation.Type == "childList")
                    {
                        if(mutation.Added is not null){
                            foreach (var addedNode in mutation.Added)
                            {
                                if (addedNode is IElement element && element.HasAttribute("xid"))
                                {
                                    var targetXid = element.GetAttribute("xid");
                                    await SendUpdateToClient("nodeAdded",
                                                targetXid,
                                                (addedNode as IElement).OuterHtml,
                                                "",
                                                ((mutation.Target as IElement).Parent as IElement).GetAttribute("xid")?? "",
                                                (mutation.PreviousSibling as IElement).GetAttribute("xid")?? "",
                                                (mutation.NextSibling as IElement).GetAttribute("xid")?? "");
                                }
                            }
                        }

                        // Handle removed nodes
                        if(mutation.Removed is not null){
                            foreach (var removedNode in mutation.Removed)
                            {
                                if (removedNode is IElement element && element.HasAttribute("xid"))
                                {
                                    var targetXid = element.GetAttribute("xid");
                                    await SendUpdateToClient("nodeRemoved", targetXid, "");
                                }
                            }
                        }
                    }

                    // Handle attribute changes
                    if (mutation.Type == "attributes")
                    {
                        var target = mutation.Target as IElement;
                        if (target != null && target.HasAttribute("xid"))
                        {
                            var targetXid = target.GetAttribute("xid");
                            var attributeName = mutation.AttributeName;
                            var newValue = target.GetAttribute(attributeName);
                            await SendUpdateToClient("attributeChanged", targetXid, newValue, attributeName);
                        }
                    }

                    // Handle text changes
                    if (mutation.Type == "characterData")
                    {
                        var target = mutation.Target as IElement;
                        if (target != null && target.HasAttribute("xid"))
                        {
                            var targetXid = target.GetAttribute("xid");
                            await SendUpdateToClient("textChanged", targetXid, target.TextContent);
                        }
                    }
                }

            });
            // Start observing the entire document or root node for mutations
                observer.Connect(element,
                        true, true, true, true
                );
                connectChildren(element);
                void connectChildren(IElement element){
                    foreach(var ce in element.Children){
                        observer.Connect(ce,
                        true,true,true,true);
                        connectChildren(ce);
                    }
                }
        }

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

            // Invoke the JavaScript function that handles DOM updates on the client side
            if (JS != null)
            {
                await JS.InvokeAsync("window.updateClientSideDom", [updatePayload]);
            }
        }

        // Custom CsxNode-based createElement
        public IElement createElement(string tagName)
        {
            var element = _innerDocument.CreateElement(tagName) as IElement;

            if (element == null)
                throw new InvalidOperationException("Failed to create element.");

            return element;
        }

        public IText createTextNode(string text)
        {
            var textNode = _innerDocument.CreateTextNode(text);

            return textNode;
        }

        public IComment createComment(string comment)
        {
            var commentNode = _innerDocument.CreateComment(comment);

            return commentNode;
        }

        public void appendChild(dynamic parent, dynamic child)
        {
            var parentElement = parent.Element as INode;
            var childElement = child.Element as INode;

            if (parentElement == null || childElement == null)
                throw new InvalidOperationException("Parent or child element is not valid.");

            parentElement.AppendChild(childElement);
            ((List<CsxNode>)parent.ChildNodes).Add(child);
        }

        public void removeChild(dynamic parent, dynamic child)
        {
            var parentElement = parent.Element as INode;
            var childElement = child.Element as INode;

            if (parentElement == null || childElement == null)
                throw new InvalidOperationException("Parent or child element is not valid.");

            parentElement.RemoveChild(childElement);
            ((List<CsxNode>)parent.ChildNodes).Remove(child);
        }

        public void setAttribute(CsxNode node, string name, dynamic value)
        {
            var element = node.Element as IHtmlElement;

            if (element == null)
                throw new InvalidOperationException("Node is not a valid element.");

            element.SetAttribute(name, value);
            ((Dictionary<string, object>)node.Attributes)[name] = value;
        }

        public string getAttribute(CsxNode node, string name)
        {
            return node.GetAttribute(name);
        }

        public void removeAttribute(CsxNode node, string name)
        {
            var element = node.Element as IHtmlElement;

            if (element == null)
                throw new InvalidOperationException("Node is not a valid element.");

            element.RemoveAttribute(name);
            ((Dictionary<string, object>)node.Attributes).Remove(name);
        }

        #region IHtmlDocument Implementation

        public IElement ActiveElement => _innerDocument.ActiveElement;

        public string CharacterSet => _innerDocument.CharacterSet;

        public string ContentType => _innerDocument.ContentType;

        public string CompatMode => _innerDocument.CompatMode;

        public string DesignMode
        {
            get => _innerDocument.DesignMode;
            set => _innerDocument.DesignMode = value;
        }

        public IHtmlAllCollection All => _innerDocument.All;

        public IHtmlCollection<IHtmlAnchorElement> Anchors => _innerDocument.Anchors;

        public IHtmlCollection<IHtmlFormElement> Forms => _innerDocument.Forms;

        public IHtmlCollection<IHtmlImageElement> Images => _innerDocument.Images;

        public IHtmlCollection<IHtmlScriptElement> Scripts => _innerDocument.Scripts;

        public string Title
        {
            get => _innerDocument.Title;
            set => _innerDocument.Title = value;
        }

        public IImplementation Implementation => _innerDocument.Implementation;

        public string? Direction { get => _innerDocument.Direction; set => _innerDocument.Direction = value; }

        public string DocumentUri => _innerDocument.DocumentUri;

        public string Url => _innerDocument.Url;

        public IDocumentType Doctype => _innerDocument.Doctype;

        public string? LastModified => _innerDocument.LastModified;

        public DocumentReadyState ReadyState => _innerDocument.ReadyState;

        public ILocation Location => _innerDocument.Location;

        public IHtmlCollection<IHtmlEmbedElement> Plugins => _innerDocument.Plugins;

        public IHtmlCollection<IElement> Commands => _innerDocument.Commands;

        public IHtmlCollection<IElement> Links => _innerDocument.Links;

        public IHtmlHeadElement? Head => _innerDocument.Head;

        public IHtmlElement? Body { get => _innerDocument.Body; set => _innerDocument.Body = value; }
        public string Cookie { get => _innerDocument.Cookie; set => _innerDocument.Cookie = value; }

        public string? Origin => _innerDocument.Origin;

        public string Domain { get => _innerDocument.Domain; set => _innerDocument.Domain = value; }

        public string? Referrer => _innerDocument.Referrer;

        public IHtmlScriptElement? CurrentScript => _innerDocument.CurrentScript;

        public IWindow? DefaultView => _innerDocument.DefaultView;

        public IBrowsingContext Context => _innerDocument.Context;

        public IDocument? ImportAncestor => _innerDocument.ImportAncestor;

        public TextSource Source => _innerDocument.Source;

        public HttpStatusCode StatusCode => _innerDocument.StatusCode;

        public IEntityProvider Entities => _innerDocument.Entities;

        public string BaseUri => _innerDocument.BaseUri;

        public Url? BaseUrl => _innerDocument.BaseUrl;

        public string NodeName => _innerDocument.NodeName;

        public INodeList ChildNodes => _innerDocument.ChildNodes;

        public IDocument? Owner => _innerDocument.Owner;

        public IElement? ParentElement => _innerDocument.ParentElement;

        public INode? Parent => _innerDocument.Parent;

        public INode? FirstChild => _innerDocument.FirstChild;

        public INode? LastChild => _innerDocument.LastChild;

        public INode? NextSibling => _innerDocument.NextSibling;

        public INode? PreviousSibling => _innerDocument.PreviousSibling;

        public NodeType NodeType => _innerDocument.NodeType;

        public string NodeValue { get => _innerDocument.NodeValue; set => _innerDocument.NodeValue = value; }
        public string TextContent { get => _innerDocument.TextContent; set => _innerDocument.TextContent = value; }

        public bool HasChildNodes => _innerDocument.HasChildNodes;

        public NodeFlags Flags => _innerDocument.Flags;

        public IHtmlCollection<IElement> Children => _innerDocument.Children;

        public IElement? FirstElementChild => _innerDocument.FirstElementChild;

        public IElement? LastElementChild => _innerDocument.LastElementChild;

        public int ChildElementCount => _innerDocument.ChildElementCount;

        public IStyleSheetList StyleSheets => _innerDocument.StyleSheets;

        public string? SelectedStyleSheetSet { get => _innerDocument.SelectedStyleSheetSet; set => _innerDocument.SelectedStyleSheetSet = value; }

        public string? LastStyleSheetSet => _innerDocument.LastStyleSheetSet;

        public string? PreferredStyleSheetSet => _innerDocument.PreferredStyleSheetSet;

        public IStringList StyleSheetSets => _innerDocument.StyleSheetSets;

        public IElement GetElementById(string id) => _innerDocument.GetElementById(id);

        public IHtmlCollection<IElement> GetElementsByClassName(string classNames) =>
            _innerDocument.GetElementsByClassName(classNames);

        public IHtmlCollection<IElement> GetElementsByTagName(string tagName) =>
            _innerDocument.GetElementsByTagName(tagName);

        public IHtmlCollection<IElement> QuerySelectorAll(string selectors) =>
            _innerDocument.QuerySelectorAll(selectors);

        public IElement QuerySelector(string selectors) => _innerDocument.QuerySelector(selectors);

        public IDocument Open(string type = "text/html", string? replace = null)
        {
            return _innerDocument.Open(type, replace);
        }

        public void Close()
        {
            _innerDocument.Close();
        }

        public void Write(string content)
        {
            _innerDocument.Write(content);
        }

        public void WriteLine(string content)
        {
            _innerDocument.WriteLine(content);
        }

        public void Load(string url)
        {
            _innerDocument.Load(url);
        }

        public IHtmlCollection<IElement> GetElementsByName(string name)
        {
            return _innerDocument.GetElementsByName(name);
        }

        public IHtmlCollection<IElement> GetElementsByTagName(string? namespaceUri, string tagName)
        {
            return _innerDocument.GetElementsByTagName(namespaceUri, tagName);
        }

        public Event CreateEvent(string type)
        {
            return _innerDocument.CreateEvent(type);
        }

        public IRange CreateRange()
        {
            return _innerDocument.CreateRange();
        }

        public IComment CreateComment(string data)
        {
            return _innerDocument.CreateComment(data);
        }

        public IDocumentFragment CreateDocumentFragment()
        {
            return _innerDocument.CreateDocumentFragment();
        }

        public IElement CreateElement(string name)
        {
            IElement elem;

            elem = _innerDocument.CreateElement(string.IsNullOrEmpty(name)? "html": name);

            return elem;
        }

        public IElement CreateElement(string? namespaceUri, string name)
        {
            return _innerDocument.CreateElement(namespaceUri, name);
        }

        public IAttr CreateAttribute(string name)
        {
            return _innerDocument.CreateAttribute(name);
        }

        public IAttr CreateAttribute(string? namespaceUri, string name)
        {
            return _innerDocument.CreateAttribute(namespaceUri, name);
        }

        public IProcessingInstruction CreateProcessingInstruction(string target, string data)
        {
            return _innerDocument.CreateProcessingInstruction(target, data);
        }

        public IText CreateTextNode(string data)
        {
            return _innerDocument.CreateTextNode(data);
        }

        public INodeIterator CreateNodeIterator(INode root, FilterSettings settings = FilterSettings.All, NodeFilter? filter = null)
        {
            return _innerDocument.CreateNodeIterator(root, settings, filter);
        }

        public ITreeWalker CreateTreeWalker(INode root, FilterSettings settings = FilterSettings.All, NodeFilter? filter = null)
        {
            return _innerDocument.CreateTreeWalker(root, settings, filter);
        }

        public INode Import(INode externalNode, bool deep = true)
        {
            return _innerDocument.Import(externalNode, deep);
        }

        public INode Adopt(INode externalNode)
        {
            return _innerDocument.Adopt(externalNode);
        }

        public bool HasFocus()
        {
            return _innerDocument.HasFocus();
        }

        public bool ExecuteCommand(string commandId, bool showUserInterface = false, string value = "")
        {
            return _innerDocument.ExecuteCommand(commandId, showUserInterface, value);
        }

        public bool IsCommandEnabled(string commandId)
        {
            return _innerDocument.IsCommandEnabled(commandId);
        }

        public bool IsCommandIndeterminate(string commandId)
        {
            return _innerDocument.IsCommandIndeterminate(commandId);
        }

        public bool IsCommandExecuted(string commandId)
        {
            return _innerDocument.IsCommandExecuted(commandId);
        }

        public bool IsCommandSupported(string commandId)
        {
            return _innerDocument.IsCommandSupported(commandId);
        }

        public string? GetCommandValue(string commandId)
        {
            return _innerDocument.GetCommandValue(commandId);
        }

        public bool AddImportUrl(Uri uri)
        {
            return _innerDocument.AddImportUrl(uri);
        }

        public bool HasImported(Uri uri)
        {
            return _innerDocument.HasImported(uri);
        }

        public INode Clone(bool deep = true)
        {
            return _innerDocument.Clone(deep);
        }

        public bool Equals(INode otherNode)
        {
            return _innerDocument.Equals(otherNode);
        }

        public DocumentPositions CompareDocumentPosition(INode otherNode)
        {
            return _innerDocument.CompareDocumentPosition(otherNode);
        }

        public void Normalize()
        {
            _innerDocument.Normalize();
        }

        public bool Contains(INode otherNode)
        {
            return _innerDocument.Contains(otherNode);
        }

        public bool IsDefaultNamespace(string namespaceUri)
        {
            return _innerDocument.IsDefaultNamespace(namespaceUri);
        }

        public string? LookupNamespaceUri(string prefix)
        {
            return _innerDocument.LookupNamespaceUri(prefix);
        }

        public string? LookupPrefix(string? namespaceUri)
        {
            return _innerDocument.LookupPrefix(namespaceUri);
        }

        public INode AppendChild(INode child)
        {
            return _innerDocument.AppendChild(child);
        }

        public INode InsertBefore(INode newElement, INode? referenceElement)
        {
            return _innerDocument.InsertBefore(newElement, referenceElement);
        }

        public INode RemoveChild(INode child)
        {
            return _innerDocument.RemoveChild(child);
        }

        public INode ReplaceChild(INode newChild, INode oldChild)
        {
            return _innerDocument.ReplaceChild(newChild, oldChild);
        }

        public void AddEventListener(string type, DomEventHandler? callback = null, bool capture = false)
        {
            _innerDocument.AddEventListener(type, callback, capture);
        }

        public void RemoveEventListener(string type, DomEventHandler? callback = null, bool capture = false)
        {
            _innerDocument.RemoveEventListener(type, callback, capture);
        }

        public void InvokeEventListener(Event ev)
        {
            _innerDocument.InvokeEventListener(ev);
        }

        public bool Dispatch(Event ev)
        {
            return _innerDocument.Dispatch(ev);
        }

        public void ToHtml(TextWriter writer, IMarkupFormatter formatter)
        {
            _innerDocument.ToHtml(writer, formatter);
        }

        public void Append(params INode[] nodes)
        {
            _innerDocument.Append(nodes);
        }

        public void Prepend(params INode[] nodes)
        {
            _innerDocument.Prepend(nodes);
        }

        public void EnableStyleSheetsForSet(string name)
        {
            _innerDocument.EnableStyleSheetsForSet(name);
        }

        public void Dispose()
        {
            _innerDocument.Dispose();
        }

        #endregion
    }
}
