﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ServerGame.ServerMainSystem;
using Voxalia.Shared.Files;
using FreneticScript;
using Voxalia.Shared;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.ServerGame.OtherSystems;

namespace Voxalia.ServerGame.NetworkSystem
{
    public class WebPage
    {
        public Server TheServer;

        public Connection Network;

        public WebPage(Server tserver, Connection conn)
        {
            TheServer = tserver;
            Network = conn;
        }

        public string http_request_original;

        public string http_request_mode;

        public string http_request_page;

        public string http_request_version;

        public Dictionary<string, string> http_request_headers = new Dictionary<string, string>();

        public void Init(string request)
        {
            string[] headbits = request.Replace("\r", "").SplitFast('\n');
            http_request_original = headbits[0];
            string[] oreq = http_request_original.SplitFast(' ');
            if (oreq.Length != 3)
            {
                throw new ArgumentException("Invalid web request!");
            }
            http_request_mode = oreq[0];
            http_request_page = oreq[1];
            http_request_version = oreq[1];
            for (int i = 1; i < headbits.Length; i++)
            {
                if (headbits[i].Length < 2)
                {
                    continue;
                }
                string[] dat = headbits[i].SplitFast(':', 2);
                if (dat.Length != 2)
                {
                    continue;
                }
                http_request_headers[dat[0].Trim()] = dat[1].Trim();
            }
            string encAllowed;
            if (http_request_headers.TryGetValue("Accept-Encoding", out encAllowed))
            {
                GZip = encAllowed.Contains("gzip");
            }
            try
            {
                GetPage();
            }
            catch (Exception ex)
            {
                Utilities.CheckException(ex);
                SysConsole.Output("Handling webpage", ex); // TODO: DebugWebError cvar?
                http_response_id = 500;
                http_response_itname = "Internal Server Error";
                http_response_contenttype = "text/plain; charset=UTF-8";
                http_response_content = FileHandler.encoding.GetBytes("500 Internal Server Error\n");
            }
            if (GZip)
            {
                http_response_content = FileHandler.GZip(http_response_content);
            }
        }

        public void GetPage()
        {
            string pageLow = http_request_page.ToLowerFast();
            if (pageLow.StartsWith("/map/region/"))
            {
                string after;
                string region = pageLow.Substring("/map/region/".Length).BeforeAndAfter("/", out after);
                for (int i = 0; i < TheServer.LoadedRegions.Count; i++)
                {
                    if (TheServer.LoadedRegions[i].Name == region)
                    {
                        string[] dat = after.SplitFast('/');
                        if (dat[0] == "img" && dat.Length >= 4)
                        {
                            int x = Utilities.StringToInt(dat[1]);
                            int y = Utilities.StringToInt(dat[2]);
                            int z = Utilities.StringToInt(dat[3].Before("."));
                            byte[] data = TheServer.LoadedRegions[i].ChunkManager.GetImage(x, y, z);
                            if (data != null)
                            {
                                http_response_contenttype = "image/png";
                                http_response_content = data;
                                return;
                            }
                        }
                        else if (dat[0] == "full_img" && dat.Length >= 3)
                        {
                            int x = Utilities.StringToInt(dat[1]);
                            int y = Utilities.StringToInt(dat[2].Before("."));
                            KeyValuePair<int, int> maxes = TheServer.LoadedRegions[i].ChunkManager.GetMaxes(x, y);
                            List<byte[]> datums = new List<byte[]>();
                            for (int z = maxes.Key; z <= maxes.Value; z++)
                            {
                                byte[] dt = TheServer.LoadedRegions[i].ChunkManager.GetImage(x, y, z);
                                if (dt != null)
                                {
                                    datums.Add(dt);
                                }
                            }
                            if (datums.Count > 1)
                            {
                                http_response_contenttype = "image/png";
                                http_response_content = TheServer.BlockImages.Combine(datums);
                                return;
                            }
                            else if (datums.Count == 1)
                            {
                                http_response_contenttype = "image/png";
                                http_response_content = datums[0];
                            }
                        }
                        else if (dat[0] == "maxes" && dat.Length >= 3)
                        {
                            int x = Utilities.StringToInt(dat[1]);
                            int y = Utilities.StringToInt(dat[2]);
                            KeyValuePair<int, int> maxes = TheServer.LoadedRegions[i].ChunkManager.GetMaxes(x, y);
                            http_response_content = FileHandler.encoding.GetBytes(maxes.Key + "," + maxes.Value);
                            return;
                        }
                        else if (dat[0] == "expquick" && dat.Length >= 3)
                        {
                            int bx = Utilities.StringToInt(dat[1]);
                            int by = Utilities.StringToInt(dat[2]);
                            int sz = Chunk.CHUNK_SIZE * BlockImageManager.TexWidth;
                            StringBuilder content = new StringBuilder();
                            content.Append("<!doctype html>\n<html>\n<head>\n<title>Voxalia EXP-QUICK</title>\n</head>\n<body>\n");
                            const int SIZE = 6;
                            for (int x = -SIZE; x <= SIZE; x++)
                            {
                                for (int y = -SIZE; y <= SIZE; y++)
                                {
                                    content.Append("<img style=\"position:absolute;top:" + (y + SIZE) * sz + "px;left:" + (x + SIZE) * sz + "px;\" src=\"/map/region/"
                                        + region + "/full_img/" + (bx + x) + "/" + (by + y) + ".png\" />");
                                }
                            }
                            content.Append("\n</body>\n</html>\n");
                            http_response_content = FileHandler.encoding.GetBytes(content.ToString());
                            return;
                        }
                        break;
                    }
                }
            }
            Do404();
        }

        public void Do404()
        {
            // Placeholder
            string respcont = "<!doctype HTML>\n<html>";
            respcont += "<head><title>Voxalia Server 404</title></head>\n";
            respcont += "<body><h1>404: File Not Found</h1>\n<br>";
            respcont += "</body>\n";
            respcont += "</html>\n";
            http_response_id = 404;
            http_response_itname = "File Not Found";
            http_response_content = FileHandler.encoding.GetBytes(respcont);
        }

        public bool GZip = false;

        public string HTTP_RESPONSE_VERSION = "HTTP/1.1";

        public int http_response_id = 200;

        public string http_response_itname = "OK";

        public string http_response_contenttype = "text/html; charset=UTF-8";

        public int http_response_contentlength = 0;

        public byte[] http_response_content = null;

        public string GetHeaders()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(HTTP_RESPONSE_VERSION + " " + http_response_id + " " + http_response_itname + "\n");
            if (GZip)
            {
                sb.Append("Content-Encoding: gzip\n");
            }
            sb.Append("Content-Type: " + http_response_contenttype + "\n");
            sb.Append("Content-Length: " + http_response_content.Length + "\n");
            sb.Append("\n");
            return sb.ToString();
        }

        public byte[] GetFullData()
        {
            byte[] header = FileHandler.encoding.GetBytes(GetHeaders());
            byte[] result = new byte[http_response_content.Length + header.Length];
            header.CopyTo(result, 0);
            http_response_content.CopyTo(result, header.Length);
            return result;
        }
    }
}
