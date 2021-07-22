using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using Common;
using Fleck;
using LineLiveSitePlugin;
using MildomSitePlugin;
using MirrativSitePlugin;
using Newtonsoft.Json;
using NicoSitePlugin;
using OpenrecSitePlugin;
using PeriscopeSitePlugin;
using Plugin;
using SitePlugin;
using TwicasSitePlugin;
using TwitchSitePlugin;
using WhowatchSitePlugin;
using YouTubeLiveSitePlugin;

namespace SocketPlugin
{
    static class MessageParts
    {
        public static string ToTextWithImageAlt(this IEnumerable<IMessagePart> parts)
        {
            string s = "";
            if (parts != null)
            {
                foreach (var part in parts)
                {
                    if (part is IMessageText text)
                    {
                        s += text;
                    }
                    else if (part is IMessageImage image)
                    {
                        s += image.Alt;
                    }
                }
            }
            return s;
        }
    }
    [Export(typeof(IPlugin))]
    public class SocketPlugin : IPlugin, IDisposable
    {
        static object lockObj = new object();
        static List<IWebSocketConnection> sockets = new List<IWebSocketConnection>();
        static List<IWebSocketConnection> sockets2 = new List<IWebSocketConnection>();

        public string Name => "NeosVRPlugin";

        public string Description => "Comment:34560,JsonParser:34561";


        public IPluginHost Host { get; set; }

        public void Dispose()
        {

        }

        public void OnClosing()
        {

        }

        public void OnLoaded()
        {

            var server = new WebSocketServer("ws://127.0.0.1:34560");
            var server2 = new WebSocketServer("ws://127.0.0.1:34561");

            server.Start(socket =>
            {
                socket.OnOpen = () => OnOpen(socket);
                socket.OnClose = () => OnClose(socket);
                socket.OnMessage = message => OnMessage(socket, message);
            }); 
            server2.Start(socket =>
            {
                socket.OnOpen = () => OnOpen2(socket);
                socket.OnClose = () => OnClose2(socket);
                socket.OnMessage = message => OnMessage2(socket, message);
            });
        }

        public void OnMessageReceived(global::SitePlugin.ISiteMessage message, global::SitePlugin.IMessageMetadata messageMetadata)
        {
            
            var (service, type, name, body, iconUrl) = GetData(message);

            var data = new JsonClass
            {
                Service = service,
                Type = type,
                Name = name,
                Body = body,
                IconUrl = iconUrl
            };

            using (var ms = new MemoryStream())
            using (var sr = new StreamReader(ms))
            {
                var serializer = new DataContractJsonSerializer(typeof(JsonClass));

                serializer.WriteObject(ms, data);
                ms.Position = 0;

                var json = sr.ReadToEnd();

                foreach (var item in sockets)
                {
                    string[] path;
                    if (item.ConnectionInfo.Path.EndsWith("/"))
                        path = item.ConnectionInfo.Path.Substring(1, item.ConnectionInfo.Path.Length - 2).Split('/');
                    else
                        path = item.ConnectionInfo.Path.Substring(1, item.ConnectionInfo.Path.Length - 1).Split('/');

                    if (path.Length == 2)
                    {
                        var connection = Host.GetAllConnectionStatus().Where(x => x.Name == path[1]).FirstOrDefault();
                        if (connection == null)
                            break;

                        var guid = new Guid(connection.Guid);
                        if (messageMetadata.SiteContextGuid != guid)
                            continue;
                    }
                    switch (path[0])
                    {
                        default:
                            break;
                        case "body":
                            item.Send(data.Body);
                            break;
                        case "name":
                            item.Send(data.Name);
                            break;
                        case "type":
                            item.Send(data.Type);
                            break;
                        case "service":
                            item.Send(data.Service);
                            break;
                        case "iconurl":
                            item.Send(data.IconUrl);
                            break;
                        case "json":
                            item.Send(json);
                            break;
                        case "command":
                            break;
                    }
                }
            }
        }

        public void OnTopmostChanged(bool isTopmost)
        {

        }

        public void ShowSettingView()
        {

        }
        void OnOpen(IWebSocketConnection socket)
        {
            lock (lockObj)
            {
                sockets.Add(socket);
            }
        }
        void OnClose(IWebSocketConnection socket)
        {
            lock (lockObj)
            {
                sockets.Remove(socket);
            }
        }
        void OnOpen2(IWebSocketConnection socket)
        {
            lock (lockObj)
            {
                sockets2.Add(socket);
            }
        }
        void OnClose2(IWebSocketConnection socket)
        {
            lock (lockObj)
            {
                sockets2.Remove(socket);
            }
        }
        void OnMessage(IWebSocketConnection socket, string message)
        {
            socket.Send(message);
            
        }
        void OnMessage2(IWebSocketConnection socket, string message)
        {
            if (message.StartsWith("Service"))
            {
                var deserialized = JsonConvert.DeserializeObject<JsonClass>(message.Substring(7));
                socket.Send(deserialized.Service);
            }
            else if (message.StartsWith("Type"))
            {
                var deserialized = JsonConvert.DeserializeObject<JsonClass>(message.Substring(4));
                socket.Send(deserialized.Type);
            }
            else if (message.StartsWith("Name"))
            {
                var deserialized = JsonConvert.DeserializeObject<JsonClass>(message.Substring(4));
                socket.Send(deserialized.Name);
            }
            else if (message.StartsWith("Body"))
            {
                var deserialized = JsonConvert.DeserializeObject<JsonClass>(message.Substring(4));
                socket.Send(deserialized.Body);
            }
        }

        void SendMessageToAll(string message)
        {
            lock (lockObj)
            {
                // remove unused sockets
                List<IWebSocketConnection> soc = new List<IWebSocketConnection>();
                foreach (var s in sockets)
                {
                    if (s.IsAvailable)
                    {
                        soc.Add(s);
                    }
                    sockets = soc;
                }

                // send message
                foreach (var s in sockets)
                {
                    s.Send(message);
                }
            }
        }

        [DataContract]
        private class JsonClass
        {
            [DataMember]
            public string Service { get; set; }
            [DataMember]
            public string Type { get; set; }
            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public string Body { get; set; }
            [DataMember]
            public string IconUrl { get; set; }
        }
        private class MessageClass
        {
            public string Method { get; set; }
            public string Arg { get; set; }
        }

        private static (string service, string type, string name, string body, string iconUrl) GetData(ISiteMessage message)
        {
            string service = null;
            string type = null;
            string name = null;
            string body = null;
            string iconUrl = null;
            if (false) { }
            else if (message is IYouTubeLiveMessage youTubeLiveMessage)
            {
                service = "YoutubeLive";
                switch (youTubeLiveMessage.YouTubeLiveMessageType)
                {
                    case YouTubeLiveMessageType.Connected:
                        type = "Connected";
                        name = null;
                        body = (youTubeLiveMessage as IYouTubeLiveConnected).Text;
                        break;
                    case YouTubeLiveMessageType.Disconnected:
                        type = "Disconnected";
                        name = null;
                        body = (youTubeLiveMessage as IYouTubeLiveDisconnected).Text;
                        break;
                    case YouTubeLiveMessageType.Comment:
                        type = "Comment";
                        name = (youTubeLiveMessage as IYouTubeLiveComment).NameItems.ToText();
                        //body = (youTubeLiveMessage as IYouTubeLiveComment).CommentItems.ToTextWithImageAlt();
                        body = (youTubeLiveMessage as IYouTubeLiveComment).CommentItems.ToText();
                        iconUrl = (youTubeLiveMessage as IYouTubeLiveComment).UserIcon.Url;
                        break;
                    case YouTubeLiveMessageType.Superchat:
                        type = "Superchat";
                        name = (youTubeLiveMessage as IYouTubeLiveSuperchat).NameItems.ToText();
                        body = (youTubeLiveMessage as IYouTubeLiveSuperchat).CommentItems.ToText();
                        iconUrl = (youTubeLiveMessage as IYouTubeLiveSuperchat).UserIcon.Url;
                        break;
                }
            }
            else if (message is IOpenrecMessage openrecMessage)
            {
                service = "OpenRec";
                switch (openrecMessage.OpenrecMessageType)
                {
                    case OpenrecMessageType.Connected:
                        type = "Connected";
                        name = null;
                        body = (openrecMessage as IOpenrecConnected).Text;
                        break;
                    case OpenrecMessageType.Disconnected:
                        type = "Disconnected";
                        name = null;
                        body = (openrecMessage as IOpenrecDisconnected).Text;
                        break;
                    case OpenrecMessageType.Comment:
                        type = "Comment";
                        name = (openrecMessage as IOpenrecComment).NameItems.ToText();
                        body = (openrecMessage as IOpenrecComment).MessageItems.ToText();
                        break;
                }
            }
            else if (message is ITwitchMessage twitchMessage)
            {
                service = "Twitch";
                switch (twitchMessage.TwitchMessageType)
                {
                    case TwitchMessageType.Connected:
                        type = "Connected";
                        name = null;
                        body = (twitchMessage as ITwitchConnected).Text;
                        break;
                    case TwitchMessageType.Disconnected:
                        type = "Disconnected";
                        name = null;
                        body = (twitchMessage as ITwitchDisconnected).Text;
                        break;
                    case TwitchMessageType.Comment:
                        type = "Comment";
                        name = (twitchMessage as ITwitchComment).DisplayName;
                        body = (twitchMessage as ITwitchComment).CommentItems.ToText();
                        iconUrl = (twitchMessage as ITwitchComment).UserIcon.Url;
                        break;
                }
            }
            else if (message is INicoMessage NicoMessage)
            {
                service = "NiconicoLive";
                switch (NicoMessage.NicoMessageType)
                {
                    case NicoMessageType.Connected:
                        type = "Connected";
                        name = null;
                        body = (NicoMessage as INicoConnected).Text;
                        break;
                    case NicoMessageType.Disconnected:
                        type = "Disconnected";
                        name = null;
                        body = (NicoMessage as INicoDisconnected).Text;
                        break;
                    case NicoMessageType.Comment:
                        type = "Comment";
                        if ((NicoMessage as INicoComment).Is184)
                            name = "184";
                        else
                        {
                            name = (NicoMessage as INicoComment).UserName;
                            iconUrl = (NicoMessage as INicoComment).ThumbnailUrl;
                        }
                        body = (NicoMessage as INicoComment).Text;
                        break;
                    case NicoMessageType.Item:
                        type = "Item";
                        body = (NicoMessage as INicoGift).Text;
                        break;
                    case NicoMessageType.Ad:
                        type = "Ad";
                        body = (NicoMessage as INicoAd).Text;
                        break;
                }
            }
            else if (message is ITwicasMessage twicasMessage)
            {
                service = "Twicas";
                switch (twicasMessage.TwicasMessageType)
                {
                    case TwicasMessageType.Connected:
                        type = "Connected";
                        name = null;
                        body = (twicasMessage as ITwicasConnected).Text;
                        break;
                    case TwicasMessageType.Disconnected:
                        type = "Disconnected";
                        name = null;
                        body = (twicasMessage as ITwicasDisconnected).Text;
                        break;
                    case TwicasMessageType.Comment:
                        type = "Comment";
                        name = (twicasMessage as ITwicasComment).UserName;
                        body = (twicasMessage as ITwicasComment).CommentItems.ToText();
                        iconUrl = (twicasMessage as ITwicasComment).UserIcon.Url;
                        break;
                    case TwicasMessageType.Item:
                        type = "Item";
                        name = (twicasMessage as ITwicasItem).UserName;
                        body = (twicasMessage as ITwicasItem).CommentItems.ToTextWithImageAlt();
                        iconUrl = (twicasMessage as ITwicasItem).UserIcon.Url;
                        break;
                }
            }
            else if (message is ILineLiveMessage lineLiveMessage)
            {
                service = "LineLive";
                switch (lineLiveMessage.LineLiveMessageType)
                {
                    case LineLiveMessageType.Connected:
                        type = "Connected";
                        name = null;
                        body = (lineLiveMessage as ILineLiveConnected).Text;
                        break;
                    case LineLiveMessageType.Disconnected:
                        type = "Disconnected";
                        name = null;
                        body = (lineLiveMessage as ILineLiveDisconnected).Text;
                        break;
                    case LineLiveMessageType.Comment:
                        type = "Comment";
                        name = (lineLiveMessage as ILineLiveComment).DisplayName;
                        body = (lineLiveMessage as ILineLiveComment).Text;
                        iconUrl = (lineLiveMessage as ILineLiveComment).UserIconUrl;
                        break;
                }
            }
            else if (message is IWhowatchMessage whowatchMessage)
            {
                service = "Whowatch";
                switch (whowatchMessage.WhowatchMessageType)
                {
                    case WhowatchMessageType.Connected:
                        type = "Connected";
                        name = null;
                        body = (whowatchMessage as IWhowatchConnected).Text;
                        break;
                    case WhowatchMessageType.Disconnected:
                        type = "Disconnected";
                        name = null;
                        body = (whowatchMessage as IWhowatchDisconnected).Text;
                        break;
                    case WhowatchMessageType.Comment:
                        type = "Comment";
                        name = (whowatchMessage as IWhowatchComment).UserName;
                        body = (whowatchMessage as IWhowatchComment).Comment;
                        iconUrl = (whowatchMessage as IWhowatchComment).UserIcon.Url;
                        break;
                    case WhowatchMessageType.Item:
                        type = "Item";
                        name = (whowatchMessage as IWhowatchItem).UserName;
                        body = (whowatchMessage as IWhowatchItem).Comment;
                        iconUrl = (whowatchMessage as IWhowatchItem).UserIconUrl;
                        break;
                }
            }
            else if (message is IMirrativMessage mirrativMessage)
            {
                service = "Mirrativ";
                switch (mirrativMessage.MirrativMessageType)
                {
                    case MirrativMessageType.Connected:
                        type = "Connected";
                        name = null;
                        body = (mirrativMessage as IMirrativConnected).Text;
                        break;
                    case MirrativMessageType.Disconnected:
                        type = "Disconnected";
                        name = null;
                        body = (mirrativMessage as IMirrativDisconnected).Text;
                        break;
                    case MirrativMessageType.Comment:
                        type = "Comment";
                        name = (mirrativMessage as IMirrativComment).UserName;
                        body = (mirrativMessage as IMirrativComment).Text;
                        break;
                    case MirrativMessageType.JoinRoom:
                        type = "JoinRoom";
                        name = null;
                        body = (mirrativMessage as IMirrativJoinRoom).Text;
                        break;
                    case MirrativMessageType.Item:
                        type = "Item";
                        name = null;
                        body = (mirrativMessage as IMirrativItem).Text;
                        break;
                }
            }
            else if (message is IPeriscopeMessage PeriscopeMessage)
            {
                service = "Periscope";
                switch (PeriscopeMessage.PeriscopeMessageType)
                {
                    case PeriscopeMessageType.Connected:
                        type = "Connected";
                        name = null;
                        body = (PeriscopeMessage as IPeriscopeConnected).Text;
                        break;
                    case PeriscopeMessageType.Disconnected:
                        type = "Disconnected";
                        name = null;
                        body = (PeriscopeMessage as IPeriscopeDisconnected).Text;
                        break;
                    case PeriscopeMessageType.Comment:
                        type = "Comment";
                        name = (PeriscopeMessage as IPeriscopeComment).DisplayName;
                        body = (PeriscopeMessage as IPeriscopeComment).Text;
                        break;
                    case PeriscopeMessageType.Join:
                        type = "Join";
                        name = null;
                        body = (PeriscopeMessage as IPeriscopeJoin).Text;
                        break;
                    case PeriscopeMessageType.Leave:
                        type = "Leave";
                        name = null;
                        body = (PeriscopeMessage as IPeriscopeLeave).Text;
                        break;
                }
            }
            else if (message is IMildomMessage MildomMessage)
            {
                service = "Mildom";
                switch (MildomMessage.MildomMessageType)
                {
                    case MildomMessageType.Connected:
                        type = "Connected";
                        name = null;
                        body = (MildomMessage as IMildomConnected).Text;
                        break;
                    case MildomMessageType.Disconnected:
                        type = "Disconnected";
                        name = null;
                        body = (MildomMessage as IMildomDisconnected).Text;
                        break;
                    case MildomMessageType.Comment:
                        type = "Comment";
                        name = (MildomMessage as IMildomComment).UserName;
                        body = (MildomMessage as IMildomComment).CommentItems.ToText();
                        break;
                    case MildomMessageType.JoinRoom:
                        type = "JoinRoom";
                        name = null;
                        body = (MildomMessage as IMildomJoinRoom).CommentItems.ToText();
                        break;
                        //case MildomMessageType.Leave:
                        //    if (_options.IsMildomLeave)
                        //    {
                        //        name = null;
                        //        comment = (MildomMessage as IMildomLeave).CommentItems.ToText();
                        //    }
                        //    break;
                }
            }
            return (service, type, name, body, iconUrl);
        }
    }
}
