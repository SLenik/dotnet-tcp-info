#nullable enable

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace TcpInfoExample
{
    internal class Program
    {
        private static async Task Main()
        {
            // У меня нет примера url-а для Streaming API, которым я мог бы поделиться,
            // поэтому указываю для примера ссылку на достаточно большой файл,
            // который не завершит скачивание к моменту окончания выполнения программы
            const string url = @"http://mirror.mephi.ru/debian-cd/12.6.0/amd64/iso-dvd/debian-12.6.0-amd64-DVD-1.iso";

            Stream? stream = null;
            var disposables = Array.Empty<IDisposable>(); // Ещё проще было бы написать с CompositeDisposable
            try
            {
                (stream, disposables) = await GetStreamWithWebRequestAsync(url);

                var socket = GetSocketFromStream(stream);
                if (socket == null)
                {
                    Console.WriteLine("Ошибка при получении данных сокета");
                    return;
                }

                var tcpInfo = WinTcpInfo.GetTcpInfoV0(socket);
                if (tcpInfo == null)
                {
                    Console.WriteLine("Ошибка при получении TCP-данных");
                    return;
                }

                Console.WriteLine(PrettySerializeTcpInfoV0(tcpInfo.Value));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                foreach (var disposable in disposables)
                    disposable.Dispose();

                stream?.Dispose();
            }
        }

        private static async Task<(Stream, IDisposable[])> GetStreamWithHttpClientAsync(string url)
        {
            // Внимание! Создание HttpClient на каждый запрос вредно!
            // Здесь так написано просто чтобы код подключения был максимально коротким.
            // Подробнее как надо - см. HttpClientFactory и https://www.aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
            var httpClient = new HttpClient();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            var responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            var stream = await responseMessage.Content.ReadAsStreamAsync();

            return (stream, new IDisposable[] { responseMessage, requestMessage, httpClient });
        }

        private static async Task<(Stream, IDisposable[])> GetStreamWithWebRequestAsync(string url)
        {
            var webRequest = WebRequest.Create(url);
            var webResponse = await webRequest.GetResponseAsync();
            var stream = webResponse.GetResponseStream();
            return (stream, new IDisposable[] { webResponse });
        }

        private static readonly JsonSerializerOptions Options =
            new JsonSerializerOptions
            {
                IncludeFields = true,
                WriteIndented = true
            };

        private static string PrettySerializeTcpInfoV0(TcpInfoV0 tcpInfo)
        {
            return JsonSerializer.Serialize(tcpInfo, Options);
        }

#if NET45_OR_GREATER
        private static readonly Type ConnectStreamType =
            Type.GetType("System.Net.ConnectStream, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")!;
        private static readonly PropertyInfo ConnectStreamInternalSocketProperty =
            ConnectStreamType.GetProperty("InternalSocket", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

        private static readonly Type DelegatingStreamType =
            Type.GetType("System.Net.Http.DelegatingStream, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")!;
        private static readonly FieldInfo DelegatingStreamInnerStreamField =
            DelegatingStreamType.GetField("innerStream", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        private static readonly Type WebExceptionWrapperStreamType =
            Type.GetType("System.Net.Http.HttpClientHandler+WebExceptionWrapperStream, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")!;
        private static readonly FieldInfo WebExceptionWrapperStreamInnerStreamField =
            DelegatingStreamType.GetField("innerStream", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

        private static Socket? GetSocketFromStream(Stream? stream)
        {
            if (stream == null)
                return null;

            // .NET Framework, HttpClient - приводим код к WebRequest
            if (DelegatingStreamType.IsInstanceOfType(stream))
                stream = (Stream?)DelegatingStreamInnerStreamField.GetValue(stream);
            if (WebExceptionWrapperStreamType.IsInstanceOfType(stream))
                stream = (Stream?)WebExceptionWrapperStreamInnerStreamField.GetValue(stream);

            // .NET Framework, WebRequest
            if (ConnectStreamType.IsInstanceOfType(stream))
                return (Socket)ConnectStreamInternalSocketProperty.GetValue(stream);

            return null;
        }
#elif NET
        private static readonly Type HttpContentStreamType = Type.GetType("System.Net.Http.HttpContentStream, System.Net.Http")!;
        private static readonly FieldInfo HttpContentStreamConnectionField =
            HttpContentStreamType.GetField("_connection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        private static readonly Type HttpConnectionType = Type.GetType("System.Net.Http.HttpConnection, System.Net.Http")!;
        private static readonly FieldInfo HttpConnectionSocketField =
            HttpConnectionType.GetField("_socket", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

        private static Socket? GetSocketFromStream(Stream? stream)
        {
            if (HttpContentStreamType.IsInstanceOfType(stream))
            {
                var httpConnection = HttpContentStreamConnectionField.GetValue(stream);
                if (httpConnection == null)
                    return null;

                return (Socket?)HttpConnectionSocketField.GetValue(httpConnection);
            }

            return null;
        }
#endif
    }
}
