using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Scripting;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using System.Dynamic;
using AngleSharp.Html.Dom;
using System.Text.Json.Serialization;
using AngleSharp.Dom;
using AngleSharp;
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
        public dynamic? Parent { get; set; } = null;
        public string ClassBody(IXavierMemory? memory) => GenerateJavaScriptClass(this, this.GetType(), memory);
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

        [JsonIgnore]
        public Assembly XAssembly { get; set; }

        public string TagName { get; set; }
        public Dictionary<string, dynamic> Attributes { get; set; } = new Dictionary<string, dynamic>();
        public List<object> ChildNodes { get; set; } = new List<object>();
        // Define an event handler delegate
        /// <summary>
        /// The array of parameters for taking query strings
        /// </summary>
        public Type[] Types { get; set; }
        /// <summary>
        /// The list of properties for the component which is going to be used by other Xavier components
        /// </summary>
        public List<PropertyInfo> Properties { get; set; } = new List<PropertyInfo>();
        /// <summary>
        /// The map of parameters and their value. This is used to pass the parameter values
        /// to other components in a Blazor application.
        /// </summary>
        public IDictionary<string, string> Parameters { get; set; }
        public bool HasChanges { get; set; } = false;
        public IElement Element { get; set; }
        public Dictionary<string, Delegate> ParameterSetFunctions { get; set; } = new();

        public CsxNode(string tagName = "div", dynamic? attributes = null)
        {
            SetParameters(attributes);
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

            // Indexer to allow direct access to _properties
        public object? this[string key]
        {
            get => _properties.TryGetValue(key, out var value) ? value : null;
            set => _properties[key] = value;
        }

        // Implicit operator to convert from CsxNode to Dictionary<string, object>
        public static implicit operator Dictionary<string, object>(CsxNode CsxNode)
        {
            return new Dictionary<string, object>(CsxNode._properties);
        }

        // Implicit operator to convert from Dictionary<string, object> to CsxNode
        public static implicit operator CsxNode(Dictionary<string, object> dictionary)
        {
            var CsxNode = new CsxNode();
            foreach (var kvp in dictionary)
            {
                CsxNode._properties[kvp.Key] = kvp.Value;
            }
            return CsxNode;
        }
        public virtual void OnSetParameters(dynamic? attributes){
        }
        public virtual void SetParameters(dynamic? attributes)
        {
            OnSetParameters(attributes);
            if (attributes != null){
                switch(attributes){
                    case string s:
                        break;
                    case Dictionary<string, dynamic> d:
                        Attributes = d;
                        _properties = d;
                        break;
                    case List<object> l:
                        ChildNodes = l;
                        break;
                    case Array:
                        break;
                    default:
                        (attributes.GetType()
                        .GetProperties() as PropertyInfo[]).ToList().ForEach(a => {
                                _properties[a.Name] = a.GetValue(attributes);
                                if(a.Name == "X_class"){
                                        Class = a.GetValue(attributes);
                                        _properties["class"] = a.GetValue(attributes);
                                }
                                GetType().GetProperties().ToList().ForEach(p => {
                                    if (a.Name == p.Name)
                                    {
                                        p.SetValue(this, a.GetValue(attributes));
                                    }

                                });
                            });
                        break;
                }

            }
        }
        public CsxNode Append(object child)
        {
            if(typeof(CsxNode).IsAssignableFrom(child.GetType())) (child as CsxNode).Parent = this;
            ChildNodes.Add(child);
            return this;
        }

        public CsxNode AddAttribute(string key, dynamic value)
        {
            Attributes[key] = value;
            return this;
        }

        public dynamic AddChild(dynamic child)
        {
            Append(child);
            return this;
        }
        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new CsxNodeJsonConverter() },
                WriteIndented = true,
                ReferenceHandler = ReferenceHandler.Preserve,
                PropertyNamingPolicy = null
            };
            return JsonSerializer.Serialize(this, options);
        }

        public override string ToString()
        {
            var attributes = string.Join(" ", Attributes.Select(a => {
                if(a.Key.StartsWith("@On")){
                    return "";
                }
                if (a.Value is Delegate && typeof(string).IsAssignableFrom(a.Value().GetType())) {
                    return ($"{a.Key}=\"{(a.Value is Delegate && typeof(string).IsAssignableFrom(a.Value()) ? a.Value() : a.Value)}\"");
                }
                else if (a.Value is string) {
                    return $"{a.Key}=\"{a.Value}\"";
                }
                else if (typeof(Array).IsAssignableFrom(a.Value.GetType()))
                {

                }
                return ""; }));
            
            var children = string.Join("", 
                ChildNodes.Select(c =>
                {
                    if (c is Delegate)
                    {
                        return (c as Func<dynamic>)?.Invoke() ?? ""; 
                    }
                    else if (c is CsxNode)
                    {
                        return (c as CsxNode)?.ToString();
                    }
                    else if (c is IEnumerable<CsxNode>)
                    {
                        return (c as IEnumerable<CsxNode>).Select(x => x.ToString());
                    }
                    else if (c is String[])
                    {
                        return string.Join("", c as String[]);
                    }
                    return c as String;
                }))?? "";
            if(string.IsNullOrWhiteSpace(_properties["TagName"] as string)) _properties["TagName"] = "html";
            return 
             $"<{_properties["TagName"] as String} xid=\"{Xid}\" {attributes}>{children}</{_properties["TagName"] as String}>";
        }

        public string Content(IXavierMemory? memory)
        {
            if(Path == null)
                return this.ToString();
            else
                return RW.WriteVirtualFile(this, File.ReadAllText(Path), GetAssembly(), memory);
        }
        public CsxNode SetXid(string id)
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
                    sb.AppendLine("    return await fetch('/api/" + typename + "/" + methodInfo.Name +  "/" + methodInfo.Name + "', {method: 'POST',  headers: {  'Content-Type': 'application/json', }, body: JSON.stringify(parameters)});");
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public string GenerateJavaScriptClass(object node, Type cSharpClass, IXavierMemory memory)
        {
            var Node = (node as CsxNode);

            StringBuilder sb = new StringBuilder();
            var propertyRun = 0;
            sb.AppendLine($@"
class {Node.Name.ToUpper()} extends CsxNode {{
    constructor(data){{
        super();");
            Type[] inheritedTypes = XAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(CsxNode))).ToArray();
            List<Type> checkTypes = new List<Type>();
            foreach (Type inheritedType in inheritedTypes)
            {
                if (inheritedType.Name == this.Name && !checkTypes.Contains(inheritedType))
                {
                    checkTypes.Add(inheritedType);
                    System.Runtime.Remoting.ObjectHandle instance =
          Activator.CreateInstanceFrom(XAssembly.Location,
                                       inheritedType.FullName);
                    Node.Properties.AddRange(instance.Unwrap().GetType().GetProperties().ToList());

                    Node.RemoveDuplicates();

                    var i = instance;

                    foreach (var xprop in instance.Unwrap().GetType().GetProperties().ToList())
                    {
                        var fullprop = xprop.GetValue(instance.Unwrap());
                        if (xprop.Name == "Code")
                        {
                            continue;
                        }
                        else if (xprop.Name == "ClassBody")
                        {
                            continue;
                        }
                        else if (xprop.Name == "Properties")
                        {
                            continue;
                        }
                        else if (xprop.Name == "Path")
                        {
                            sb.AppendLine($@"this.{xprop.Name} = '{xprop.GetValue(instance.Unwrap()).ToString()}';");
                        }
                        else if (xprop.Name == "Scripts")
                        {
                        }
                        else if (xprop.Name == "HTML")
                        {

                        }

                        else if (xprop.Name == "Route")
                        {
                            sb.AppendLine($@"this.{xprop.Name} = {RW.ClearSlashes(xprop.GetValue(instance.Unwrap())?.ToString()) ?? ""};");
                        }
                        else if (xprop.PropertyType.ToString().Contains("List"))
                        {
                            sb.AppendLine($@"this.{xprop.Name} = new ObservableArray(...{JsonSerializer.Serialize(xprop.GetValue(instance.Unwrap()) ?? Array.Empty<string>())});");
                        }
                        else if (xprop.PropertyType.IsArray)
                        {
                            sb.AppendLine($@"this.{xprop.Name} = new ObservableArray(...{JsonSerializer.Serialize(xprop.GetValue(instance.Unwrap()) ?? Array.Empty<string>())});");
                        }

                        else
                        {
                            try
                            {
                                sb.AppendLine($@"this.{xprop.Name} = {JsonSerializer.Serialize(xprop.GetValue(instance.Unwrap())) ?? ""};");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString().ToString());
                            }
                        }
                    }
                }
            }
            sb.Append($@"}}
    GetScripts(){{
        return `{this.Scripts}`;
    }}
    GetHTML(){{
        return `{this.Content(memory).Replace("/", "\\/").Replace("`", "\\`")}`; 
    }}
}}");
            GC.Collect();
            return sb.ToString();
        }
        public string ExtractAtLast(string code)
        {
            StringBuilder ScriptBuilder = new StringBuilder();
            Regex ScriptReg = new Regex(@"[\s\S]*?(<script[^>]*>[\s\S]*?</script>)[\s\S]*?");
            MatchCollection Scripts = ScriptReg.Matches(code);


            Regex regex = new Regex(@"(@\w+[-+]{2}|@\w+)");
            MatchCollection matches = regex.Matches(code);
            Type[] inheritedTypes = XAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(CsxNode))).ToArray();
            List<PropertyInfo[]> properties = new List<PropertyInfo[]>();
            System.Runtime.Remoting.ObjectHandle instance = null;
            foreach (Type inheritedType in inheritedTypes)
            {
                if (inheritedType.GetType().Name == this.Name) { }
                instance =
    Activator.CreateInstanceFrom(XAssembly.Location,
                                 inheritedType.FullName);

                List<PropertyInfo> props = instance.Unwrap().GetType().GetRuntimeProperties().ToList();

                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        foreach (var property in props)
                        {
                            if (match.Groups[1].Value.Replace("@", "") == property.Name && this.Name == property.GetType().Name)
                            {
                                code = code.Replace(match.Value, property.GetValue(instance.Unwrap()).ToString());
                            }
                            else
                            {

                            }
                        }
                    }
                    foreach (Match m in Scripts)
                    {
                        ScriptBuilder.Append(m.Groups[1].Value);
                    }
                    foreach (Match t in Scripts)
                    {
                        code = code.Replace(t.Groups[1].Value, "");
                    }
                    ScriptBuilder = ScriptBuilder.Replace("<script>", "");
                    ScriptBuilder = ScriptBuilder.Replace("</script>", "");

                    this.Scripts = ScriptBuilder.ToString().Replace("/", "\\/").Replace("`", "\\`");
                    GC.Collect();
                    return code;
                }
            }
            foreach (Match m in Scripts)
            {
                ScriptBuilder.Append(m.Groups[1].Value);
            }
            foreach (Match t in Scripts)
            {
                code = code.Replace(t.Groups[1].Value, "");
            }
            ScriptBuilder = ScriptBuilder.Replace("<script>", "");
            ScriptBuilder = ScriptBuilder.Replace("</script>", "");

            this.Scripts = ScriptBuilder.ToString().Replace("/", "\\/").Replace("`", "\\`");
            ScriptBuilder.Clear();
            GC.Collect();
            return code;
        }
        public void GetPropertiesFromClasses()
        {
            // get all types that inherit from CsxNode
            Type[] inheritedTypes = XAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(CsxNode))).ToArray();

            // get all properties from each type
            foreach (var inheritedType in inheritedTypes)
            {

                if (inheritedType.Name == this.Name)
                {
                    PropertyInfo[] xprops = inheritedType.GetProperties().ToArray();
                    foreach (var xprop in xprops)
                    {
                        if (!Properties.Contains(xprop))
                        {
                            this.Properties.Add(xprop);
                        }
                    }
                }
            }
            // return the properties
        }
        public void RemoveDuplicates()
        {
            // Create a new List to store the information
            List<PropertyInfo> distinctPropertyInfo = new List<PropertyInfo>();

            // Iterate through the list of PropertyInfo
            foreach (PropertyInfo property in Properties)
            {
                //Check if the Distinct List already contains the item
                if (!distinctPropertyInfo.Contains(property))
                {
                    // Add the item to the Distinct List
                    distinctPropertyInfo.Add(property);
                }
            }
            this.Properties = distinctPropertyInfo;
        }
        public List<PropertyInfo> ReadProperties()
        {
            List<PropertyInfo> propertyInfos = new List<PropertyInfo>();

            // Get the type of this class

            Type ClassType = this.GetType();

            // Get properties from this class

            PropertyInfo[] properties = ClassType.GetProperties();

            // Loop through all properties and display their values
            foreach (PropertyInfo property in properties)
            {
                if (property != null)
                {
                    if (!Properties.Contains(property))
                    {
                        Properties.Add(property);
                        //Console.WriteLine("Property: {0}, Value: {1}",

                        //                  property.Name,

                        //                  property.GetValue(this));
                    }
                }
            }
            RemoveDuplicates();
            GC.Collect();
            return Properties;
        }
        public string ExtractPropertyDeclaration(object xavier, PropertyInfo propertyInfo, Assembly assembly)
        {
            System.Runtime.Remoting.ObjectHandle instance = null;
            instance = Activator.CreateInstanceFrom(assembly.Location,
                             xavier.GetType().FullName);
            try
            {

                //.GetRuntimeProperty("Name")
                //.GetValue(instance.Unwrap()));
                if (instance.Unwrap()
                           .GetType()
                           .GetRuntimeProperty("Name")
                           .GetValue(instance.Unwrap()) == this.Name)
                {

                    this.Properties
                        .Where(p => p.Name == propertyInfo.Name && p.GetType().Name == instance
                                                                                         .Unwrap()
                                                                                         .GetType()
                                                                                         .GetRuntimeProperty("Name")
                                                                                         .GetValue(instance.Unwrap()))
                                           .First()
                                           .SetValue(instance
                                                    .Unwrap(), this
                                                             .GetType()
                                                             .GetRuntimeProperties()
                                                             .Where(p => p.Name == propertyInfo.Name)
                                                                    .First()
                                                                    .GetValue(this));
                    if (this.Properties.Where(p => p.Name == propertyInfo.Name && p.GetType().Name == instance.Unwrap().GetType().GetRuntimeProperty("Name").GetValue(instance.Unwrap()))?.ToList().Count() < 2)
                    {

                        if (propertyInfo.PropertyType == typeof(string))
                        {
                            if (propertyInfo.Name == "Code" || propertyInfo.Name.Contains("ClassBody") || propertyInfo.Name.Contains("Content"))
                            {

                            }
                            else
                            {
                                string declaration = $"public {propertyInfo.PropertyType.Name} {propertyInfo.Name} {{ get; set; }} = \"{propertyInfo.GetValue(instance.Unwrap())?.ToString()}\";";
                                GC.Collect();
                                return declaration;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return "";
        }
        public string ExtractVariableList(object xavier, Assembly assembly)
        {
            string result = " ";
            foreach (PropertyInfo property in this.Properties)
            {
                //Check if the property is of type ScriptVariable
                //Extract the ScriptVariable from the property
                string scriptVar = ExtractPropertyDeclaration(xavier, property, assembly);

                //Add it to the list
                result += $"{RW.ClearSlashes(scriptVar)}";
            }

            return result;
        }
        public List<ScriptVariable> ExtractScriptVariables()
        {
            List<ScriptVariable> svs = new List<ScriptVariable>();
            foreach (FieldInfo field in this.GetType().GetFields())
            {
                if (field.FieldType == typeof(ScriptVariable))
                {
                    svs.Add((ScriptVariable)field.GetValue(this));
                }
            }
            return svs;
        }
        public CsxNode(CsxNode node)
        {
            Route = node.Route;
            Name = node.Name;
            Scripts = node.Scripts;
            TagName = node.TagName;
            this.HTML = node.HTML;
            this.Attributes = node.Attributes;
            this.ContentType = node.ContentType;
            this._properties = node._properties;
        }
        public CsxNode Clone()
        {
            return new CsxNode(this);
        }

        public void on(string eventName, object listener)
        {
            if (!_handlers.ContainsKey(eventName))
                _handlers[eventName] = new List<object>();
            _handlers[eventName].Add(listener);
        }

        public void emit(string eventName, params object[] args)
        {
            if (_handlers.ContainsKey(eventName))
            {
                foreach (var handler in _handlers[eventName])
                {
                    switch(handler){
                        case Action<object[]> handle:
                            handle(args);
                            break;
                        case Action<CsxNode, object[]> handle:
                            handle(this, args);
                            break;
                    }
                }
            }
        }

        public void once(string eventName, Action<object[]> listener)
        {
            Action<object[]> wrapper = null;
            wrapper = args =>
            {
                listener(args);
                off(eventName, wrapper);
            };
            on(eventName, wrapper);
        }

        public void off(string eventName, object listener)
        {
            if (_handlers.ContainsKey(eventName))
                _handlers[eventName].Remove(listener);
        }

        dynamic CreateMethodWrapper(MethodInfo methodInfo, object instance)
        {
            return new Func<object[], object>((parameters) =>
            {
                return methodInfo.Invoke(instance, parameters);
            });
        }
        async Task<dynamic> CreateAsyncMethodWrapper(MethodInfo methodInfo, object instance)
        {
            return new Func<object[], Task<object>>(async (parameters) =>
            {
                return await Task.Run(() => methodInfo.Invoke(instance, parameters));
            });
        }

        public CsxNode(string path, Assembly assembly, IXavierMemory memory)
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(path);
            Path = path;
            XAssembly = assembly;
            Code = File.ReadAllText(Path);

            this.GetPropertiesFromClasses();
            RemoveDuplicates();
        }
        /// <summary>
        /// This is a parameter method which allows the user to pass in Types as parameters
        /// in order to take query stringss.
        /// </summary>
        /// <param name="types">The array of types to use as parameters</param>
        public CsxNode SetAttribute(string name, Delegate value)
        {
            Attributes[name] = value;
            return this;
        }
        public CsxNode SetAttribute(string name, dynamic value)
        {
            Attributes[name] = value;
            return this;
        }

        public void ExecuteParameterFunctions()
        {
            foreach (var item in ParameterSetFunctions)
            {
                                
            }
        }

        /// <summary>
        /// This is an overridable method which allows the user to define
        /// the list of properties to be used by the component.
        /// </summary>
        /// <param name="properties">The list of properties.</param>
        public virtual void SetProperties(List<PropertyInfo> properties)
        {

            Properties = properties;
        }
        [JsonIgnore]
        public Dictionary<string, object> _properties = new();

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
            return _properties.TryGetValue(binder.Name, out result);
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
            _properties[binder.Name] = value;
            return true;
        }
        // Optionally handle dynamic method invocation
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out dynamic? result)
        {
            if (_properties.TryGetValue(binder.Name, out var value) && value is Delegate method)
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
            foreach (var kvp in _properties)
            {
                allProperties[kvp.Key] = kvp.Value;
            }

            return allProperties;
        }

        // Get all dynamic properties (optional)
        public Dictionary<string, object> GetDynamicProperties()
        {
            return new Dictionary<string, dynamic>(_properties);
        }

        public dynamic GetAttribute(string name)
        {
            return Attributes[name];
        }
    }

    public class CsxNodeJsonConverter : JsonConverter<CsxNode>
    {
        public override CsxNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

        public override void Write(Utf8JsonWriter writer, CsxNode value, JsonSerializerOptions options)
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
                        case "_properties":
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
            else if (value is CsxNode nestedNode)
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