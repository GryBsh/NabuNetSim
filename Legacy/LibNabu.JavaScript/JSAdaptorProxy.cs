using Gry.Protocols;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;
using Nabu.Network;
using Nabu.Settings;

namespace Nabu.JavaScript;

public class JSAdaptorProxy(Protocol<AdaptorSettings> proxy, V8ScriptEngine engine)
{
    public V8ScriptEngine Engine { get; } = engine;
    public Protocol<AdaptorSettings> Proxy { get; } = proxy;

    public byte Read() => Proxy.Read();

    public ITypedArray<byte> Read(int length)
    {
        var array = Engine.CreateArray<byte>(length);
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