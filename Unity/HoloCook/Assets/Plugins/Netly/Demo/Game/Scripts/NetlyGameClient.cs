using Netly.Core;
using System.Collections.Generic;
using UnityEngine;
using Byter;
using System;
using TcpClient = Netly.TcpClient;
using UdpClient = Netly.UdpClient;

public class NetlyGameClient
{
    TcpClient tcp = new TcpClient();
    UdpClient udp = new UdpClient();
    string ID = Guid.NewGuid().ToString();
    List<NetlyGamePlayer> players = new List<NetlyGamePlayer>();
    Host udpHost = null;

    public NetlyGameClient(NetlyGame game)
    {
        // UDP
        udp.OnOpen(() =>
        {
            Debug.Log("UdpClient: OnOpen: " + udpHost.ToString());
        });

        udp.OnError((e) =>
        {
            Debug.Log("UdpClient: OnError: " + e.Message);
        });

        udp.OnClose(() =>
        {
            Debug.Log("UdpClient: OnClose");
        });

        udp.OnEvent((name, data) =>
        {
            if (name == "sync_transform")
            {
                using Reader r = new Reader(data);

                string m_id = r.Read<string>();

                float positionX = r.Read<float>();
                float positionY = r.Read<float>();
                float positionZ = r.Read<float>();

                float rotationX = r.Read<float>();
                float rotationY = r.Read<float>();
                float rotationZ = r.Read<float>();
                float rotationW = r.Read<float>();

                if (r.Success is false) return;

                if (m_id == ID) return;

                foreach (var p in players)
                {
                    if (p.Id == m_id)
                    {
                        p.Position = new Vector3(positionX, positionY, positionZ);
                        p.Rotation = new Quaternion(rotationX, rotationY, rotationZ, rotationW);
                        return;
                    }
                }
            }
        });


        // TCP
        tcp.OnOpen(() =>
        {
            Debug.Log("TcpClient: OnOpen: " + tcp.Host.ToString());

            game.auth.SetActive(false);
            game.loadingPanel.SetActive(false);

            // send login
            using Writer w = new Writer();
            w.Write(ID);
            w.Write(game.usernameIF.text);

            tcp.ToEvent("new_client", w.GetBytes());
        });

        tcp.OnError((e) =>
        {
            Debug.Log("TcpServer: OnError: " + e.Message);

            game.started = false;
            game.auth.SetActive(true);
            game.ShowError("Connection Errror", e.Message);
        });

        tcp.OnClose(() =>
        {
            Debug.Log("TcpServer: OnClose");

            game.started = false;
            game.auth.SetActive(true);
            game.ShowError("Connection Closed", "");
        });

        tcp.OnEvent((name, data) =>
        {
            if (name == "new_player")
            {
                // read
                using Reader r = new Reader(data);
                int count = r.Read<int>();

                for (int i = 0; i < count; i++)
                {
                    string m_id = r.Read<string>();
                    string m_name = r.Read<string>();

                    float m_posX = r.Read<float>();
                    float m_posY = r.Read<float>();
                    float m_posZ = r.Read<float>();

                    float m_rotX = r.Read<float>();
                    float m_rotY = r.Read<float>();
                    float m_rotZ = r.Read<float>();
                    float m_rotW = r.Read<float>();

                    if (r.Success is false)
                    {
                        break;
                    }

                    bool exist = false;
                    foreach (var p in players)
                    {
                        if (p.Id == m_id)
                        {
                            exist = true;
                            break;
                        }
                    }
                    if (exist) continue;

                    var pos = new Vector3(m_posX, m_posY, m_posZ);
                    var rot = new Quaternion(m_rotX, m_rotY, m_rotZ, m_rotW);

                    var player = GameObject.Instantiate(game.playerPrefab, pos, rot).GetComponent<NetlyGamePlayer>();
                    players.Add(player);

                    player.Id = m_id;
                    player.Name = m_name;
                    player.Position = pos;
                    player.Rotation = rot;
                    player.IsMain = (m_id == ID);
                    player.client = (m_id == ID) ? udp : null;
                }

                if (r.Success is false)
                {
                    return;
                }
            }
            else if (name == "remove_player")
            {
                using Reader r = new Reader(data);
                string m_id = r.Read<string>();
                string m_name = r.Read<string>();
                if (r.Success is false) return;

                foreach (var p in players)
                {
                    if (p.Id == m_id)
                    {
                        if (m_id == ID)
                        {
                            game.started = false;
                            game.auth.SetActive(true);
                            game.ShowError("Server Logic", "connection banned");
                        }

                        players.Remove(p);
                        p.Destroy();
                        return;
                    }
                }
            }
            else if (name == "udp_login")
            {
                // read request
                using Reader r = new Reader(data);
                string ip = r.Read<string>();
                int port = r.Read<int>();

                if (r.Success is false) return;

                // open udp connection
                udpHost = new Host(ip, port);
                Debug.Log("UDP LOGIN: " + udpHost.ToString());
                udp.Open(udpHost);
            }
        });

        // OPEN
        tcp.Open(game.host);
    }
}
