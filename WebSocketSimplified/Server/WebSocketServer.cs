using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace WebSocketSimplified.Server
{
    public class WebSocketServer
    {
        TcpListener server;
        static Dictionary<string, TcpClient> connectedClients = new Dictionary<string, TcpClient>();

        public void StartWebSocketServer(string ipAddress, int port)
        {
            server = new TcpListener(IPAddress.Parse(ipAddress), port);
            server.Start();
            Debug.WriteLine("Server has started on localhost. {0} Waiting for a connection...", Environment.NewLine);

            StartClient();
        }

        public void StartClient()
        {
            TcpClient client = server.AcceptTcpClient();

            while (true)
            {

                NetworkStream stream = client.GetStream();
                while (!stream.DataAvailable) ;

                Byte[] bytes = new Byte[client.Available];
                stream.Read(bytes, 0, bytes.Length);
                String data = Encoding.UTF8.GetString(bytes);

                if (new Regex("^GET").IsMatch(data))
                {
                    Handshaking(client, data);
                    Debug.WriteLine("After Handshake, add client to dictionary : " + client.Client.RemoteEndPoint.ToString());
                    connectedClients.Add(client.Client.RemoteEndPoint.ToString(), client);
                }
                else
                {
                    String msg = DecodeMessageFromClient(bytes);
                    Debug.WriteLine("Message From Client : " + msg);
                }

            }
        }

        private void Handshaking(Object newClient, String data)
        {
            try
            {
                TcpClient client = (TcpClient)newClient;
                NetworkStream stream = client.GetStream();

                Byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                + "Connection: Upgrade" + Environment.NewLine
                + "Upgrade: websocket" + Environment.NewLine
                + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                    SHA1.Create().ComputeHash(
                        Encoding.UTF8.GetBytes(
                            new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                        )
                    )
                ) + Environment.NewLine
                + Environment.NewLine);

                stream.Write(response, 0, response.Length);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error during handshaking : " + ex.Message);
            }

        }

        public void SendMessageToClient(string msg)
        {
            foreach(var c in connectedClients) {
                TcpClient cli = c.Value;
                NetworkStream str = cli.GetStream();
                byte[] bytesFromServer = EncodeMessageToSend(msg);
                str.Write(bytesFromServer, 0, bytesFromServer.Length);
            }
        }

        private String DecodeMessageFromClient(Byte[] bytes)
        {
            try
            {
                String incomingData = String.Empty;
                Byte secondByte = bytes[1];

                Int32 dataLength = secondByte & 127;
                Int32 indexFirstMask = 2;

                if (dataLength == 126) indexFirstMask = 4;
                else if (dataLength == 127) indexFirstMask = 10;

                IEnumerable<Byte> keys = bytes.Skip(indexFirstMask).Take(4);
                Int32 indexFirstDataByte = indexFirstMask + 4;

                Byte[] decoded = new Byte[bytes.Length - indexFirstDataByte];
                for (Int32 i = indexFirstDataByte, j = 0; i < bytes.Length; i++, j++)
                {
                    decoded[j] = (Byte)(bytes[i] ^ keys.ElementAt(j % 4));
                }

                return incomingData = Encoding.UTF8.GetString(decoded, 0, decoded.Length);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not decode due to :" + ex.Message);
            }
            return null;
        }

        private static Byte[] EncodeMessageToSend(String message)
        {
            Byte[] response;
            Byte[] bytesRaw = Encoding.UTF8.GetBytes(message);
            Byte[] frame = new Byte[10];

            Int32 indexStartRawData = -1;
            Int32 length = bytesRaw.Length;

            frame[0] = (Byte)129;
            if (length <= 125)
            {
                frame[1] = (Byte)length;
                indexStartRawData = 2;
            }
            else if (length >= 126 && length <= 65535)
            {
                frame[1] = (Byte)126;
                frame[2] = (Byte)((length >> 8) & 255);
                frame[3] = (Byte)(length & 255);
                indexStartRawData = 4;
            }
            else
            {
                frame[1] = (Byte)127;
                frame[2] = (Byte)((length >> 56) & 255);
                frame[3] = (Byte)((length >> 48) & 255);
                frame[4] = (Byte)((length >> 40) & 255);
                frame[5] = (Byte)((length >> 32) & 255);
                frame[6] = (Byte)((length >> 24) & 255);
                frame[7] = (Byte)((length >> 16) & 255);
                frame[8] = (Byte)((length >> 8) & 255);
                frame[9] = (Byte)(length & 255);

                indexStartRawData = 10;
            }

            response = new Byte[indexStartRawData + length];

            Int32 i, reponseIdx = 0;

            //Add the frame bytes to the reponse
            for (i = 0; i < indexStartRawData; i++)
            {
                response[reponseIdx] = frame[i];
                reponseIdx++;
            }

            //Add the data bytes to the response
            for (i = 0; i < length; i++)
            {
                response[reponseIdx] = bytesRaw[i];
                reponseIdx++;
            }

            return response;
        }
    }
}