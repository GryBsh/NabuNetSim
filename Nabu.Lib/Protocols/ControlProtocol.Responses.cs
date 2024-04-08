using Gry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Protocols;

public partial class ControlProtocol
{
    

    public static byte[] Text(string text)
    {
        text ??= string.Empty;
        var bytes = Encoding.ASCII.GetBytes(text);
        return [(byte)bytes.Length, .. bytes];
    }
    

    static Memory<byte> List(IEnumerable<CtrlItem> items)
    {
        return new([
            (byte)items.Count(),
            ..items.SelectMany(i => CtrlItem.ToBuffer(i).ToArray()).ToArray()
        ]);
    }

    static Memory<byte> Error(byte code, string message) => new([code, .. Text(message)]);

    static (byte, Memory<byte>) Page(
        byte pageSize,
        byte page,
        IEnumerable<CtrlItem> items
    )
    {
        var count = items.Count();
        var pages = count == pageSize ?
                        (byte)1 :
                        (byte)Math.Floor((double)(count / pageSize) + 1);
        items = items.Skip(pageSize * page).Take(pageSize);
        return (
            (byte)items.Count(),
            new([
                pages,
                ..List(items).Span
            ])
        );
    }
}
