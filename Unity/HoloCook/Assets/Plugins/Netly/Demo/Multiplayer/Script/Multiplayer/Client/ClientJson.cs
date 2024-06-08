using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class ClientJson
{
    [Serializable]
    public class LoginList
    {
        public List<Login> logins;

        public LoginList(List<Login> logins)
        {
            this.logins = logins;
        }
    }


    [Serializable]
    public class Login
    {
        public int id;
        public string username;
        public string role;
        public Vector3 position;
        public Vector3 rotation;
        
        public Login(int id, string username)
        {
            this.id = id;
            this.username = username;
        }

        /// <summary>
        /// Client Version
        /// </summary>
        public Login(int id, string username, string role)
        {
            this.id = id;
            this.username = username;
            this.role = role; // indicate the server's role
        }

        /// <summary>
        /// Server Version
        /// </summary>
        public Login(int id, string username, Vector3 position, Vector3 rotation)
        {
            this.id = id;
            this.username = username;
            this.position = position;
            this.rotation = rotation;
        }
    }

    [Serializable]
    public class Logout
    {
        public int id;

        public Logout(int id)
        {
            this.id = id;
        }
    }

    [Serializable]
    public class Transform
    {
        public int id;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;

        public Transform(int id, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            this.id = id;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }
    }

    [Serializable]
    public class SyncDirection
    {
        public int id;
        public string username;
        public bool client2Server;
        public bool server2Client;

        public SyncDirection(int id, string username, bool c2s, bool s2c)
        {
            this.id = id;
            this.username = username;
            this.client2Server = c2s;
            this.server2Client = s2c;
        }
    }

}
