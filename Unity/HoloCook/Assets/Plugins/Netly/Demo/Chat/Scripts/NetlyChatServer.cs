using Byter;
using Netly;
using System;
using UnityEngine;

public class NetlyChatServer
{
    public NetlyChatServer(NetlyChat chat)
    {
        var server = new TcpServer();

        // add function if click on send button
        chat.sendButton.onClick.AddListener(() =>
        {
            if (!server.IsOpened) return;

            // write data
            using Writer w = new Writer();
            w.Write("*"); // ID
            w.Write(chat.isClient);
            w.Write("*");
            w.Write(chat.sendIF.text);

            // write message on screen
            if (chat.PrintOnScreen(w.GetBytes(), true))
            {
                // broadcast event to clients
                server.ToEvent("chat", w.GetBytes());
            }

            // clean input field
            chat.sendIF.text = string.Empty;
        });

        server.OnOpen(() =>
        {
            Debug.Log("Server: OnOpen");

            chat.auth.SetActive(false);
            chat.scrollView.SetActive(true);
            chat.loadingPanel.SetActive(false);
        });

        server.OnError((e) =>
        {
            Debug.Log("Server: OnError");
            chat.ShowError("Connection Error", e.Message);

            chat.started = false;
            chat.auth.SetActive(true);
            chat.scrollView.SetActive(false);
            chat.loadingPanel.SetActive(false);
        });

        server.OnClose(() =>
        {
            Debug.Log("Server: OnClose");
            chat.ShowError("Connection Closed", null);

            chat.started = false;
            chat.auth.SetActive(true);
            chat.scrollView.SetActive(false);
            chat.loadingPanel.SetActive(false);
        });

        server.OnEvent((client, name, data) =>
        {
            Debug.Log("Server: OnEvent");

            if (name == "chat")
            {
                // write message on screen
                if (chat.PrintOnScreen(data, false))
                {
                    server.ToEvent("chat", data);
                }
            }
        });

        // open connection
        server.Open(chat.host);
        Debug.Log("Init Server");
    }
}