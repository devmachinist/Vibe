using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Collections.Concurrent;
using AngleSharp;
using DotJS;
using System.Diagnostics;
using AngleSharp.Html.Dom;

namespace Vibe;
public static class XStateManager
{
    // Store states in a dictionary by user ID
    private static ConcurrentDictionary<string, State> States = new();
    [ToJS]
    public static async Task<bool> ProcessUpdate(string updateJson)
    {
        return await Task.Run(() =>
        {
            var update = System.Text.Json.JsonSerializer.Deserialize<Update>(updateJson);

            if (!States.TryGetValue(update.userId ?? "", out var state))
                if (!States.TryGetValue(update.eventData?.userId ?? "", out state)) return false;

            switch (update.action)
            {
                case "nodeAdded":
                    AddNodeToDom(state.Document, update);
                    break;

                case "nodeRemoved":
                    RemoveNodeFromDom(state.Document, update);
                    break;

                case "attributeChanged":
                    UpdateNodeAttribute(state.Document, update);
                    break;

                case "textChanged":
                    UpdateTextContent(state.Document, update);
                    break;

                case "event":
                    HandleDomEvent(state.Document, update);
                    break;

                default:
                    Console.WriteLine($"Unknown action: {update.action}");
                    break;
            }
            return true;
        });
    }
    [ToJS]
    public static async Task<bool> ProcessBatchUpdate(string batchJson)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Deserialize the batch of updates
                var updates = System.Text.Json.JsonSerializer.Deserialize<List<Update>>(batchJson);
                if (updates == null || updates.Count == 0) return false;

                foreach (var update in updates)
                {
                    if (!States.TryGetValue(update.userId ?? "", out var state))
                        if (!States.TryGetValue(update.eventData?.userId ?? "", out state)) continue;
                    
                    foreach (var change in state.Document.LastChanges)
                    {
                        if (change.targetXid == update.targetXid && update.html == change.htmlContent)
                        {
                            state.Document.LastChanges = new ConcurrentBag<dynamic>(state.Document.LastChanges.Select(p => p != change));
                            return true;
                        }
                    }
                    switch (update.action)
                    {
                        case "nodeAdded":
                            AddNodeToDom(state.Document, update);
                            break;

                        case "nodeRemoved":
                            RemoveNodeFromDom(state.Document, update);
                            break;

                        case "attributeChanged":
                            UpdateNodeAttribute(state.Document, update);
                            break;

                        case "textChanged":
                            UpdateTextContent(state.Document, update);
                            break;

                        case "event":
                            HandleDomEvent(state.Document, update);
                            break;

                        default:
                            Console.WriteLine($"Unknown action: {update.action}");
                            break;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing batch update: {ex.Message}");
                return false;
            }
        });
    }


    public static State? GetState(string id){
        if(States.TryGetValue(id, out var state)){
            return state;
        }
        return null;
    }
    public static void AddState(string userId, State state)
    {
        States[userId] = state;
    }

    public static bool RemoveState(string userId)
    {
        try
        {
            if (States.TryRemove(userId, out var removedState))
            {
                // Backup or log the removed state if necessary
                BackupRemovedState(userId, removedState);
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to remove state for userId: {userId}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception occurred while removing state for userId: {userId}. Exception: {ex.Message}");
            return false;
        }
    }

    private static void BackupRemovedState(string userId, State removedState)
    {
        // Implement backup logic here, e.g., save to a file or database
        Console.WriteLine($"State for userId: {userId} deleted.");
    }

    private static void AddNodeToDom(CsxDocument document, Update update)
    {
        var parent = document.QuerySelector($"[xid='{update.parentXid}']");
        if (parent == null) return;

        var parser = new HtmlParser();
        var newNode = parser.ParseFragment(update.html, parent).FirstOrDefault();
        if (newNode == null) return;

        if (!string.IsNullOrEmpty(update.previousSiblingXid))
        {
            var previousSibling = document.QuerySelector($"[xid='{update.previousSiblingXid}']");
            previousSibling?.Parent?.InsertBefore(newNode, previousSibling.NextSibling);
        }
        else if (!string.IsNullOrEmpty(update.nextSiblingXid))
        {
            var nextSibling = document.QuerySelector($"[xid='{update.nextSiblingXid}']");
            nextSibling?.Parent?.InsertBefore(newNode, nextSibling);
        }
        else
        {
            parent.AppendChild(newNode);
        }
    }

    private static void RemoveNodeFromDom(CsxDocument document, Update update)
    {
        var node = document.QuerySelector($"[xid='{update.targetXid}']");
        node?.Remove();
    }

    private static void UpdateNodeAttribute(CsxDocument document, Update update)
    {
        var node = document.QuerySelector($"[xid='{update.targetXid}']");
        node?.SetAttribute(update.attribute, update.value);
    }

    private static void UpdateTextContent(CsxDocument document, Update update)
    {
        var node = document.QuerySelector($"[xid='{update.targetXid}']");
        if (node != null)
        {
            node.TextContent = update.newText;
        }
    }

    private static void HandleDomEvent(CsxDocument document, Update update)
    {
        _ = Task.Run(() =>
        {
            var node = document.QuerySelector($"[xid='{update.eventData.targetXid}']");

            if (node != null)
            {
                if(update.eventData.value != null)
                (node as IHtmlInputElement).Value = update.eventData.value;
                
                var ev = new AngleSharp.Dom.Events.Event(update.eventData.type, false, false);
                
                node.Dispatch(ev);
            }
        });
    }
}

public class Update
{
    public string action { get; set; }
    public string? userId { get; set; }
    public string? targetXid { get; set; }
    public string? parentXid { get; set; }
    public string? previousSiblingXid { get; set; }
    public string? nextSiblingXid { get; set; }
    public string? attribute { get; set; }
    public string? value { get; set; }
    public string? html { get; set; }
    public string? newText { get; set; }
    public JSeventData? eventData { get; set; }
}
public class JSeventData
{
    public string value {get; set; }
    public string userId { get; set; }
    public string type { get; set; }
    public string targetXid { get; set; }
}
