using Byter;
using Netly;
using System;
using UnityEngine;

public class NetlyChatClient
{
    public NetlyChatClient(NetlyChat chat)
    {
        var client = new TcpClient();
        var ID = Guid.NewGuid().ToString();

        // add function if click on send button
        chat.sendButton.onClick.AddListener(() =>
        {
            if (!client.IsOpened) return;

            // write data
            using Writer w = new Writer();
            w.Write(ID);
            w.Write(chat.isClient);
            w.Write(chat.usernameIF.text);
            w.Write(chat.sendIF.text);

            // write message on screen
            if (chat.PrintOnScreen(w.GetBytes(), true))
            {
                // send message to server
                client.ToEvent("chat", w.GetBytes());
            }

            // clean input field
            chat.sendIF.text = string.Empty;
        });

        client.OnOpen(() =>
        {
            Debug.Log("Client: OnOpen");

            chat.auth.SetActive(false);
            chat.scrollView.SetActive(true);
            chat.loadingPanel.SetActive(false);
        });

        client.OnError((e) =>
        {
            Debug.Log("Server: OnError");
            chat.ShowError("Connection Error", e.Message);

            chat.started = false;
            chat.auth.SetActive(true);
            chat.scrollView.SetActive(false);
            chat.loadingPanel.SetActive(false);
        });

        client.OnClose(() =>
        {
            Debug.Log("Client: OnClose");
            chat.ShowError("Connection Closed", null);

            chat.started = false;
            chat.auth.SetActive(true);
            chat.scrollView.SetActive(false);
            chat.loadingPanel.SetActive(false);
        });

        client.OnEvent((name, data) =>
        {
            Debug.Log("Client: OnEvent");

            if (name == "chat")
            {
                // write message on screen
                using Reader r = new Reader(data);
                string id = r.Read<string>();
                // return is message if mine
                if (r.Success is false || id == ID) return;

                chat.PrintOnScreen(data, false);
            }
        });

        // open connection
        client.Open(chat.host);
        Debug.Log("Init Client");
    }
}