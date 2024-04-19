﻿namespace Nabu.Network;public static class CommonUI{    static string[] Phrases = [
        //"👁️🚢👿",
        "Assimilation in progress",
        "Admiral! There be whales here!",
        "Ay Sir, I'm working on it!",
        "Hey Mr. 🦉",
        "Standby for NABUfall",
        "Your honor, I object to this preposterous poppycock",
        "It works for us now, Comrade",
        "Buy Pants",
        "2 NABUs and a KayPro walk into a bar...",
        "💣 0.015625 MEGA POWER 💣",
        "9/10 Doctors would prefer not to endorse this product",
        "NABU4Ever!",
        "👸Beware the wrath of King NABU 👸",
        "☎️ Please stay on the line, your call is important to us ☎️",
        "🎵 Never gonna give you up. Never gonna let you down 🎵",
        "Excuse me human, can I interest you in this pamphlet on the kingdom of NABU?"
    ];    public static string Phrase() => Phrases[Random.Shared.Next(0, Phrases.Length)];    public static byte[] NabuIconPattern { get; } = [
        //0xFF,0x80,0xA2,0xB2,0xAA,0xA6,0xA2,0x80,
        //0x80,0xBE,0xA2,0xAC,0xA2,0xBE,0x80,0xFF,
        //0xFF,0x01,0x7D,0x45,0x7D,0x45,0x45,0x01,
        //0x01,0x45,0x45,0x45,0x45,0x7D,0x01,0xFF               0xE0,0xC0,0xC0,0x80,0xFF,0x6D,0x2A,0x0A,        0x48,0x6A,0xFF,0x80,0xC0,0xC0,0xE0,0xE0,
        0x07,0x03,0x03,0x01,0xFF,0x9A,0xAA,0x9A,        0xAA,0x98,0xFF,0x01,0x03,0x03,0x07,0x07    ];    public static byte[] NabuIconColor { get; } = [
        //0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,
        //0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,
        //0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,
        //0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F        0x4F,0x41,0x4E,0x41,0xE1,0xE1,0xE1,0xE1,
        0xE1,0xE1,0xE1,0x41,0x4E,0x41,0x4F,0x41,        0x4F,0x41,0x4E,0x41,0xE1,0xE1,0xE1,0xE1,        0xE1,0xE1,0xE1,0x41,0x4E,0x41,0x4F,0x41    ];
        public static string DefaultIconClrStr { get; } = Convert.ToBase64String(NabuIconColor);
    public static string DefaultIconPtrnStr { get; } = Convert.ToBase64String(NabuIconPattern);    public static string IconData(string? value, string defaultValue)    {        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;    }}