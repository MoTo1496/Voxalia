//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Voxalia.Shared;
using System.Collections.Generic;
using System.Collections.Specialized;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ClientGame.NetworkSystem.PacketsIn;
using Voxalia.ClientGame.NetworkSystem.PacketsOut;
using FreneticGameCore.Files;
using System.Threading.Tasks;
using System.Web;
using Voxalia.ClientGame.UISystem;
using System.Diagnostics;
using FreneticGameCore;

namespace Voxalia.ClientGame.NetworkSystem
{
    public class NetworkBase
    {
        public Client TheClient;

        public NetworkBase(Client tclient)
        {
            TheClient = tclient;
            Strings = new NetStringManager();
            recd = new byte[MAX];
            recd2 = new byte[MAX];
            if (System.IO.File.Exists("logindata.dat"))
            {
                string dat = System.IO.File.ReadAllText("logindata.dat");
                string[] d = dat.SplitFast('=');
                Username = d[0];
                Key = d[1];
            }
        }

        public NetStringManager Strings;

        public Socket ConnectionSocket;

        public Socket ChunkSocket;

        public Thread ConnectionThread;

        public CancellationTokenSource ConnectionCanceller;

        public string LastIP;

        public string LastPort;

        public bool IsAlive = false;

        bool norep = false;

        public string Username;

        private string Key;

        public string GetWebSession()
        {
            if (Username == null || Key == null)
            {
                throw new Exception("Can't get session, not logged in!");
            }
            if (!TheClient.CVars.n_online.ValueB)
            {
                return "NO_SESSION";
            }
            NameValueCollection data = new NameValueCollection();
            using (ShortWebClient wb = new ShortWebClient())
            {
                data["formtype"] = "getsess";
                data["username"] = Username;
                data["session"] = Key;
                byte[] response = wb.UploadValues(VoxProgram.GlobalServerAddress + "account/microgetsess", "POST", data);
                string resp = FileHandler.DefaultEncoding.GetString(response).Trim(' ', '\n', '\r', '\t');
                if (resp.StartsWith("ACCEPT=") && resp.EndsWith(";"))
                {
                    return resp.Substring("ACCEPT=".Length, resp.Length - 1 - "ACCEPT=".Length);
                }
                throw new Exception("Failed to get session: " + resp);
            }
        }

        public void Disconnect()
        {
            Disconnect(ConnectionThread, ConnectionCanceller, ConnectionSocket, ChunkSocket);
        }

        public void Disconnect(Thread ConnectionThread, CancellationTokenSource cancelMe, Socket ConnectionSocket, Socket ChunkSocket)
        {
            if (norep)
            {
                return;
            }
            norep = true;
            if (ConnectionThread != null && ConnectionThread.IsAlive && cancelMe != null)
            {
                cancelMe.Cancel();
            }
            if (ConnectionSocket != null)
            {
                if (IsAlive)
                {
                    try
                    {
                        SendPacket(new DisconnectPacketOut());
                    }
                    catch (Exception ex)
                    {
                        if (ex is ThreadAbortException)
                        {
                            throw ex;
                        }
                        SysConsole.Output(OutputType.WARNING, "Disconnecting: " + ex.ToString());
                    }
                }
                Socket csock = ConnectionSocket;
                TheClient.Schedule.ScheduleSyncTask(() => csock.Close(2), 2);
                ConnectionSocket = null;
            }
            if (ChunkSocket != null)
            {
                Socket csock = ChunkSocket;
                TheClient.Schedule.ScheduleSyncTask(() => csock.Close(2), 2);
                ChunkSocket = null;
            }
            TheClient.ShowMainMenu();
            IsAlive = false;
            norep = false;
        }

        public void Connect(string IP, string port, bool shortrender, string game)
        {
            Disconnect();
            TheClient.Files.SetSaveDirLate(game == null ? null : "client_" + game);
            Strings.Strings.Clear();
            TheClient.Resetregion();
            TheClient.ShowLoading();
            LastIP = IP;
            LastPort = port;
            TheClient.Schedule.StartAsyncTask(() => ConnectInternal(shortrender, () =>
            {
                if (!shortrender)
                {
                    TheClient.ShowGame();
                }
            }));
        }
        
        public void Ping(string IP, string port, Action<PingInfo> callback)
        {
            TheClient.Schedule.StartAsyncTask(() =>
            {
                try
                {
                    Interlocked.Increment(ref TheClient.Loading);
                    IPAddress address = GetAddress(IP);
                    ConnectionSocket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    ConnectionSocket.LingerState.LingerTime = 5;
                    ConnectionSocket.LingerState.Enabled = true;
                    ConnectionSocket.ReceiveTimeout = 10000;
                    ConnectionSocket.SendTimeout = 10000;
                    ConnectionSocket.ReceiveBufferSize = 5 * 1024 * 1024;
                    ConnectionSocket.SendBufferSize = 5 * 1024 * 1024;
                    int tport = Utilities.StringToInt(port);
                    ConnectionSocket.Connect(new IPEndPoint(address, tport));
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    ConnectionSocket.Send(FileHandler.DefaultEncoding.GetBytes("VOXp_\n"));
                    byte[] resp = ReceiveUntil(ConnectionSocket, 150, (byte)'\n');
                    stopwatch.Stop();
                    long ping = stopwatch.ElapsedMilliseconds;
                    string respString = FileHandler.DefaultEncoding.GetString(resp);
                    bool success = false;
                    string message = "No server ping response.";
                    if (respString != null)
                    {
                        string[] datums = respString.SplitFast('\r');
                        if (datums.Length < 2 || datums[0] != "SUCCESS")
                        {
                            message = "Invalid server ping details!";
                        }
                        else
                        {
                            success = true;
                            message = datums[1];
                        }
                    }
                    TheClient.Schedule.ScheduleSyncTask(() =>
                    {
                        callback.Invoke(new PingInfo() { Success = success, Message = message, Ping = ping });
                    });
                    Interlocked.Decrement(ref TheClient.Loading);
                }
                catch (Exception ex)
                {
                    if (ex is ThreadAbortException)
                    {
                        throw ex;
                    }
                    TheClient.Schedule.ScheduleSyncTask(() =>
                    {
                        callback.Invoke(new PingInfo() { Success = false, Message = "Failed: Network exception: " + ex.ToString(), Ping = -1 });
                    });
                    Interlocked.Decrement(ref TheClient.Loading);
                }
            });
        }

        int MAX = 1024 * 1024 * 2; // 2 MB by default

        byte[] recd;

        int recdsofar = 0;

        byte[] recd2;

        int recdsofar2 = 0;

        bool pLive = false;

        public long[] UsagesLastSecond = new long[(int)NetUsageType.COUNT];

        public long[] UsagesThisSecond = new long[(int)NetUsageType.COUNT];

        public long[] UsagesTotal = new long[(int)NetUsageType.COUNT];

        public void TickSocket(Socket sock, ref byte[] rd, ref int rdsf)
        {
            int avail = sock.Available;
            if (avail <= 0)
            {
                return;
            }
            if (avail + rdsf > MAX)
            {
                avail = MAX - rdsf;
                if (avail == 0)
                {
                    throw new Exception("Received overly massive packet?!");
                }
            }
            sock.Receive(rd, rdsf, avail, SocketFlags.None);
            rdsf += avail;
            if (rdsf < 5)
            {
                return;
            }
            while (true)
            {
                byte[] len_bytes = new byte[4];
                Array.Copy(rd, len_bytes, 4);
                int len = Utilities.BytesToInt(len_bytes);
                if (len + 5 > MAX)
                {
                    throw new Exception("Unreasonably huge packet!");
                }
                if (rdsf < 5 + len)
                {
                    return;
                }
                ServerToClientPacket packetID = (ServerToClientPacket)rd[4];
                byte[] data = new byte[len];
                Array.Copy(rd, 5, data, 0, len);
                byte[] rem_data = new byte[rdsf - (len + 5)];
                if (rem_data.Length > 0)
                {
                    Array.Copy(rd, len + 5, rem_data, 0, rem_data.Length);
                    Array.Copy(rem_data, rd, rem_data.Length);
                }
                rdsf -= len + 5;
                AbstractPacketIn packet;
                bool asyncable = false;
                NetUsageType usage;
                switch (packetID) // TODO: Packet registry!
                {
                    case ServerToClientPacket.PING:
                        packet = new PingPacketIn();
                        usage = NetUsageType.PINGS;
                        break;
                    case ServerToClientPacket.YOUR_POSITION:
                        packet = new YourPositionPacketIn();
                        usage = NetUsageType.PLAYERS;
                        break;
                    case ServerToClientPacket.SPAWN_ENTITY:
                        packet = new SpawnEntityPacketIn();
                        usage = NetUsageType.ENTITIES;
                        break;
                    case ServerToClientPacket.PHYSICS_ENTITY_UPDATE:
                        packet = new PhysicsEntityUpdatePacketIn();
                        usage = NetUsageType.ENTITIES;
                        break;
                    case ServerToClientPacket.MESSAGE:
                        packet = new MessagePacketIn();
                        usage = NetUsageType.GENERAL;
                        break;
                    case ServerToClientPacket.CHARACTER_UPDATE:
                        packet = new CharacterUpdatePacketIn();
                        usage = NetUsageType.PLAYERS;
                        break;
                    case ServerToClientPacket.TOPS:
                        packet = new TopsPacketIn();
                        usage = NetUsageType.CHUNKS;
                        break;
                    case ServerToClientPacket.DESPAWN_ENTITY:
                        packet = new DespawnEntityPacketIn();
                        usage = NetUsageType.ENTITIES;
                        break;
                    case ServerToClientPacket.NET_STRING:
                        packet = new NetStringPacketIn();
                        usage = NetUsageType.GENERAL;
                        break;
                    case ServerToClientPacket.SPAWN_ITEM:
                        packet = new SpawnItemPacketIn();
                        usage = NetUsageType.GENERAL;
                        break;
                    case ServerToClientPacket.YOUR_STATUS:
                        packet = new YourStatusPacketIn();
                        usage = NetUsageType.PLAYERS;
                        break;
                    case ServerToClientPacket.ADD_JOINT:
                        packet = new AddJointPacketIn();
                        usage = NetUsageType.ENTITIES;
                        break;
                    case ServerToClientPacket.YOUR_EID:
                        packet = new YourEIDPacketIn();
                        usage = NetUsageType.GENERAL;
                        break;
                    case ServerToClientPacket.DESTROY_JOINT:
                        packet = new DestroyJointPacketIn();
                        usage = NetUsageType.ENTITIES;
                        break;
                    case ServerToClientPacket.TOPS_DATA:
                        packet = new TopsDataPacketIn();
                        usage = NetUsageType.CHUNKS;
                        break;
                    case ServerToClientPacket.PRIMITIVE_ENTITY_UPDATE:
                        packet = new PrimitiveEntityUpdatePacketIn();
                        usage = NetUsageType.ENTITIES;
                        break;
                    case ServerToClientPacket.ANIMATION:
                        packet = new AnimationPacketIn();
                        usage = NetUsageType.PLAYERS;
                        break;
                    case ServerToClientPacket.FLASHLIGHT:
                        packet = new FlashLightPacketIn();
                        usage = NetUsageType.PLAYERS;
                        break;
                    case ServerToClientPacket.REMOVE_ITEM:
                        packet = new RemoveItemPacketIn();
                        usage = NetUsageType.GENERAL;
                        break;
                    case ServerToClientPacket.SET_ITEM:
                        packet = new SetItemPacketIn();
                        usage = NetUsageType.GENERAL;
                        break;
                    case ServerToClientPacket.CVAR_SET:
                        packet = new CVarSetPacketIn();
                        usage = NetUsageType.GENERAL;
                        break;
                    case ServerToClientPacket.SET_HELD_ITEM:
                        packet = new SetHeldItemPacketIn();
                        usage = NetUsageType.GENERAL;
                        break;
                    case ServerToClientPacket.CHUNK_INFO:
                        packet = new ChunkInfoPacketIn();
                        usage = NetUsageType.CHUNKS;
                        break;
                    case ServerToClientPacket.BLOCK_EDIT:
                        packet = new BlockEditPacketIn();
                        usage = NetUsageType.CHUNKS;
                        break;
                    case ServerToClientPacket.SUN_ANGLE:
                        packet = new SunAnglePacketIn();
                        usage = NetUsageType.EFFECTS;
                        break;
                    case ServerToClientPacket.TELEPORT:
                        packet = new TeleportPacketIn();
                        usage = NetUsageType.PLAYERS;
                        break;
                    case ServerToClientPacket.OPERATION_STATUS:
                        packet = new OperationStatusPacketIn();
                        usage = NetUsageType.GENERAL;
                        break;
                    case ServerToClientPacket.PARTICLE_EFFECT:
                        packet = new ParticleEffectPacketIn();
                        usage = NetUsageType.EFFECTS;
                        break;
                    case ServerToClientPacket.PATH:
                        packet = new PathPacketIn();
                        usage = NetUsageType.EFFECTS;
                        break;
                    case ServerToClientPacket.CHUNK_FORGET:
                        packet = new ChunkForgetPacketIn();
                        usage = NetUsageType.CHUNKS;
                        break;
                    case ServerToClientPacket.FLAG_ENTITY:
                        packet = new FlagEntityPacketIn();
                        usage = NetUsageType.ENTITIES;
                        break;
                    case ServerToClientPacket.DEFAULT_SOUND:
                        packet = new DefaultSoundPacketIn();
                        usage = NetUsageType.EFFECTS;
                        break;
                    case ServerToClientPacket.GAIN_CONTROL_OF_VEHICLE:
                        packet = new GainControlOfVehiclePacketIn();
                        usage = NetUsageType.ENTITIES;
                        break;
                    case ServerToClientPacket.ADD_CLOUD:
                        packet = new AddCloudPacketIn();
                        usage = NetUsageType.CLOUDS;
                        break;
                    case ServerToClientPacket.REMOVE_CLOUD:
                        packet = new RemoveCloudPacketIn();
                        usage = NetUsageType.CLOUDS;
                        break;
                    case ServerToClientPacket.ADD_TO_CLOUD:
                        packet = new AddToCloudPacketIn();
                        usage = NetUsageType.CLOUDS;
                        break;
                    case ServerToClientPacket.YOUR_VEHICLE:
                        packet = new YourVehiclePacketIn();
                        usage = NetUsageType.ENTITIES;
                        break;
                    case ServerToClientPacket.SET_STATUS:
                        packet = new SetStatusPacketIn();
                        usage = NetUsageType.PLAYERS;
                        break;
                    case ServerToClientPacket.HIGHLIGHT:
                        packet = new HighlightPacketIn();
                        usage = NetUsageType.EFFECTS;
                        break;
                    case ServerToClientPacket.PLAY_SOUND:
                        packet = new PlaySoundPacketIn();
                        usage = NetUsageType.EFFECTS;
                        break;
                    case ServerToClientPacket.LOD_MODEL:
                        packet = new LODModelPacketIn();
                        usage = NetUsageType.ENTITIES;
                        break;
                    case ServerToClientPacket.LOSE_CONTROL_OF_VEHICLE:
                        packet = new LoseControlOfVehiclePacketIn();
                        usage = NetUsageType.ENTITIES;
                        break;
                    case ServerToClientPacket.JOINT_UPDATE:
                        packet = new JointUpdatePacketIn();
                        usage = NetUsageType.ENTITIES;
                        break;
                    default:
                        throw new Exception("Invalid packet ID: " + packetID);
                }
                UsagesThisSecond[(int)usage] += 5 + data.Length;
                UsagesTotal[(int)usage] += 5 + data.Length;
                packet.TheClient = TheClient;
                packet.ChunkN = sock == ChunkSocket;
                ServerToClientPacket pid = packetID;
                if (asyncable)
                {
                    // TODO: StartAsyncTask?
                    if (!packet.ParseBytesAndExecute(data))
                    {
                        SysConsole.Output(OutputType.ERROR, "Bad async packet (ID=" + pid + ") data!");
                    }
                }
                else
                {
                    TheClient.Schedule.ScheduleSyncTask(() =>
                    {
                        try
                        {
                            if (!packet.ParseBytesAndExecute(data))
                            {
                                SysConsole.Output(OutputType.ERROR, "Bad sync packet (ID=" + pid + ") data!");
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is ThreadAbortException)
                            {
                                throw ex;
                            }
                            SysConsole.Output(OutputType.ERROR, "Bad sync packet (ID=" + pid + ") data: " + ex.ToString());
                        }
                    });
                }
            }
        }

        public void LaunchTicker()
        {
            TheClient.Schedule.StartAsyncTask(() =>
            {
                ConnectionThread = Thread.CurrentThread;
                CancellationTokenSource cts = ConnectionCanceller = new CancellationTokenSource();
                while (true)
                {
                    try
                    {
                        if (cts.IsCancellationRequested)
                        {
                            return;
                        }
                        if (!Tick())
                        {
                            return;
                        }
                        Thread.Sleep(16);
                    }
                    catch (Exception ex)
                    {
                        if (ex is ThreadAbortException)
                        {
                            throw ex;
                        }
                        SysConsole.Output(OutputType.ERROR, "Connection: " + ex.ToString());
                    }
                }
            });
        }

        bool Tick()
        {
            // TODO: Connection timeout
            if (!IsAlive)
            {
                if (pLive)
                {
                    pLive = false;
                }
                return false;
            }
            try
            {
                if (!pLive)
                {
                    TheClient.Schedule.ScheduleSyncTask(() => { if (!TheClient.IsMainMenu) { TheClient.ShowGame(); } });
                    pLive = true;
                }
                TickSocket(ConnectionSocket, ref recd, ref recdsofar);
                TickSocket(ChunkSocket, ref recd2, ref recdsofar2);
                return true;
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException)
                {
                    throw ex;
                }
                SysConsole.Output(OutputType.ERROR, "Forcibly disconnected from server: " + ex.GetType().Name + ": " + ex.Message);
                SysConsole.Output(OutputType.DEBUG, ex.ToString());
                Disconnect();
                return false;
            }
        }

        public byte[] GetBytesFor(AbstractPacketOut packet)
        {
            byte id = (byte)packet.ID;
            byte[] data = packet.Data;
            byte[] fdata = new byte[data.Length + 5];
            Utilities.IntToBytes(data.Length).CopyTo(fdata, 0);
            fdata[4] = id;
            data.CopyTo(fdata, 5);
            return fdata;
        }

        public void SendPacket(AbstractPacketOut packet)
        {
            if (!IsAlive)
            {
                return;
            }
            try
            {
                ConnectionSocket.Send(GetBytesFor(packet));
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException)
                {
                    throw ex;
                }
                SysConsole.Output(OutputType.WARNING, "Forcibly disconnected from server: " + ex.GetType().Name + ": " + ex.Message);
                SysConsole.Output(OutputType.DEBUG, ex.ToString());
                Disconnect();
            }
        }

        public void SendChunkPacket(AbstractPacketOut packet)
        {
            if (!IsAlive)
            {
                return;
            }
            try
            {
                ChunkSocket.Send(GetBytesFor(packet));
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException)
                {
                    throw ex;
                }
                SysConsole.Output(OutputType.WARNING, "Forcibly disconnected from server: " + ex.GetType().Name + ": " + ex.Message);
                Disconnect();
            }
        }

        public bool LastConnectionFailed = false;

        void ConnectInternal(bool shortrender, Action disco)
        {
            try
            {
                Socket ConnectionSocket;
                Socket ChunkSocket;
                LastConnectionFailed = false;
                Interlocked.Increment(ref TheClient.Loading);
                string key = GetWebSession();
                IPAddress address = GetAddress(LastIP);
                ConnectionSocket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ConnectionSocket.LingerState.LingerTime = 5;
                ConnectionSocket.LingerState.Enabled = true;
                ConnectionSocket.ReceiveTimeout = 10000;
                ConnectionSocket.SendTimeout = 10000;
                ConnectionSocket.ReceiveBufferSize = 5 * 1024 * 1024;
                ConnectionSocket.SendBufferSize = 5 * 1024 * 1024;
                int tport = Utilities.StringToInt(LastPort);
                ConnectionSocket.Connect(new IPEndPoint(address, tport));
                string renderd = shortrender ? "2,0,0,0,0,2,0"
                    : TheClient.CVars.r_renderdist.ValueI + "," + TheClient.CVars.r_renderdist_2.ValueI + ","
                    + TheClient.CVars.r_renderdist_2h.ValueI + "," + TheClient.CVars.r_renderdist_5.ValueI + ","
                    + TheClient.CVars.r_renderdist_5h.ValueI + "," + TheClient.CVars.r_renderdist_6.ValueI + ","
                    + TheClient.CVars.r_renderdist_15.ValueI;
                ConnectionSocket.Send(FileHandler.DefaultEncoding.GetBytes("VOX__\r" + Username
                     + "\r" + key + "\r" + LastIP + "\r" + LastPort + "\r" + renderd + "\n"));
                byte[] resp = ReceiveUntil(ConnectionSocket, 150, (byte)'\n');
                if (FileHandler.DefaultEncoding.GetString(resp) != "ACCEPT")
                {
                    ConnectionSocket.Close();
                    throw new Exception("Server did not accept connection");
                }
                ConnectionSocket.Blocking = false;
                ChunkSocket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ChunkSocket.LingerState.LingerTime = 5;
                ChunkSocket.LingerState.Enabled = true;
                ChunkSocket.ReceiveTimeout = 10000;
                ChunkSocket.SendTimeout = 10000;
                ChunkSocket.ReceiveBufferSize = 5 * 1024 * 1024;
                ChunkSocket.SendBufferSize = 5 * 1024 * 1024;
                ChunkSocket.Connect(new IPEndPoint(address, tport));
                ChunkSocket.Send(FileHandler.DefaultEncoding.GetBytes("VOXc_\r" + Username
                    + "\r" + key + "\r" + LastIP + "\r" + LastPort + "\n"));
                resp = ReceiveUntil(ChunkSocket, 150, (byte)'\n');
                if (FileHandler.DefaultEncoding.GetString(resp) != "ACCEPT")
                {
                    ConnectionSocket.Close();
                    ChunkSocket.Close();
                    throw new Exception("Server did not accept connection");
                }
                ChunkSocket.Blocking = false;
                TheClient.Schedule.ScheduleSyncTask(() =>
                {
                    disco();
                    this.ConnectionSocket = ConnectionSocket;
                    this.ChunkSocket = ChunkSocket;
                    SysConsole.Output(OutputType.INFO, "Connected to " + address.ToString() + " " + tport);
                    IsAlive = true;
                    LaunchTicker();
                    Interlocked.Decrement(ref TheClient.Loading);
                });
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException)
                {
                    throw ex;
                }
                LastConnectionFailed = true;
                Interlocked.Decrement(ref TheClient.Loading);
                TheClient.Schedule.ScheduleSyncTask(() =>
                {
                    UIConsole.WriteLine("Connection failed: " + ex.Message);
                });
                SysConsole.Output(OutputType.ERROR, "Networking / connect internal: " + ex.ToString());
                if (ConnectionSocket != null)
                {
                    ConnectionSocket.Close(5);
                }
                if (ChunkSocket != null)
                {
                    ChunkSocket.Close(5);
                }
            }
        }

        IPAddress GetAddress(string IP)
        {
            if (!IPAddress.TryParse(IP, out IPAddress address))
            {
                IPHostEntry entry = Dns.GetHostEntry(IP);
                if (entry.AddressList.Length == 0)
                {
                    throw new Exception("Empty address list for DNS server at '" + IP + "'");
                }
                if (TheClient.CVars.n_first.Value.ToLowerFast() == "ipv4")
                {
                    foreach (IPAddress saddress in entry.AddressList)
                    {
                        if (saddress.AddressFamily == AddressFamily.InterNetwork)
                        {
                            address = saddress;
                            break;
                        }
                    }
                }
                else if (TheClient.CVars.n_first.Value.ToLowerFast() == "ipv6")
                {
                    foreach (IPAddress saddress in entry.AddressList)
                    {
                        if (saddress.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            address = saddress;
                            break;
                        }
                    }
                }
                if (address == null)
                {
                    foreach (IPAddress saddress in entry.AddressList)
                    {
                        if (saddress.AddressFamily == AddressFamily.InterNetworkV6 || saddress.AddressFamily == AddressFamily.InterNetwork)
                        {
                            address = saddress;
                            break;
                        }
                    }
                }
                if (address == null)
                {
                    throw new Exception("DNS has entries, but none are IPv4 or IPv6!");
                }
            }
            return address;
        }

        byte[] ReceiveUntil(Socket s, int max_bytecount, byte ender)
        {
            byte[] bytes = new byte[max_bytecount];
            int gotten = 0;
            while (gotten < max_bytecount)
            {
                while (s.Available <= 0)
                {
                    Thread.Sleep(1);
                }
                s.Receive(bytes, gotten, 1, SocketFlags.None);
                if (bytes[gotten] == ender)
                {
                    byte[] got = new byte[gotten];
                    Array.Copy(bytes, got, gotten);
                    return got;
                }
                gotten++;
            }
            throw new Exception("Maximum byte count reached without valid ender");
        }
    }

    class ShortWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = 30 * 1000;
            return w;
        }
    }
}
