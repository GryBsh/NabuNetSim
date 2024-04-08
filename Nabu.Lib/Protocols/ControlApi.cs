using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Nabu.Protocols
{
    public static class CtrlTypeId          // 0xEF
    {
        // [type]#[length:ushort][data]...

        /* 
        0xAF|0x82|0x00#0x01|0x01|0x00 > Sources, List, Paged, Page Size 1, Page 0
        < 0x80#0x01|0x01|0x82|0x00|0x04|T|E|S|T < List, Pages, Items, Type, None, Label
        0xAF|0x83|0x00#0x01|0x01|0x00|0x00 > Programs, List, Paged, Page Size 1, Page 0, Source 0
        < 0x80#0x01|0x01|0x83|0x00|0x04|T|E|S|T < List, Pages, Items, Type, None, Label
        0xAF|0x83|0x01#0x00|0xFF|0x00|0x00 > Program, Set, Select, Current, Source 0, Program 0
        */

        /*
        0xAF#0x84|0x00|0x00|0xFF > Settings, List, Full, Global
        < 0x80#0x01|0x84|0xFE|0x04|P|a|t|h|0x01|/ < List, Items, Type, ValueType (string), Label, String (/)
        0xAF#0x84|0x01|0x01|0xFF|0x00|0xFE|0x05|/|p|a|t|h > Settings, Set, Value, Global, Setting 0, ValueType, String (/path)
        */

        public const byte List = 0x80;          //> 0x80:[count]{[Item]}
        public const byte Item = 0x81;          //> [type]|[value]|[label:String][value]

        // Simple Responses / Items
        public const byte OK = 0xA0;            //> 0xA0:[]
        public const byte Error = 0xAF;         //> 0xAF:[code][message:String]

        // Values
        public const byte None = 0x00;    
        
        public const byte Byte = 0xB0;          //> 0xB0:{}:[]
        
        public const byte Short = 0xFA;         //> 0xFB:{}:[count][2]
        public const byte UShort = 0xFB;        //> 0xFC:{}:[count][2]
        public const byte Int = 0xFC;           //> 0xFC:{}:[count][4]
        public const byte UInt = 0xFD;          //> 0xFD:{}:[count][4]
        public const byte String = 0xFE;        //> 0xFE:{}:[length]([char]...)
        public const byte Array = 0xFF;         //> 0xFF:{}:[count]([value]...)

    }


    public enum CtrlCommand : byte
    {
        List = 0x00,
        Set = 0x01,
        Get = 0x02,
        Count = 0x03,
        Auth = 0xFF
    }

    public enum CtrlResponse : byte
    {
        List = CtrlTypeId.List,
        OK = CtrlTypeId.OK,
        Error = CtrlTypeId.Error,
    }

    public enum CtrlListType : byte
    {
        Full = 0x00,
        Paged = 0x01,
    }

    public enum CtrlSetType : byte
    {
        Select = 0x00,
        Value = 0x01,
    }

    public enum CtrlValueType : byte
    {
        None = CtrlTypeId.None,
        Byte = CtrlTypeId.Byte,
        Short = CtrlTypeId.Short,
        UShort = CtrlTypeId.UShort,
        Int = CtrlTypeId.Int,
        UInt = CtrlTypeId.UInt,
        String = CtrlTypeId.String,
        Array = CtrlTypeId.Array,
    }

    public enum CtrlItemType : byte
    {
        None = CtrlTypeId.None,
        OK = CtrlTypeId.OK,
        Error = CtrlTypeId.Error
    }

    public enum CtrlErrorCode : byte
    {
        Unknown = 0x00,
        UnknownType = 0x01,
        InvalidCommand = 0x02,
        InvalidSetType = 0x03,

        Unauthorized = 0xFA
    }
}
