using Byter;
using Netly;
using Netly.Core;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetlyGameServer
{
    TcpServer tcp = new TcpServer();
    UdpServer udp = new UdpServer();
    List<NetlyGamePlayer> players = new List<NetlyGamePlayer>();
    Host udpHost = null;

    public NetlyGameServer(NetlyGame game)
    {
        // UDP
        udp.OnOpen(() =>
        {
            udpHost = udp.Host;
            Debug.Log("UdpServer: OnOpen: " + udpHost.ToString());
        });

        udp.OnModify((socket) =>
        {
            Debug.Log("UdpServer: OnModify");
        });


        udp.OnEvent((server, name, data) =>
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

                foreach (var p in players)
                {
                    if (p.Id == m_id)
                    {
                        p.Position = new Vector3(positionX, positionY, positionZ);
                        p.Rotation = new Quaternion(rotationX, rotationY, rotationZ, rotationW);

                        udp.ToEvent("sync_transform", data);
                        return;
                    }
                }
            }
        });


        // TCP
        tcp.OnOpen(() =>
        {
            Debug.Log("TcpServer: OnOpen: " + tcp.Host.ToString());

            game.auth.SetActive(false);
            game.loadingPanel.SetActive(false);
            udpHost = new Host(game.host.Address, 0);
            udp.Open(udpHost);
        });

        tcp.OnError((e) =>
        {
            Debug.Log("TcpServer: OnError: " + e.ToString());

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

        tcp.OnEvent((client, name, data) =>
        {
            if (name == "new_client")
            {
                // read
                using Reader r = new Reader(data);
                string m_id = r.Read<string>();
                string m_name = r.Read<string>();
                if (r.Success is false) return;

                // check
                bool exist = false;
                foreach (var p in players)
                {
                    if (p.Id == m_id)
                    {
                        exist = true;
                        break;
                    }
                }
                if (exist) return;

                // instance
                var pos = new Vector3(Random.Range(-3, 3), 3, Random.Range(-3, 3));
                var rot = Quaternion.Euler(x: 0, y: Random.Range(0, 360), z: 0);

                var player = GameObject.Instantiate(game.playerPrefab, pos, rot).GetComponent<NetlyGamePlayer>();
                player.Id = m_id;
                player.Name = m_name;
                player.IsMain = false;
                player.Position = pos;
                player.Rotation = rot;
                player.UUID = client.UUID;

                players.Add(player);

                // write
                using Writer w = new Writer();

                w.Write(players.Count);

                foreach (var p in players)
                {
                    w.Write(p.Id);
                    w.Write(p.Name);

                    w.Write(p.Position.x);
                    w.Write(p.Position.y);
                    w.Write(p.Position.z);

                    w.Write(p.Rotation.x);
                    w.Write(p.Rotation.y);
                    w.Write(p.Rotation.z);
                    w.Write(p.Rotation.w);
                }

                tcp.ToEvent("new_player", w.GetBytes());

                // udp
                using Writer wUdp = new Writer();
                wUdp.Write(udpHost.Address.ToString());
                wUdp.Write(udpHost.Port);
                client.ToEvent("udp_login", wUdp.GetBytes());
            }
        });

        tcp.OnExit((client) =>
        {
            foreach (var p in players)
            {
                if (p.UUID == client.UUID)
                {
                    using Writer w = new Writer();
                    w.Write(p.Id);
                    w.Write(p.Name);

                    tcp.ToEvent("remove_player", w.GetBytes());

                    players.Remove(p);
                    p.Destroy();
                    return;
                }
            }
        });

        // OPEN
        tcp.Open(game.host);
    }
}
