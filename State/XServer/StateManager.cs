using System.Collections.Concurrent;
using DotJS;
using System.Diagnostics;
using System.Text.Json;

namespace Vibe;
public static class XStateManager
{
    // Store states in a dictionary by user ID
    public static ConcurrentDictionary<string, State> States = new();
    [ToJS]
    public static async Task<bool> ProcessUpdate(string updateJson)
    {
        return await Task.Run(() =>
        {
            var update = System.Text.Json.JsonSerializer.Deserialize<Update>(updateJson);

            if (!States.TryGetValue(update.userId ?? "", out var state) && state == null)
                if (!States.TryGetValue(update.eventData?.userId ?? "", out state)) return false;

            switch (update.action)
            {
                //case "nodeAdded":
                //    AddNodeToDom(state.Document, update);
                //    break;

                //case "nodeRemoved":
                //    RemoveNodeFromDom(state.Document, update);
                //    break;

                //case "attributeChanged":
                //    UpdateNodeAttribute(state.Document, update);
                //    break;

                //case "textChanged":
                //    UpdateTextContent(state.Document, update);
                //    break;

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
                    if (!States.TryGetValue(update.userId ?? "", out var state) && state == null)
                        if (!States.TryGetValue(update.eventData?.userId ?? "", out state)) continue;
                    
                    switch (update.action)
                    {
                        //case "nodeAdded":
                        //    AddNodeToDom(state.Document, update);
                        //    break;

                        //case "nodeRemoved":
                        //    RemoveNodeFromDom(state.Document, update);
                        //    break;

                        //case "attributeChanged":
                        //    UpdateNodeAttribute(state.Document, update);
                        //    break;

                        //case "textChanged":
                        //    UpdateTextContent(state.Document, update);
                        //    break;

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
          var xs = States.TryGetValue(id, out var state);
        return xs? state: null;
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
                removedState.Document.Dispose();
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

    private static void UpdateNodeAttribute(CsxDocument document, Update update)
    {
        var lc = document.LastChanges.FirstOrDefault(lastChange =>
                    lastChange.targetXid == update.targetXid &&
                    lastChange.action == update.action &&
                    lastChange.attributeName == update.attribute &&
                    lastChange.htmlContent == update.value);
        if (lc is not null) {
            document.LastChanges = new ConcurrentBag<dynamic>(document.LastChanges.Where(c => c != lc));
            return;
        }

        var node = document.QuerySelector($"[xid='{update.targetXid}']");
        node?.SetAttribute(update.attribute, update.value);
    }

    private static void UpdateTextContent(CsxDocument document, Update update)
    {
        var lc = document.LastChanges.FirstOrDefault(lastChange =>
                    lastChange.targetXid == update.targetXid &&
                    lastChange.action == update.action &&
                    lastChange.htmlContent == update.newText);
        if (lc is not null) {
            document.LastChanges = new ConcurrentBag<dynamic>(document.LastChanges.Where(c => c != lc));
            return;
        }

        var node = document.QuerySelector($"[xid='{update.targetXid}']");
        if (node != null)
        {
            node.Children.Clear();
            node.Children.Add(update.newText);
        }
    }

    private static void HandleDomEvent(CsxDocument document, Update update)
    {
        Task.Run(() =>
        {
            try
            {
                var node = document.QuerySelector($@"[xid=""{update.eventData.targetXid}""]");
                if (node != null)
                {
                    if (update.eventData.value != null)
                    {
                        var v = update.eventData.value;
                        node.SetAttribute("value", v);
                    }

                    node.emit(update.eventData.type, update.eventData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }).Wait();
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
