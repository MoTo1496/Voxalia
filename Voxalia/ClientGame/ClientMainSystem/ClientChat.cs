//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.ClientGame.UISystem;
using FreneticGameGraphics.UISystem;
using Voxalia.Shared;
using Voxalia.ClientGame.OtherSystems;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.ClientGame.NetworkSystem.PacketsOut;
using OpenTK;
using FreneticGameCore;
using FreneticGameGraphics;
using FreneticGameGraphics.GraphicsHelpers;

namespace Voxalia.ClientGame.ClientMainSystem
{
    public partial class Client
    {
        public UIGroup ChatMenu;

        public List<ChatMessage> ChatMessages = new List<ChatMessage>(600);
        
        public UIInputBox ChatBox;

        public UIScrollBox ChatScroller;

        public bool[] Channels;

        public void InitChatSystem()
        {
            FontSet font = FontSets.Standard;
            int minY = 10 + (int)font.font_default.Height;
            ChatMenu = new UIGroup(new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_CENTER).GetterWidthHeight(() => Window.Width, () => Window.Height - minY - UIBottomHeight).ConstantXY(0, 0));
            ChatScroller = new UIScrollBox(new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_CENTER).GetterWidthHeight(() => ChatMenu.Position.Width - (30 * 2), () => ChatMenu.Position.Height - minY).ConstantXY(0, minY)) { Color = new Color4F(0f, 0.5f, 0.5f, 0.6f) };
            ChatBox = new UIInputBox("", "Enter a /command or a chat message...", font, new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_CENTER).GetterWidth(() => ChatScroller.Position.Width).ConstantX(0).GetterY(() => (int)ChatScroller.Position.Height + minY))
            {
                EnterPressed = EnterChatMessage
            };
            ChatMenu.AddChild(ChatBox);
            ChatMenu.AddChild(ChatScroller);
            Channels = new bool[(int)TextChannelHelpers.COUNT];
            Func<int> xer = () => 30;
            Channels[0] = true;
            for (int i = 1; i < Channels.Length; i++)
            {
                Channels[i] = true;
                string n = ((TextChannel)i).ToString();
                int len = (int)FontSets.Standard.MeasureFancyText(n);
                UITextLink link = null;
                Func<int> fxer = xer;
                int chan = i;
                link = new UITextLink(null, "^r^t^0^h^o^2" + n, "^!^e^t^0^h^o^2" + n, "^2^e^t^0^h^o^0" + n, FontSets.Standard, () => ToggleLink(link, n, chan), new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).GetterX(fxer).ConstantY(10));
                xer = () => fxer() + len + 10;
                ChatMenu.AddChild(link);
            }
            ClearChat();
            ChatScrollToBottom();
        }

        void EnterChatMessage()
        {
            if (ChatBox.Text.Length == 0)
            {
                CloseChat();
                return;
            }
            if (ChatBox.Text.StartsWith("/"))
            {
                Commands.ExecuteCommands(ChatBox.Text);
            }
            else
            {
                CommandPacketOut packet = new CommandPacketOut("say\n" + ChatBox.Text);
                Network.SendPacket(packet);
            }
            CloseChat();
        }

        void ToggleLink(UITextLink link, string n, int chan)
        {
            char c = '2';
            Channels[chan] = !Channels[chan];
            if (!Channels[chan])
            {
                c = '&';
            }
            link.Text = "^r^t^0^h^o^" + c + n;
            link.TextHover = "^!^e^t^0^h^o^" + c + n;
            link.TextClick = "^" + c + "^e^t^0^h^o^0" + n;
            UpdateChats();
        }

        bool WVis = false;

        public bool ChatBottomLastTick = true;

        public void TickChatSystem()
        {
            if (IsChatVisible())
            {
                ChatBottomLastTick = ChatIsAtBottom();
                if (ChatBox.TriedToEscape)
                {
                    CloseChat();
                    return;
                }
                if (!WVis)
                {
                    KeyHandler.GetKBState();
                    WVis = true;
                }
                ChatBox.Selected = true;
            }
            else
            {
                ChatBottomLastTick = true;
            }
        }

        public bool ShouldShowMessage(TextChannel channel)
        {
            return channel == TextChannel.CHAT || channel == TextChannel.BROADCAST || channel == TextChannel.IMPORTANT;
        }

        public void SetChatText(string text)
        {
            ChatBox.Text = text;
        }

        public string GetChatText()
        {
            return ChatBox.Text;
        }

        public void ShowChat()
        {
            if (!IsChatVisible())
            {
                KeyHandler.GetKBState();
                TheGameScreen.AddChild(ChatMenu);
                FixMouse();
            }
        }

        /// <summary>
        /// NOTE: Do not call this without good reason, let's not annoy players!
        /// </summary>
        public void CloseChat()
        {
            if (IsChatVisible())
            {
                KeyHandler.GetKBState();
                TheGameScreen.RemoveChild(ChatMenu);
                WVis = false;
                FixMouse();
                ChatBox.Clear();
            }
        }

        public void ClearChat()
        {
            ChatMessages.Clear();
            for (int i = 0; i < 100; i++)
            {
                ChatMessage cm = new ChatMessage() { Channel = TextChannel.ALWAYS, Text = "", Sent = 0 };
                ChatMessages.Add(cm);
            }
            UpdateChats();
        }

        public bool IsChatVisible()
        {
            return TheGameScreen.HasChild(ChatMenu);
        }

        public void WriteMessage(TextChannel channel, string message)
        {
            FontSets.Standard.MeasureFancyText(message, pushStr: true);
            bool bottomed = ChatIsAtBottom();
            UIConsole.WriteLine(channel + ": " + message);
            ChatMessage cm = new ChatMessage() { Channel = channel, Text = message, Sent = GlobalTickTimeLocal };
            ChatMessages.Add(cm);
            if (ChatMessages.Count > 550)
            {
                ChatMessages.RemoveRange(0, 50);
            }
            UpdateChats();
            if (bottomed)
            {
                ChatScrollToBottom();
            }
        }
        
        public int ChatBottom = 0;

        public void ChatScrollToBottom()
        {
            ChatScroller.Scroll = ChatBottom;
        }

        public bool ChatIsAtBottom()
        {
            return ChatScroller.Scroll >= (ChatBottom - 5);
        }
        
        public void UpdateChats()
        {
            ChatScroller.RemoveAllChildren();
            float by = 0;
            for (int i = 0; i < ChatMessages.Count; i++)
            {
                if (Channels[(int)ChatMessages[i].Channel])
                {
                    by += FontSets.Standard.font_default.Height;
                    int y = (int)by;
                    string ch = (ChatMessages[i].Channel == TextChannel.ALWAYS) ? "" : (ChatMessages[i].Channel.ToString() + ": ");
                    ChatScroller.AddChild(new UILabel(ch + ChatMessages[i].Text, FontSets.Standard, new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).ConstantXY(0, y).GetterWidth(() => (int)ChatScroller.Position.Width)));
                }
            }
            by += FontSets.Standard.font_default.Height;
            ChatBottom = (int)(by - ChatScroller.Position.Height);
            ChatScroller.MaxScroll = ChatBottom;
        }

        public void ChatRenderRecent()
        {
            int y = (Window.Height / 2);
            for (int i = ChatMessages.Count - 1; i >= 0; i--)
            {
                if (ShouldShowMessage(ChatMessages[i].Channel) && ChatMessages[i].Sent != 0 && ChatMessages[i].Sent + 5 > GlobalTickTimeLocal)
                {
                    y -= (int)FontSets.Standard.font_default.Height;
                    float tm = 1;
                    if (ChatMessages[i].Sent + 2.5 < GlobalTickTimeLocal)
                    {
                        tm = (float)((ChatMessages[i].Sent + 5) - GlobalTickTimeLocal) * (1.0f / 2.5f);
                    }
                    FontSets.Standard.DrawColoredText(ChatMessages[i].Text, new Location(5, y, 0), transmod: tm);
                }
                else
                {
                    break;
                }
            }
        }
    }
}
