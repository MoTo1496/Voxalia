﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class ChunkInfoPacketOut: AbstractPacketOut
    {
        public ChunkInfoPacketOut(Chunk chunk)
        {
            ID = 24;
            byte[] data_orig = new byte[chunk.BlocksInternal.Length * 3];
            for (int i = 0; i < chunk.BlocksInternal.Length; i++)
            {
                Utilities.UshortToBytes(chunk.BlocksInternal[i].BlockMaterial).CopyTo(data_orig, i * 2);
            }
            for (int i = 0; i < chunk.BlocksInternal.Length; i++)
            {
                data_orig[chunk.BlocksInternal.Length * 2 + i] = chunk.BlocksInternal[i].BlockData;
            }
            byte[] gdata = FileHandler.GZip(data_orig);
            DataStream ds = new DataStream(gdata.Length + 12);
            DataWriter dw = new DataWriter(ds);
            dw.WriteInt((int)chunk.WorldPosition.X);
            dw.WriteInt((int)chunk.WorldPosition.Y);
            dw.WriteInt((int)chunk.WorldPosition.Z);
            dw.WriteBytes(gdata);
            Data = ds.ToArray();
        }
    }
}