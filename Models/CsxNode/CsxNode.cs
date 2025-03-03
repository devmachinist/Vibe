using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Dynamic;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;

namespace Vibe
{
    /// <summary>
    /// This is a Xavier FIle model with all methods and options.
    /// It includes a C# attribute handler to add types as parameters for taking query strings.
    /// </summary>

    public partial class CsxNode : DynamicObject, ICsxNode
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Code { get; set; }
        public string dataset { get; set; }
        public string ContentType { get; set; } = "text/html";
        public string Xid { get; set; } = Guid.NewGuid().ToString();
        public ICsxNode? Parent { get; set; } = null;
        public string Route { get; set; } = "";
        public State state { get; set; }
        public string HTML { get; set; } = "";
        public string Scripts { get; set; }
        public bool ShouldRender { get; set; } = true;
        public bool JSClientApi { get; set; } = true;
        public string BaseUrl { get; set; }
        public bool Authorize { get; set; } = false;
        public string SessionId { get; set; }
        public string Class { get; set; } = "";
        private readonly Dictionary<string, List<object>> _handlers = new();
        public bool Rendering { get;set; } = false;
        public bool FirstRender { get; set; } = true;

        [JsonIgnore]
        public Assembly XAssembly { get; set; }

        public string TagName { get; set; } = "div";
        [JsonIgnore]
        public Dictionary<string, dynamic> Attributes { get; set; } = new Dictionary<string, dynamic>();
        public Dictionary<string, string> LiveAttributes { get; set; } = new Dictionary<string, string>();
        public List<object> ChildNodes { get; set; } = new List<object>();
        // Define an event handler delegate
        /// <summary>
        /// The array of parameters for taking query strings
        /// </summary>
        public Type[] Types { get; set; }
        /// <summary>
        /// The map of parameters and their value. This is used to pass the parameter values
        /// to other components in a Blazor application.
        /// </summary>
        public bool HasChanges { get; set; } = false;

        public CsxNode(string tagName = "div")
        {
            TagName = tagName;
            this["TagName"] = TagName;
        }
        public static CsxNode operator +(CsxNode parent, dynamic? child)
        {
            parent.Append(child);
            return parent;
        }
        public static CsxNode operator +(CsxNode obj, (int index, dynamic child) item)
        {
            if (item.index >= 0 && item.index <= obj.ChildNodes.Count)
                obj.ChildNodes.Insert(item.index, item.child);
            return obj;
        }

        // Indexer to allow direct access to Properties
        public object? this[string key]
        {
            get => Properties.TryGetValue(key, out var value) ? value : null;
            set => Properties[key] = value;
        }

        // Implicit operator to convert from CsxNode to Dictionary<string, object>
        public static implicit operator Dictionary<string, object>(CsxNode csxNode)
        {
            return new Dictionary<string, object>(csxNode.Properties);
        }

        // Implicit operator to convert from Dictionary<string, object> to ICsxNode
        public static implicit operator CsxNode(Dictionary<string, object> dictionary)
        {
            var ICsxNode = new CsxNode();
            foreach (var kvp in dictionary)
            {
                ICsxNode.Properties[kvp.Key] = kvp.Value;
            }
            return ICsxNode;
        }
        public virtual void OnParametersSet()
        {
        }
        public virtual void OnInitialized()
        {

        }
        public virtual void OnAfterRender()
        {

        }
        public ICsxNode Append(object child)
        {
            if (typeof(ICsxNode).IsAssignableFrom(child.GetType())) (child as ICsxNode).Parent = this;
            ChildNodes.Add(child);
            return this;
        }
        public ICsxNode GetElementById(string id)
        {
            return document.QuerySelector("[id='" + id + "']") ?? null;
        }

        public ICsxNode AddAttribute(string key, dynamic value)
        {
            Properties[key] = value;
            Attributes[key] = value;
            return this;
        }

        public dynamic AddChild(dynamic child)
        {
            Children.Add(child);
            return this;
        }
        public IEnumerable<object> Parse(string html)
        {
            var psr = new CSXParser();
            return psr.Parse(html);
        }
        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new ICsxNodeJsonConverter() },
                WriteIndented = true,
                ReferenceHandler = ReferenceHandler.Preserve,
                PropertyNamingPolicy = null
            };
            return JsonSerializer.Serialize(this, options);
        }

        public override string ToString()
        {
            var attributes = string.Join(" ", LiveAttributes.Select(a =>
            {
                    return $"{a.Key}=\"{a.Value}\"";
            }));

            var children = string.Join("",
                Children.Select(c =>
                {
                    if (c is string)
                    {
                        return c;
                    }
                    else if (c is ICsxNode)
                    {
                        return (c as ICsxNode)?.ToString();
                    }
                    return c as String;
                })) ?? "";
            if (string.IsNullOrWhiteSpace(Properties["TagName"] as string)) Properties["TagName"] = "html";
            return
             $"<{Properties["TagName"] as String} {attributes}>{children}</{Properties["TagName"] as String}>";
        }
        public ICsxNode SetXid(string id)
        {
            Xid = id;
            return this;
        }
        public Assembly GetAssembly()
        {
            dynamic k = this;
            return XAssembly;
        }
        public string GenerateJavascriptApi()
        {
            if (JSClientApi)
            {
                var typename = this.Name;
                var NSpace = XAssembly.FullName.Split(",")[0];
                var theType = XAssembly.GetType(NSpace + "." + this.Name);

                string jsModuleName = typename;

                StringBuilder sb = new StringBuilder();


                sb.AppendLine("var " + jsModuleName + " = window['${this.xid}'];");
                sb.AppendLine(" = jsModuleName");
                //Loop over all of the methods in the type and create a JS function for each
                foreach (var methodInfo in theType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                  BindingFlags.Static | BindingFlags.Instance))
                {
                    sb.AppendLine(jsModuleName + "." + methodInfo.Name + " = async function(parameters){ ");
                    sb.AppendLine("try {");
                    sb.AppendLine("    return await fetch('/api/" + typename + "/" + methodInfo.Name + "/" + methodInfo.Name + "', {method: 'POST',  headers: {  'Content-Type': 'application/json', }, body: JSON.stringify(parameters)});");
                    sb.AppendLine("} catch(err) {");
                    sb.AppendLine("console.log(err);");
                    sb.AppendLine("} };");
                }
                GC.Collect();
                //Write the output to a file
                return sb.ToString();
            }
            else
            {
                return "";
            }
        }
        public CsxNode(ICsxNode node)
        {
            Xid = Guid.NewGuid().ToString();
            Route = node.Route;
            Name = node.Name;
            Scripts = node.Scripts;
            TagName = node.TagName;
            HTML = node.HTML;
            Attributes = node.Attributes;
            Attributes["xid"] = Xid;
            ContentType = node.ContentType;
            Properties = node.Properties;
            ChildNodes = node.ChildNodes;
        }
        public ICsxNode SetAttribute(string name, string value)
        {
            SetLiveAttribute(name, value);
            return this;
        }
        public ICsxNode StageAtt(string name, Delegate value)
        {
            Properties[name] = value;
            Attributes[name] = value;
            return this;
        }
        public ICsxNode StageAtt(string name, dynamic value)
        {
            Properties[name] = value;
            Attributes[name] = value;
            var targetXid = Xid;
            var attributeName = name;
            return this;
        }

        public ICsxNode SetLiveAttribute(string name, string value)
        {
            if (char.IsLower(name[0])){
                LiveAttributes[name] = value;
                if (document != null && document.JS != null && !Rendering)
                {
                    _ = Task.Run(async () => await document.SendUpdateToClient("attributeChanged", Xid, value, name));
                }
            }
            return this;
        }
        public ICsxNode RemoveAttribute(string name)
        {
            if(LiveAttributes.ContainsKey(name))
            {
                LiveAttributes.Remove(name);
                if (document != null && document.JS != null && !Rendering)
                {
                    _ = Task.Run(async () => await document.SendUpdateToClient("attributeRemoved", Xid, "", name));
                }
            }
            return this;
        }

        /// <summary>
        /// This is an overridable method which allows the user to define
        /// the list of properties to be used by the component.
        /// </summary>
        /// <param name="properties">The list of properties.</param>
        public virtual void SetProperties(Dictionary<string, object> properties)
        {
            Properties = properties;
        }
        [JsonIgnore]
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        // Override TryGetMember to handle dynamic and explicit properties
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            // Check explicitly declared properties
            var property = GetType().GetProperty(binder.Name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                result = property.GetValue(this);
                return true;
            }

            // Check dynamic properties
            return Properties.TryGetValue(binder.Name, out result);
        }

        // Override TrySetMember to handle dynamic and explicit properties
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            // Check explicitly declared properties
            var property = GetType().GetProperty(binder.Name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                property.SetValue(this, value);
                return true;
            }

            // Otherwise, set as a dynamic property
            Properties[binder.Name] = value;
            return true;
        }
        // Optionally handle dynamic method invocation
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out dynamic? result)
        {
            if (Properties.TryGetValue(binder.Name, out var value) && value is Delegate method)
            {
                result = method.DynamicInvoke(args);
                return true;
            }

            result = null;
            return false;
        }
        // Access all properties (dynamic + explicit)
        public Dictionary<string, object> GetProperties()
        {
            var allProperties = new Dictionary<string, object>();

            // Add explicitly declared properties
            foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                allProperties[property.Name] = property.GetValue(this);
            }

            // Add dynamic properties
            foreach (var kvp in Properties)
            {
                allProperties[kvp.Key] = kvp.Value;
            }

            return allProperties;
        }

        // Get all dynamic properties (optional)
        public Dictionary<string, object> GetDynamicProperties()
        {
            return new Dictionary<string, dynamic>(Properties);
        }

        public string GetAttribute(string name)
        {
            return LiveAttributes[name];
        }

        public virtual void Dispose()
        {
            Attributes.Clear();
            Properties.Clear();
            Properties.Clear();
            ChildNodes.Clear();
            Xid = "";
            _handlers.Clear();
        }

        public void ReplaceChild(ICsxNode oldNode, ICsxNode newNode)
        {
            var i = Children.IndexOf(oldNode);
            if (i != -1)
            {
                Children.Remove(oldNode);
                if (i + 1 == Children.Count) Children.Add(newNode);
                else Children.Insert(i, newNode);
            }
        }
        public void Toggle(string _class, string? replacement = ""){
            var cl = GetAttribute("class");
            cl.Replace(_class, replacement);
            SetAttribute("class", cl);
        }
    }

    public class ICsxNodeJsonConverter : JsonConverter<ICsxNode>
    {
        public override ICsxNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var node = new CsxNode();

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token");
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected PropertyName token");
                }

                string propertyName = reader.GetString();
                reader.Read();

                object value = JsonSerializer.Deserialize<object>(ref reader, options);
                node.TrySetMember(new SimpleSetMemberBinder(propertyName), value);
            }
            return node;
        }

        public override void Write(Utf8JsonWriter writer, ICsxNode value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var kvp in value.GetProperties())
            {
                if ( kvp.Value != null && !(kvp.Value is Delegate))
                {
                    switch(kvp.Key){
                        case "Parent":
                            break;
                        case "document":
                            break;
                        case "Context":
                            break;
                        case "Properties":
                            break;
                        default:
                            writer.WritePropertyName(kvp.Key);
                            WriteValue(writer, kvp.Value, options);
                            break;
                    }
                }
                else
                {
                }
            }

            writer.WriteEndObject();
        }
        
        private void WriteValue(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            Type type = value.GetType();

            if (IsDelegate(type))
            {
                // Skip delegates
                return;
            }
            else if (typeof(IDictionary).IsAssignableFrom(type))
            {
                writer.WriteStartObject();
                var dict = (IDictionary)value;
                foreach (var key in dict.Keys)
                {
                    var dictValue = dict[key];
                    if (dictValue != null && !IsDelegate(dictValue.GetType()))
                    {
                        writer.WritePropertyName(key.ToString());
                        WriteValue(writer, dictValue, options);
                    }
                    else
                    {
                    }
                }
                writer.WriteEndObject();
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                writer.WriteStartArray();
                foreach (var item in (IEnumerable)value)
                {
                    if (item != null && !IsDelegate(item.GetType()))
                    {
                        WriteValue(writer, item, options);
                    }
                    else
                    {
                    }
                }
                writer.WriteEndArray();
            }
            else if (value is ICsxNode nestedNode)
            {
                JsonSerializer.Serialize(writer, nestedNode, options); // Uses the same converter
            }
            else
            {
                JsonSerializer.Serialize(writer, value, type, options);
            }
        }

        private bool IsDelegate(Type type)
        {
            return type != null && typeof(Delegate).IsAssignableFrom(type);
        }

        private bool IsSerializableProperty(Type type)
        {
            if (IsDelegate(type))
                return false;

            if (type == typeof(Assembly)) // Exclude Assembly type
                return false;

            // Add more exclusions as needed (e.g., types that are not serializable)
            return true;
        }
    }
    public class SimpleSetMemberBinder : SetMemberBinder
    {
        public SimpleSetMemberBinder(string name) : base(name, false) { }

        public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }
    }


}