using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;
using Nabu.Network;

namespace Nabu.JavaScript;

public class JSAdaptorProxy
{
    public JSAdaptorProxy(Protocol proxy, V8ScriptEngine engine)
    {
        Proxy = proxy;
        Engine = engine;
    }

    public V8ScriptEngine Engine { get; }
    public Protocol Proxy { get; }

    public byte Read() => Proxy.Read();

    public ITypedArray<byte> Read(int length)
    {
        var array = JS.CreateArray<byte>(Engine, length);
        var buffer = Proxy.Read(length);
        array.Write(buffer, 0, (ulong)buffer.Length, 0);
        return array;
    }

    public int ReadInt() => Proxy.ReadInt();

    public ushort ReadShort() => Proxy.ReadShort();

    public void Write(ITypedArray<byte> array)
    {
        Proxy.Write(array.ToArray());
    }
}