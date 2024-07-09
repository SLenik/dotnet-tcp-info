#nullable enable

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace TcpInfoExample
{
// Код на основе https://stackoverflow.com/questions/59368554/access-tcp-retransmitted-byte-count-and-retransmitted-packet-count-for-a-given
/// <summary>
/// Простой статический класс для получения TCP-статистики по сокету.
/// </summary>
/// <remarks>Не является потокобезопасным.</remarks>
    public static unsafe class WinTcpInfo
{
    private const int SIO_TCP_INFO = unchecked((int)0xD8000027);

    private static readonly byte[] ZeroInValue = BitConverter.GetBytes(0);

    public static TcpInfoV0? GetTcpInfoV0(Socket socket)
    {
        var optionOutValue = new byte[sizeof(TcpInfoV0)];

        if (socket.IOControl(SIO_TCP_INFO, ZeroInValue, optionOutValue) <= 0)
            return null;

        var handle = GCHandle.Alloc(optionOutValue, GCHandleType.Pinned);
        var result = Marshal.PtrToStructure<TcpInfoV0>(handle.AddrOfPinnedObject());
        handle.Free();
        return result;
    }
}
}
