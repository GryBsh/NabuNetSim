namespace Nabu.Protocols
{
    public enum NabuCtrlItemType : byte
    {
        None = CtrlTypeId.None,
        Source = 0x82,
        Program = 0x83,
        Setting = 0x84,
    }

    public enum NabuCtrlErrorCode : byte
    {
        None = CtrlTypeId.None,

        InvalidSource = 0x04,
        InvalidProgram = 0x05,
        InvalidSetting = 0x06,
        InvalidAdapter = 0x07
    }
}
