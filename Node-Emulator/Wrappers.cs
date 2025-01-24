using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Dynamic;

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.IO.Compression;

public class NodePath
{
    public string dirname(string path) => Path.GetDirectoryName(path);

    public string basename(string path, string ext = "")
    {
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(ext) && name.EndsWith(ext))
            return name.Substring(0, name.Length - ext.Length);
        return name;
    }

    public string extname(string path) => Path.GetExtension(path);

    public string join(params string[] paths) => Path.Combine(paths);

    public string resolve(params string[] paths)
    {
        string combinedPath = Path.Combine(paths);
        return Path.GetFullPath(combinedPath);
    }

    public string relative(string from, string to)
    {
        Uri fromUri = new Uri(Path.GetFullPath(from));
        Uri toUri = new Uri(Path.GetFullPath(to));
        return Uri.UnescapeDataString(fromUri.MakeRelativeUri(toUri).ToString());
    }

    public string normalize(string path) => Path.GetFullPath(new Uri(path).LocalPath)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public bool isAbsolute(string path) => Path.IsPathRooted(path);

    public string sep => Path.DirectorySeparatorChar.ToString();
    public string delimiter => Path.PathSeparator.ToString();

    public string parse(string path)
    {
        return $"{{ root: \"{Path.GetPathRoot(path)}\", dir: \"{Path.GetDirectoryName(path)}\", base: \"{Path.GetFileName(path)}\", ext: \"{Path.GetExtension(path)}\", name: \"{Path.GetFileNameWithoutExtension(path)}\" }}";
    }

    public string format(dynamic pathObject)
    {
        return Path.Combine(pathObject.dir, pathObject["base"]);
    }
}

public class OS
{
    public string platform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "win32";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "darwin";
        return "unknown";
    }

    public string arch() => RuntimeInformation.ProcessArchitecture.ToString().ToLower();

    public object[] cpus()
    {
        int coreCount = Environment.ProcessorCount;
        object[] cpuInfo = new object[coreCount];
        for (int i = 0; i < coreCount; i++)
        {
            cpuInfo[i] = new
            {
                model = "Generic CPU",
                speed = 3000, // Replace with actual CPU speed if available
                times = new
                {
                    user = 0,
                    nice = 0,
                    sys = 0,
                    idle = 0,
                    irq = 0
                }
            };
        }
        return cpuInfo;
    }

    public long freemem() => GC.GetTotalMemory(false);

    public long totalmem()
    {
        return Process.GetCurrentProcess().WorkingSet64;
    }

    public string homedir() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public string tmpdir() => Path.GetTempPath();

    public string hostname() => Environment.MachineName;

    public long uptime() => Environment.TickCount64 / 1000;

    public object networkInterfaces()
    {
        return new { }; // Placeholder for simplicity; implement with System.Net.NetworkInformation
    }
}

public class NodeFileSystem
{
    public async Task<string> readFile(string path, string encoding = "utf-8")
    {
        using var reader = new StreamReader(path, System.Text.Encoding.GetEncoding(encoding));
        return await reader.ReadToEndAsync();
    }

    public string readFileSync(string path, string encoding = "utf-8")
    {
        return File.ReadAllText(path, System.Text.Encoding.GetEncoding(encoding));
    }

    public async Task writeFile(string path, string data, string encoding = "utf-8")
    {
        using var writer = new StreamWriter(path, false, System.Text.Encoding.GetEncoding(encoding));
        await writer.WriteAsync(data);
    }

    public void writeFileSync(string path, string data, string encoding = "utf-8")
    {
        File.WriteAllText(path, data, System.Text.Encoding.GetEncoding(encoding));
    }

    public async Task appendFile(string path, string data, string encoding = "utf-8")
    {
        using var writer = new StreamWriter(path, true, System.Text.Encoding.GetEncoding(encoding));
        await writer.WriteAsync(data);
    }

    public void appendFileSync(string path, string data, string encoding = "utf-8")
    {
        File.AppendAllText(path, data, System.Text.Encoding.GetEncoding(encoding));
    }

    public void mkdir(string path)
    {
        Directory.CreateDirectory(path);
    }

    public void mkdirSync(string path)
    {
        Directory.CreateDirectory(path);
    }

    public string[] readdirSync(string path)
    {
        return Directory.GetFileSystemEntries(path);
    }

    public void unlinkSync(string path)
    {
        File.Delete(path);
    }

    public void renameSync(string oldPath, string newPath)
    {
        File.Move(oldPath, newPath);
    }
}

public class EventEmitter
{
    private readonly Dictionary<string, List<Action<object[]>>> _handlers = new();

    public void on(string eventName, Action<object[]> listener)
    {
        if (!_handlers.ContainsKey(eventName))
            _handlers[eventName] = new List<Action<object[]>>();
        _handlers[eventName].Add(listener);
    }

    public void emit(string eventName, params object[] args)
    {
        if (_handlers.ContainsKey(eventName))
        {
            foreach (var handler in _handlers[eventName])
                handler(args);
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

    public void off(string eventName, Action<object[]> listener)
    {
        if (_handlers.ContainsKey(eventName))
            _handlers[eventName].Remove(listener);
    }
}

public class HTTP
{
    // ==========================
    // Constants
    // ==========================
    public static string[] METHODS => Enum.GetNames(typeof(HttpMethod));
    public static Dictionary<int, string> STATUS_CODES => new()
    {
        { 200, "OK" },
        { 404, "Not Found" },
        { 500, "Internal Server Error" },
        // Add other HTTP status codes as needed
    };

    // ==========================
    // Class: http.Agent
    // ==========================
    public class Agent
    {
        private Dictionary<string, Queue<HttpWebRequest>> _freeSockets = new();
        public Dictionary<string, Queue<HttpWebRequest>> freeSockets => _freeSockets;

        public int maxSockets { get; set; } = 10;
        public int maxFreeSockets { get; set; } = 256;

        public void destroy() => _freeSockets.Clear();

        public string getName(Dictionary<string, string> options)
        {
            return $"{options["host"]}:{options["port"]}";
        }

        public void reuseSocket(HttpWebRequest socket, HttpWebRequest request) => throw new NotImplementedException();
        public void keepSocketAlive(HttpWebRequest socket) => throw new NotImplementedException();
        public void createConnection(Dictionary<string, string> options) => throw new NotImplementedException();
    }

    // ==========================
    // Class: http.ClientRequest
    // ==========================
    public class ClientRequest
    {
        public event Action onAbort;
        public event Action onClose;
        public event Action onConnect;
        public event Action onContinue;
        public event Action onFinish;
        public event Action<HttpWebResponse> onResponse;
        public event Action onTimeout;
        public event Action onUpgrade;

        private readonly HttpWebRequest _request;
        private readonly MemoryStream _body = new MemoryStream();

        public bool writableEnded { get; private set; }
        public bool writableFinished { get; private set; }

        public ClientRequest(string method, string url, Dictionary<string, string> headers = null)
        {
            _request = WebRequest.CreateHttp(url);
            _request.Method = method;

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    _request.Headers[header.Key] = header.Value;
                }
            }
        }

        public void write(string chunk, string encoding = "utf-8", Action callback = null)
        {
            var bytes = Encoding.GetEncoding(encoding).GetBytes(chunk);
            _body.Write(bytes, 0, bytes.Length);
            callback?.Invoke();
        }

        public void end(Action callback = null)
        {
            writableEnded = true;
            _request.ContentLength = _body.Length;

            if (_body.Length > 0)
            {
                using (var stream = _request.GetRequestStream())
                {
                    _body.Seek(0, SeekOrigin.Begin);
                    _body.CopyTo(stream);
                }
            }

            Task.Run(() =>
            {
                try
                {
                    var response = (HttpWebResponse)_request.GetResponse();
                    writableFinished = true;
                    onResponse?.Invoke(response);
                }
                catch (WebException ex)
                {
                    onResponse?.Invoke((HttpWebResponse)ex.Response);
                }

                callback?.Invoke();
            });
        }

        public void abort()
        {
            _request.Abort();
            onAbort?.Invoke();
        }

        public void setTimeout(int timeout, Action callback = null)
        {
            _request.Timeout = timeout;
            callback?.Invoke();
        }

        public void destroy(Exception error = null)
        {
            _request.Abort();
            onClose?.Invoke();
        }
    }

    // ==========================
    // Class: http.Server
    // ==========================
    public class Server
    {
        private readonly HttpListener _listener;

        public event Action<HttpListenerRequest, HttpListenerResponse> onRequest;
        public event Action onClose;

        public Server()
        {
            _listener = new HttpListener();
        }

        public void listen(int port, string hostname = "localhost", Action callback = null)
        {
            _listener.Prefixes.Add($"http://{hostname}:{port}/");
            _listener.Start();

            Task.Run(async () =>
            {
                Console.WriteLine($"Server is listening on http://{hostname}:{port}");
                callback?.Invoke();

                while (_listener.IsListening)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        onRequest?.Invoke(context.Request, context.Response);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            });
        }

        public void close(Action callback = null)
        {
            _listener.Stop();
            onClose?.Invoke();
            callback?.Invoke();
        }
    }

    // ==========================
    // Class: http.ServerResponse
    // ==========================
    public class ServerResponse
    {
        private readonly HttpListenerResponse _response;

        public bool writableEnded { get; private set; }
        public bool writableFinished { get; private set; }

        public ServerResponse(HttpListenerResponse response)
        {
            _response = response;
        }

        public void setHeader(string name, string value)
        {
            _response.Headers[name] = value;
        }

        public void write(string chunk, string encoding = "utf-8", Action callback = null)
        {
            var bytes = Encoding.GetEncoding(encoding).GetBytes(chunk);
            _response.OutputStream.Write(bytes, 0, bytes.Length);
            callback?.Invoke();
        }

        public void end(string data = null, string encoding = "utf-8", Action callback = null)
        {
            if (data != null)
            {
                write(data, encoding);
            }

            writableEnded = true;
            writableFinished = true;
            _response.Close();
            callback?.Invoke();
        }
    }

    // ==========================
    // HTTP Utility Methods
    // ==========================
    public Server createServer(Action<HttpListenerRequest, HttpListenerResponse> handler)
    {
        var server = new Server();
        server.onRequest += handler;
        return server;
    }

    public ClientRequest request(string method, string url, Dictionary<string, string> headers = null)
    {
        return new ClientRequest(method, url, headers);
    }

    public void get(string url, Dictionary<string, string> headers, Action<HttpWebResponse> callback)
    {
        var req = request("GET", url, headers);
        req.onResponse += callback;
        req.end();
    }

    public void get(string url, Action<HttpWebResponse> callback)
    {
        get(url, null, callback);
    }
}


public class Crypto
{
    public string createHash(string algorithm, string data)
    {
        using var hashAlgorithm = HashAlgorithm.Create(algorithm);
        if (hashAlgorithm == null)
            throw new ArgumentException($"Unsupported algorithm: {algorithm}");

        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hashBytes = hashAlgorithm.ComputeHash(dataBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    public string randomBytes(int size)
    {
        var buffer = new byte[size];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToHexString(buffer).ToLower();
    }
}

public class Timers
{
    public Timer setTimeout(Action callback, int delay)
    {
        return new Timer(_ => callback(), null, delay, Timeout.Infinite);
    }

    public Timer setInterval(Action callback, int interval)
    {
        return new Timer(_ => callback(), null, interval, interval);
    }

    public void clearTimeout(Timer timer)
    {
        timer?.Dispose();
    }

    public void clearInterval(Timer timer)
    {
        timer?.Dispose();
    }
}

public class URL
{
    public Uri parse(string url)
    {
        return new Uri(url);
    }

    public string resolve(string baseUrl, string relativeUrl)
    {
        var baseUri = new Uri(baseUrl);
        return new Uri(baseUri, relativeUrl).ToString();
    }

    public string format(Uri uri)
    {
        return uri.ToString();
    }
}

public class Util
{
    public string format(string formatString, params object[] args)
    {
        return string.Format(formatString, args);
    }

    public bool isArray(object obj)
    {
        return obj is Array;
    }

    public bool isObject(object obj)
    {
        return obj != null && obj.GetType().IsClass;
    }

    public Func<object[], object> promisify(Func<object[], object> func)
    {
        return args =>
        {
            var task = Task.Run(() => func(args));
            return task;
        };
    }
}

public class Buffer
{
    private byte[] _data;

    // Constructor to initialize buffer with given size
    private Buffer(int size)
    {
        _data = new byte[size];
    }

    // Constructor to initialize buffer with existing byte array
    private Buffer(byte[] data)
    {
        _data = data;
    }

    // Factory method to create a buffer from a string
    public static Buffer from(string input, string encoding = "utf-8")
    {
        return encoding.ToLower() switch
        {
            "utf-8" => new Buffer(Encoding.UTF8.GetBytes(input)),
            "ascii" => new Buffer(Encoding.ASCII.GetBytes(input)),
            "base64" => new Buffer(Convert.FromBase64String(input)),
            "hex" => new Buffer(Convert.FromHexString(input)),
            _ => throw new ArgumentException($"Unsupported encoding: {encoding}")
        };
    }

    // Factory method to create a buffer from an existing byte array
    public static Buffer from(byte[] data)
    {
        return new Buffer(data);
    }

    // Factory method to create an uninitialized buffer
    public static Buffer alloc(int size)
    {
        return new Buffer(size);
    }

    // Factory method to create a buffer filled with the specified value
    public static Buffer alloc(int size, byte fill)
    {
        var buffer = new Buffer(size);
        Array.Fill(buffer._data, fill);
        return buffer;
    }

    // Factory method to create a buffer filled with a specified string
    public static Buffer alloc(int size, string fill, string encoding = "utf-8")
    {
        var buffer = alloc(size);
        var fillData = from(fill, encoding)._data;
        Array.Copy(fillData, buffer._data, Math.Min(size, fillData.Length));
        return buffer;
    }

    // Get the length of the buffer
    public int length => _data.Length;

    // Read an integer from the buffer (similar to readUInt8)
    public byte readUInt8(int offset)
    {
        if (offset >= _data.Length)
            throw new IndexOutOfRangeException("Offset is out of bounds.");
        return _data[offset];
    }

    // Write an integer to the buffer (similar to writeUInt8)
    public void writeUInt8(byte value, int offset)
    {
        if (offset >= _data.Length)
            throw new IndexOutOfRangeException("Offset is out of bounds.");
        _data[offset] = value;
    }

    // Convert the buffer to a string
    public string toString(string encoding = "utf-8", int start = 0, int end = -1)
    {
        if (end == -1 || end > _data.Length) end = _data.Length;
        byte[] subData = new byte[end - start];
        Array.Copy(_data, start, subData, 0, end - start);

        return encoding.ToLower() switch
        {
            "utf-8" => Encoding.UTF8.GetString(subData),
            "ascii" => Encoding.ASCII.GetString(subData),
            "base64" => Convert.ToBase64String(subData),
            "hex" => BitConverter.ToString(subData).Replace("-", "").ToLower(),
            _ => throw new ArgumentException($"Unsupported encoding: {encoding}")
        };
    }

    // Fill the buffer with a specified value
    public Buffer fill(byte value, int start = 0, int end = -1)
    {
        if (end == -1 || end > _data.Length) end = _data.Length;
        for (int i = start; i < end; i++)
        {
            _data[i] = value;
        }
        return this;
    }

    // Slice a portion of the buffer
    public Buffer slice(int start, int end)
    {
        if (start < 0 || end > _data.Length || start >= end)
            throw new ArgumentException("Invalid slice range.");

        byte[] slicedData = new byte[end - start];
        Array.Copy(_data, start, slicedData, 0, end - start);
        return new Buffer(slicedData);
    }

    // Compare this buffer with another buffer
    public int compare(Buffer other)
    {
        for (int i = 0; i < Math.Min(_data.Length, other._data.Length); i++)
        {
            int diff = _data[i] - other._data[i];
            if (diff != 0) return diff;
        }
        return _data.Length - other._data.Length;
    }

    // Copy data from this buffer to another buffer
    public void copy(Buffer target, int targetStart = 0, int sourceStart = 0, int sourceEnd = -1)
    {
        if (sourceEnd == -1 || sourceEnd > _data.Length) sourceEnd = _data.Length;
        int length = sourceEnd - sourceStart;

        if (length + targetStart > target._data.Length)
            throw new ArgumentException("Target buffer is too small.");

        Array.Copy(_data, sourceStart, target._data, targetStart, length);
    }

    // Static method to compare two buffers
    public static int compare(Buffer buf1, Buffer buf2) => buf1.compare(buf2);

    // Static method to concatenate multiple buffers
    public static Buffer concat(Buffer[] list, int totalLength = -1)
    {
        if (totalLength == -1) totalLength = list.Sum(buf => buf.length);

        var concatenatedData = new byte[totalLength];
        int offset = 0;

        foreach (var buf in list)
        {
            buf._data.CopyTo(concatenatedData, offset);
            offset += buf.length;
        }

        return new Buffer(concatenatedData);
    }
}


public class Zlib
{
    public byte[] deflate(byte[] data)
    {
        using var outputStream = new MemoryStream();
        using (var compressionStream = new DeflateStream(outputStream, CompressionLevel.Optimal))
        {
            compressionStream.Write(data, 0, data.Length);
        }
        return outputStream.ToArray();
    }

    public byte[] inflate(byte[] data)
    {
        using var inputStream = new MemoryStream(data);
        using var decompressionStream = new DeflateStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        decompressionStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }
}

public class DNS
{
    public string[] resolve(string hostname)
    {
        return Dns.GetHostAddresses(hostname).Select(ip => ip.ToString()).ToArray();
    }
}

public class Node
{
    public NodePath path = new NodePath();
    public OS os = new OS();
    public EventEmitter events = new EventEmitter();
    public HTTP http = new HTTP();
    public Crypto crypto = new Crypto();
    public Timers timers = new Timers();
    public URL url = new URL();
    public Util util = new Util();
    public Zlib zlib = new Zlib();
    public DNS dns = new DNS();
}

