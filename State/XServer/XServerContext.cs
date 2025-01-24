using System;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using EonDB;

namespace Vibe;

public class XServerContext
{
    public Dictionary<string,string> Params {get;set;} = new Dictionary<string, string>();
    public HttpListenerContext HttpListenerContext { get; private set; }
    public HttpListenerRequest Request => HttpListenerContext.Request;
    public HttpListenerResponse Response => HttpListenerContext.Response;
    public XUser User { get; set; }
    public Stream OutputStream { get; private set; }
    public XUser XUser { get; private set; }
    public EonDB.EonDB Cache { get; private set; }
    public string Body { get; set; } = "";
    public bool HasReplied {get;set;} = false;

    public XServerContext(HttpListenerContext context, Stream outputStream, XUser xUser, EonDB.EonDB cache)
    {
        this.HttpListenerContext = context;
        this.OutputStream = outputStream;
        this.XUser = xUser;
        this.Cache = cache;
    }

    public void Return(string response)
    {
        using (StreamWriter writer = new StreamWriter(OutputStream))
        {
            OutputStream.WriteAsync(Encoding.UTF8.GetBytes(response));
            Response.Close();
            HasReplied = true;
            return;
        }
    }
}
