using Constellations;
using DotJS;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EonDB;
using Microsoft.Extensions.DependencyInjection;
using AngleSharp.Html.Dom;
using AngleSharp;
using Swan;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AngleSharp.Dom;

namespace Vibe
{
    public class XServer
    {
        public ConcurrentBag<XUser> Users = new();
        public IServiceCollection Services = new ServiceCollection();
        public IServiceProvider ServiceProvider { get; set; }
        public Constellation Constellation {get;set;}
        public List<string> Prefixes {get;set;} = new List<string>();
        public string HostPage { get; set; } = "index.html";
        public string WebRoot { get; set; } = "wwwroot";
        public string XavierRoot { get; set; } = "Components";
        public string ContentRoot { get; set; }
        public string AssemblyName { get; set; }
        public RouteMatchService Router {get; set;} = new RouteMatchService();
        public CsxNode App { get; set; }
        public EonDB.EonDB EonDB { get; set; }
        private SecureHttpListener Listener { get; set; }
        private readonly X509Certificate2 _certificate;
        public TimeSpan SessionTimeout { get; set; } = TimeSpan.Zero;

        public XServer(string hostPage = "Index.xavier", string webRoot = "wwwroot", string certPath = null, string certPassword = null)
        {
            Services = ServiceHub.Services;
            ServiceProvider = Services.BuildServiceProvider();
            ContentRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, WebRoot);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");

            if (!string.IsNullOrEmpty(certPath))
            {
                _certificate = new X509Certificate2(certPath, certPassword);
            }


            var key = Guid.NewGuid().ToString();
            var id = Guid.NewGuid().ToString();
            var constellation = new Constellation("Vibe")
                .AsServer()
                .ListenOn(null, "127.0.0.1", 65124)
                .NoBroadcasting()
                .AllowOrigins("*")
                .SetKey(Guid.NewGuid().ToString());


            Constellation = constellation;
            Constellation.ConnectionClosed += ConnectionClosed;

            var provider = new LocalStorageProvider();
            provider.Initialize(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EonDB"));
            EonDB = new EonDB.EonDB(provider);
        }
        public XServer AddPrefixes(string[] prefixes){
            Prefixes.AddRange(prefixes);
            return this;
        }

        private void ConnectionClosed(NamedClient client)
        {
            XStateManager.RemoveState(client.EncryptionId);    
        }

        public void AddActiveState(State state)
        {
            XStateManager.AddState(state.XUser.Id, state);
        }

        public XServer SetAppComponent(dynamic root)
        {
            if (root is Action)
            {
                App = root();
            }
            else
            {
                App = root as CsxNode;
            }
            return this;
        }

        public async void Start()
        {

            var listener = new SecureHttpListener(ContentRoot, HostPage, XavierRoot, _certificate, Users, SessionTimeout, EonDB, Services, this);
            Constellation.Run();
            Listener = listener;
            foreach (var pfx in Prefixes){
                if (pfx.StartsWith("http"))
                {
                    Listener._listener.Prefixes.Add(pfx);
                }
            }
            Task.Run(async () => { await listener.StartAsync(); }).Wait();
        }

        public class SecureHttpListener
        {
            public HttpListener _listener;
            private readonly string ContentRoot;
            private readonly string HostPage;
            private readonly string XavierRoot;
            private readonly X509Certificate2 _certificate;
            private readonly ConcurrentBag<XUser> Users;
            private readonly TimeSpan SessionTimeout;
            private readonly EonDB.EonDB EonDB;
            private readonly IServiceCollection Services;
            private readonly IServiceProvider ServiceProvider;
            public XServer Server { get; set; }

            public SecureHttpListener(
                string contentRoot,
                string hostPage,
                string xavierRoot,
                X509Certificate2 certificate,
                ConcurrentBag<XUser> users,
                TimeSpan sessionTimeout,
                EonDB.EonDB eonDB,
                IServiceCollection services,
                XServer server)
            {
                ContentRoot = contentRoot;
                HostPage = hostPage;
                XavierRoot = xavierRoot;
                _certificate = certificate;
                Users = users;
                SessionTimeout = sessionTimeout;
                EonDB = eonDB;
                Services = services;
                Server = server;
                ServiceProvider = Services.BuildServiceProvider();
                
                _listener = new HttpListener();

            }

            public async Task StartAsync()
            {
                try
                {
                    _listener.Start();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }

                while (_listener.IsListening)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await _listener.GetContextAsync();
                        Debug.WriteLine(context.Request.Url);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                        continue;
                    }

                    if (context.Request.IsSecureConnection && _certificate != null)
                    {
                        await HandleSecureConnection(context);
                    }
                    else
                    {
                        await HandleRequest(context);
                    }
                }
            }

            private async Task HandleSecureConnection(HttpListenerContext context)
            {
                using (var sslStream = new SslStream(context.Response.OutputStream, false))
                {
                    try
                    {
                        await sslStream.AuthenticateAsServerAsync(_certificate, false, System.Security.Authentication.SslProtocols.Tls12, true);
                        Debug.WriteLine("SSL authentication successful.");
                        await ProcessRequest(context, sslStream);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"SSL Error: {ex}");
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    }
                }
            }

            private async Task HandleRequest(HttpListenerContext context)
            {
                using (var networkStream = context.Response.OutputStream)
                {
                    await ProcessRequest(context, networkStream);
                }
            }

            private async Task ProcessRequest(HttpListenerContext context, Stream outputStream)
            {
                using (var Res = context.Response){

                    XUser user = GetOrCreateSession(context);
                    var router = Server.Router;
                    var state = XStateManager.GetState(user.Id);
                    user.State = state?? new State(Server.App).SetUser(user).UseJs(Server.Constellation);
                    user.State.Document.JS = user.State.js;

                    if(state == null)
                    { 
                        XStateManager.AddState(user.Id, user.State);
                    }
                    else{
                    }

                    var xServerContext = new XServerContext( context, outputStream, user, Server.EonDB );
                    if (string.IsNullOrEmpty(Path.GetExtension(context.Request.Url.AbsolutePath)) && context.Request.Url.AbsolutePath != "/_csx")
                    {

                    }
                    if(context.Request.Url.AbsolutePath == "/_csx")
                    {
                        var js = Encoding.UTF8.GetBytes(user.State.js.Script + "\r\n" + Server.GetXJs());
                        context.Response.ContentType = "application/javascript";
                        context.Response.StatusCode = 200;
                        await outputStream.WriteAsync(js, 0, js.Length);
                        return;
                    }

                    if(xServerContext.HasReplied){
                        return;
                    }
                    string sessionId = user.Id;

                    var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, XavierRoot);
                    var memory = XavierGlobal.Memory;
                    var requestPath = context.Request.Url.AbsolutePath.Trim('/');

                    if (string.IsNullOrEmpty(requestPath))
                    {
                        requestPath = "Index";
                    }

                    var xavierFilePath = Path.Combine(folderPath, requestPath + ".xavier");
                    var indexFilePath = Path.Combine(folderPath, requestPath, requestPath.Replace("/", "") + "Index.xavier");

                    if (File.Exists(xavierFilePath))
                    {
                        var content = File.ReadAllText(xavierFilePath);
                        if (content.Split(Environment.NewLine).FirstOrDefault()?.Trim() != "@page")
                        {
                            await ServeNext();
                            return;
                        }

                        var component = memory.CsxNodes.FirstOrDefault(p => (p as CsxNode).Route == requestPath || (p as dynamic).Route == requestPath) as CsxNode;
                        var userScopedComponent = EonDB.Query<CsxNode>(sessionId, (p) => p.Xid == component.Xid).FirstOrDefault();
                        if (component == null)
                        {
                            await ServeNext();
                            return;
                        }
                        if(userScopedComponent == null)
                        {
                            EonDB.Add(sessionId, component);
                            userScopedComponent = component;
                        }
                        var htmL = userScopedComponent.Content(memory);
                        var js = component.Scripts;
                        string pattern = @"<\s*/\s*body\s*>";
                        var page = Encoding.UTF8.GetBytes(Regex.Replace(String.Join(Environment.NewLine, htmL.Split(Environment.NewLine).Skip(1)), pattern, "<script async type='module'>" + js + "</script></body>", RegexOptions.IgnoreCase));

                        context.Response.ContentType = component.ContentType;
                        context.Response.StatusCode = 200;
                        await outputStream.WriteAsync(page, 0, page.Length);
                        return;
                    }
                    else if (File.Exists(indexFilePath))
                    {
                        var content = File.ReadAllText(indexFilePath);
                        if (content.Split(Environment.NewLine).FirstOrDefault()?.Trim() != "@page")
                        {
                            await ServeNext();
                            return;
                        }

                        var componentName = indexFilePath.Split("/").Last().Replace(".xavier", "");
                        var component = memory.CsxNodes.FirstOrDefault(p => (p as CsxNode).Name == componentName) as CsxNode;

                        if (component == null)
                        {
                            await ServeNext();
                            return;
                        }

                        var htmL = component.Content(memory);
                        var js = component.Scripts;
                        string pattern = @"<\s*/\s*body\s*>";
                        var page = Encoding.UTF8.GetBytes(Regex.Replace(String.Join(Environment.NewLine, htmL.Split(Environment.NewLine).Skip(1)), pattern, "<script async type='module'>" + js + "</script></body>", RegexOptions.IgnoreCase));

                        context.Response.ContentType = "text/html";
                        context.Response.StatusCode = 200;
                        await outputStream.WriteAsync(page, 0, page.Length);
                        return;
                    }
                    router.Run( ServeNext, xServerContext );
                    await ServeNext();
                    user.State.Document.Initialize(xServerContext);

                    var res = Res;
                    res.ContentType = "text/html";
                    res.StatusCode = 200;
                    await outputStream.WriteAsync(Encoding.UTF8.GetBytes("<!DOCTYPE html>\r\n"+user.State.Document.ToHtml())); 
                    return;
                    
                    async Task ServeNext()
                    {
                        var requestUrl = context.Request.Url.AbsolutePath;
                        string filePath = Path.Combine(ContentRoot, requestUrl.TrimStart('/'));
                        Console.WriteLine(filePath);
                        if (filePath == ContentRoot)
                        {
                            filePath = Path.Combine(filePath, "index.html");
                        }

                        if (File.Exists(filePath))
                        {
                            byte[] fileBytes = File.ReadAllBytes(filePath);
                            context.Response.StatusCode = 200;
                            context.Response.ContentType = GetMimeType(filePath);
                            await outputStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                            return;
                        }
                        if(Server.App == null)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            await outputStream.WriteAsync(Encoding.UTF8.GetBytes("404 - File Not Found"));
                            return;
                        }
                    }
                }
            }

            private XUser GetOrCreateSession(HttpListenerContext context)
            {
                string sessionId;

                XUser? user; 
                var cookie = context.Request.Cookies["SessionId"];
                if (cookie != null)
                {
                    sessionId = cookie.Value;
                    user = EonDB.Query<XUser>(sessionId, u => u.Id == sessionId).FirstOrDefault();
                    if (user == null)
                    {
                        user = new XUser();
                        user.Id = sessionId;
                        EonDB.Add(sessionId, user);
                    }
                }
                else
                {
                    sessionId = Guid.NewGuid().ToString();
                    context.Response.SetCookie(new Cookie("SessionId", sessionId));
                    user = new XUser();
                    user.Id = sessionId;
                    EonDB.Add(sessionId, user);
                }

                sessionId = Guid.NewGuid().ToString();
                return user;
            }

            private string GetMimeType(string filePath)
            {
                // MIME types are **exactly** as in your original code
                string ext = Path.GetExtension(filePath).ToLower();
                return ext switch
                {
                    ".txt" => "text/plain",
                    ".html" => "text/html",
                    ".htm" => "text/html",
                    ".css" => "text/css",
                    ".csv" => "text/csv",
                    ".js" => "application/javascript",
                    ".json" => "application/json",
                    ".xml" => "application/xml",
                    ".rss" => "application/rss+xml",
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    ".svg" => "image/svg+xml",
                    ".ico" => "image/x-icon",
                    ".bmp" => "image/bmp",
                    ".tiff" => "image/tiff",
                    ".webp" => "image/webp",
                    ".ttf" => "font/ttf",
                    ".otf" => "font/otf",
                    ".woff" => "font/woff",
                    ".woff2" => "font/woff2",
                    ".mp3" => "audio/mpeg",
                    ".wav" => "audio/wav",
                    ".ogg" => "audio/ogg",
                    ".m4a" => "audio/mp4",
                    ".aac" => "audio/aac",
                    ".flac" => "audio/flac",
                    ".mp4" => "video/mp4",
                    ".m4v" => "video/x-m4v",
                    ".mov" => "video/quicktime",
                    ".wmv" => "video/x-ms-wmv",
                    ".avi" => "video/x-msvideo",
                    ".webm" => "video/webm",
                    ".mkv" => "video/x-matroska",
                    ".pdf" => "application/pdf",
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".xls" => "application/vnd.ms-excel",
                    ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ".ppt" => "application/vnd.ms-powerpoint",
                    ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                    ".zip" => "application/zip",
                    ".tar" => "application/x-tar",
                    ".gz" => "application/gzip",
                    ".rar" => "application/vnd.rar",
                    ".7z" => "application/x-7z-compressed",
                    ".exe" => "application/octet-stream",
                    ".iso" => "application/x-iso9660-image",
                    ".apk" => "application/vnd.android.package-archive",
                    ".bin" => "application/octet-stream",
                    ".deb" => "application/vnd.debian.binary-package",
                    ".dmg" => "application/x-apple-diskimage",
                    ".msi" => "application/x-msdownload",
                    ".wasm" => "application/wasm",
                    _ => "application/octet-stream",
                };
            }
        }
        public string GetXJs(){
            return @"
const handledElements = new WeakSet();
var lastChange = {};
const messageQueue = [];
const BATCH_INTERVAL = 100; // Flush the batch every 100ms

// Event types that should bypass batching
const IMMEDIATE_EVENTS = ['click', 'input', 'change'];

// Function to flush the message queue to the server
function flushMessageQueue() {
    if (messageQueue.length > 0) {
        try {
            CS.invokeAsync('Vibe.XStateManager.ProcessBatchUpdate', [JSON.stringify(messageQueue)])
                .catch((err) => console.error('Failed to send batch update:', err, messageQueue));
            messageQueue.length = 0; // Clear the queue
        } catch (err) {
            console.error('Error sending batch update to server:', err);
        }
    }
}

// Periodically flush the message queue
setInterval(flushMessageQueue, BATCH_INTERVAL);

CS.onReady(() => {
    /**
     * Send updates to the server via `CS.invokeAsync`.
     * Critical updates are sent immediately; others are batched.
     */
    function sendToServer(update, immediate = false) {
        if (immediate) {
            // Send critical updates immediately
            try {
                CS.invokeAsync('Vibe.XStateManager.ProcessUpdate', [JSON.stringify(update)])
                    .catch((err) => console.error('Failed to send immediate update:', err, update));
            } catch (err) {
                console.error('Error sending immediate update to server:', err);
            }
        } else {
            // Add non-critical updates to the batch
            messageQueue.push(update);
        }
    }

    /**
     * Serialize an event for sending to the server.
     */
    function serializeEvent(event) {
        return {
            userId: CS.client.name,
            type: event.type,
            targetXid: event.target.getAttribute('xid'),
            value: event.target.value || null,
        };
    }

    /**
     * Observe all DOM mutations and forward them to the server.
     */
    function observeDomMutations() {
        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                const targetXid = mutation.target.getAttribute('xid');
                let update;

                switch (mutation.type) {
                    case 'attributes':
                        const value = mutation.target.getAttribute(mutation.attributeName);
                        if (lastChange.xid === targetXid && lastChange.value === value) {
                            return;
                        }
                        update = {
                            userId: CS.client.name,
                            action: 'attributeChanged',
                            targetXid: targetXid,
                            attribute: mutation.attributeName,
                            value: value,
                            timestamp: Date.now(),
                        };
                        break;

                    case 'childList':
                        mutation.addedNodes.forEach((node) => {
                            if (node.nodeType === Node.ELEMENT_NODE) {
                                update = {
                                    userId: CS.client.name,
                                    action: 'nodeAdded',
                                    parentXid: targetXid,
                                    targetXid: node.getAttribute('xid'),
                                    html: node.outerHTML,
                                    previousSiblingXid: node.previousElementSibling?.getAttribute('xid') || null,
                                    nextSiblingXid: node.nextElementSibling?.getAttribute('xid') || null,
                                    timestamp: Date.now(),
                                };
                                sendToServer(update);
                            }
                        });

                        mutation.removedNodes.forEach((node) => {
                            if (node.nodeType === Node.ELEMENT_NODE) {
                                update = {
                                    userId: CS.client.name,
                                    action: 'nodeRemoved',
                                    targetXid: node.getAttribute('xid'),
                                    timestamp: Date.now(),
                                };
                                sendToServer(update);
                            }
                        });
                        break;

                    case 'characterData':
                        update = {
                            userId: CS.client.name,
                            action: 'textChanged',
                            targetXid: mutation.target.parentNode?.getAttribute('xid'),
                            htmlContent: mutation.target.textContent,
                            timestamp: Date.now(),
                        };
                        break;

                    default:
                        console.warn('Unhandled mutation type:', mutation.type);
                        return;
                }

                if (update) {
                    sendToServer(update, true);
                }
            });
        });

        observer.observe(document, {
            attributes: true,
            childList: true,
            subtree: true,
            characterData: true,
        });
    }

    /**
     * Capture all DOM events and forward them to the server.
     */
    function captureAllEvents() {
        const handleEvent = (event) => {
            event.stopPropagation();
            if (event.target instanceof Element && event.target.getAttribute) {
                const targetXid = event.target.getAttribute('xid');
                if (!targetXid) return;

                const update = {
                    action: 'event',
                    eventData: serializeEvent(event),
                };

                // Send immediate events directly
                const isImmediate = IMMEDIATE_EVENTS.includes(event.type);
                sendToServer(update, isImmediate);
            }
        };

        const allEventTypes = Object.getOwnPropertyNames(window)
            .filter((prop) => /^on/.test(prop))
            .map((prop) => prop.slice(2));

        Array.from(document.getElementsByTagName('*')).forEach((element) => {
            if (handledElements.has(element)) {
                return; // Skip elements that already have listeners
            }

            allEventTypes.forEach((eventType) => {
                element.addEventListener(eventType, handleEvent);
            });

            handledElements.add(element); // Mark element as handled
        });
    }

    // Start observing DOM mutations and capturing events
    observeDomMutations();
    captureAllEvents();
    console.log('Script loaded and event listeners attached.');
});

// Periodically flush the message queue on window unload to avoid data loss
window.addEventListener('beforeunload', flushMessageQueue);
document.addEventListener('DOMContentLoaded', () => {
    // Function to handle updates from the server
    window.updateClientSideDom = function(update) {
        console.log(`Received update: ${update.action}`);
        lastChange = {action: update.action, xid: update.targetXid, value: update.htmlContent }

        if (update.action === 'nodeAdded') {
            const parent = document.querySelector(`[xid='${update.parentXid}']`);
            if (parent) {
                const newNode = document.createElement('div');
                newNode.innerHTML = update.htmlContent;
                parent.insertBefore(newNode, getNextSibling(parent, update.previousSiblingXid, update.nextSiblingXid));
            }
        }

        if (update.action === 'nodeRemoved') {
            const node = document.querySelector(`[xid='${update.targetXid}']`);
            if (node) {
                node.remove();
            }
        }

        if (update.action === 'attributeChanged') {
            const node = document.querySelector(`[xid='${update.targetXid}']`);
            if (node) {
                node.setAttribute(update.attributeName, update.htmlContent);
            }
        }

        if (update.action === 'textChanged') {
            const node = document.querySelector(`[xid='${update.targetXid}']`);
            if (node) {
                node.textContent = update.htmlContent;
            }
        }
    };

    // Helper function to handle precise positioning (previous and next siblings)
    function getNextSibling(parent, previousSiblingXid, nextSiblingXid) {
        const previousSibling = previousSiblingXid ? parent.querySelector(`[xid='${previousSiblingXid}']`) : null;
        const nextSibling = nextSiblingXid ? parent.querySelector(`[xid='${nextSiblingXid}']`) : null;
        if (previousSibling) {
            return previousSibling.nextSibling;
        } else if (nextSibling) {
            return nextSibling;
        } else {
            return null;
        }
    }
});
";
        }
    }
}
