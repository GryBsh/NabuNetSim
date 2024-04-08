namespace NHACP.V0
{
    public interface INHACPProtocolService
    {
        // 0x00 0x00                                    -> FRAME SIZE
        // 2 bytes

        //-----------------------
        // 0x04                 |                       -> TIME
        Task<(bool, string, string)> DateTime();

        void End();

        // -----------------------------
        // 0x02 |Index |Offset |Length |                -> GET
        //      |1 byte|4 bytes|2 bytes|
        Task<(bool, string, Memory<byte>)> Get(byte index, int offset, ushort length);

        //----------------------------------
        // 0x01 |Index |Length|Uri         |            -> LOAD
        //      |1 byte|1 byte|Length bytes|
        Task<(bool, string, byte, int)> Open(byte index, ushort flags, string uri);

        // 0x83 |Index |Length |                        -> LOADED   [bytes buffered]
        //      |1 byte|4 bytes|
        // - OR -
        // 0x82 |Code   |Length|Error       |           -> ERROR    [error message]
        //      |2 bytes|1 byte|Length bytes|
        //---------------------------------|
        // 0x84 |Length |Buffer      |                  -> BUFFER   [bytes]
        //      |2 bytes|Length bytes|
        // - OR -
        // 0x82                                         -> ERROR    [error message]
        //-----------------------------|

        // ------------------------------------------
        // 0x03 |Index |Offset |Length |Buffer      |
        //      |1 byte|4 bytes|2 bytes|Length bytes|
        Task<(bool, string)> Put(byte index, int offset, Memory<byte> buffer);

        // 0x81 ||                                      -> OK
        // - OR -
        // 0x82                                         -> ERROR    [error message]
        //------------------------------------------|
        // 0x85 |Date    |Time  |                       -> DATETIME [Date]  [Time]
        //      |YYYYMMdd|HHmmss|
        // - OR -
        // 0x82                                         -> ERROR    [error message]
        //----------------------|
    }
}