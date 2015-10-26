﻿using System;

namespace Voxalia.Shared
{
    [Flags]
    public enum YourStatusFlags : byte
    {
        NONE = 0,
        RELOADING = 1,
        NEEDS_RELOAD = 2,
        FOUR = 4,
        EIGHT = 8,
        SIXTEEN = 16,
        THIRTYTWO = 32,
        SIXTYFOUR = 64,
        ONETWENTYEIGHT = 128
    }

    public enum StatusOperation : byte
    {
        NONE = 0,
        CHUNK_LOAD = 1
    }
}