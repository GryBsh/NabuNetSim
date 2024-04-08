using NHACPy;
using NHACP;
using NHACP.Messages;
using NHACP.Messages.V1;

using var host = new ServiceHost();
await host.StartAsync();

host.WaitForShutdown();

async void Test()
{
    var client = new NHACPClient("localhost", 9090);

    /*
        0x00 - HELLO!
    */

    var message = client.Hello();

    if (IsError(message)) return;

    var session = (message as NHACPV1SessionStarted)!;

    Console.WriteLine($"Hello from `{session.AdapterId}` (v{session.Version})");


    /*
        0x04 - DateTime     
    */

    var date = client.DateTime(session.SessionId);

    if (IsError(date)) return;

    Console.WriteLine($"Date: {date.Date}, Time: {date.Time}");


    /*
        DONE
    */

    await host.StopAsync();

    static bool IsError(NHACPResponse message)
    {
        if (message is NHACPV1Error error)
        {
            Console.WriteLine($"Error {error.Code}: {error.Message}");
            return true;
        }
        return false;
    }
}
