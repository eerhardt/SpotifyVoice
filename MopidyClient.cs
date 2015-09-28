using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace BackgroundApplication1
{
    class MopidyClient : IDisposable
    {
        private MessageWebSocket socket;
        private DataWriter messageWriter;
        private Dictionary<double, Action<JsonObject>> callbacks = new Dictionary<double, Action<JsonObject>>();
        private int currentId = 0;

        public MopidyClient()
        {
            socket = new MessageWebSocket();
            socket.Control.MessageType = SocketMessageType.Utf8;

            socket.Closed += (senderSocket, args) =>
            {
                // DO SOMETHING
            };
            socket.MessageReceived += (sender, args) =>
            {
                using (var reader = args.GetDataReader())
                {
                    reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                    string resultString = reader.ReadString(reader.UnconsumedBufferLength);
                    JsonObject message;
                    if (JsonObject.TryParse(resultString, out message))
                    {
                        IJsonValue idValue;
                        if (message.TryGetValue("id", out idValue))
                        {
                            double id = idValue.GetNumber();

                            Action<JsonObject> callback;
                            if (callbacks.TryGetValue(id, out callback))
                            {
                                callback(message);
                            }
                        }
                    }
                }
            };
            messageWriter = new DataWriter(socket.OutputStream);
        }

        public void Dispose()
        {
            messageWriter?.Dispose();
            messageWriter = null;
            socket?.Dispose();
            socket = null;
        }

        public async Task Open()
        {
            await socket.ConnectAsync(new Uri("ws://192.168.1.111:6680/mopidy/ws"));
        }

        private void Socket_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
        }

        public async Task<string> Search(string search)
        {
            using (AutoResetEvent handle = new AutoResetEvent(false))
            {
                JsonObject message = new JsonObject();
                message.Add("method", JsonValue.CreateStringValue("core.library.search"));

                JsonObject queryObject = new JsonObject();
                queryObject.Add("track_name", JsonValue.CreateStringValue(search));

                JsonArray urisArray = new JsonArray();
                urisArray.Add(JsonValue.CreateStringValue("spotify:"));

                JsonObject paramsObject = new JsonObject();
                paramsObject.Add("query", queryObject);
                paramsObject.Add("uris", urisArray);

                message.Add("params", paramsObject);

                string result = null;
                await Send(message, searchResult =>
                {
                    JsonArray tracks = searchResult.GetNamedArray("result")[0].GetObject().GetNamedArray("tracks");
                    result = tracks.First().GetObject().GetNamedString("uri");

                    handle.Set();
                });

                handle.WaitOne(TimeSpan.FromSeconds(30));
                return result;
            }
        }

        public async Task<string> SearchArtist(string artistName)
        {
            using (AutoResetEvent handle = new AutoResetEvent(false))
            {
                JsonObject message = new JsonObject();
                message.Add("method", JsonValue.CreateStringValue("core.library.search"));

                JsonObject queryObject = new JsonObject();
                queryObject.Add("artist", JsonValue.CreateStringValue(artistName));

                JsonArray urisArray = new JsonArray();
                urisArray.Add(JsonValue.CreateStringValue("spotify:"));

                JsonObject paramsObject = new JsonObject();
                paramsObject.Add("query", queryObject);
                paramsObject.Add("uris", urisArray);

                message.Add("params", paramsObject);

                string result = null;
                await Send(message, searchResult =>
                {
                    JsonArray resultArray = searchResult.GetNamedArray("result");
                    JsonArray artists = resultArray.FirstOrDefault()?.GetObject().GetNamedArray("artists");
                    if (artists != null)
                    {
                        result = artists.FirstOrDefault()?.GetObject().GetNamedString("uri");
                    }

                    handle.Set();
                });

                handle.WaitOne(TimeSpan.FromSeconds(30));
                return result;
            }
        }

        private async Task Send(JsonObject message, Action<JsonObject> callback = null)
        {
            double id = currentId++;
            message.SetNamedValue("jsonrpc", JsonValue.CreateStringValue("2.0"));
            message.SetNamedValue("id", JsonValue.CreateNumberValue(id));

            if (callback != null)
            {
                callbacks[id] = callback;
            }

            messageWriter.WriteString(message.Stringify());
            await messageWriter.StoreAsync();
        }

        public async Task Play(string trackUri)
        {
            // Buffer any data we want to send.
            messageWriter.WriteString(@"[
                {""jsonrpc"": ""2.0"", ""id"": 1, ""method"": ""core.tracklist.clear"" },
                { ""jsonrpc"": ""2.0"", ""id"": 1, ""method"": ""core.tracklist.add"", ""params"": { ""uri"": """ + trackUri + @"""} },
                { ""jsonrpc"": ""2.0"", ""id"": 1, ""method"": ""core.playback.play""}
            ]");

            // Send the data as one complete message.
            await messageWriter.StoreAsync();
        }

        public async Task Stop()
        {
            JsonObject message = new JsonObject();
            message.Add("method", JsonValue.CreateStringValue("core.tracklist.clear"));
            await Send(message);
        }

        public async Task<int> GetVolume()
        {
            JsonObject message = new JsonObject();
            message.Add("method", JsonValue.CreateStringValue("core.playback.get_volume"));

            using (AutoResetEvent handle = new AutoResetEvent(false))
            {
                int result = -1;
                await Send(message, volumeResult =>
                {
                    double volumeDouble = volumeResult.GetNamedNumber("result");
                    result = (int)volumeDouble;

                    handle.Set();
                });

                handle.WaitOne(TimeSpan.FromSeconds(30));
                return result;
            }

        }
        public async Task SetVolume(int volume)
        {
            JsonObject message = new JsonObject();
            message.Add("method", JsonValue.CreateStringValue("core.playback.set_volume"));

            JsonArray paramsArray = new JsonArray();
            paramsArray.Add(JsonValue.CreateNumberValue(volume));
            message.Add("params", paramsArray);

            await Send(message);
        }
    }
}
