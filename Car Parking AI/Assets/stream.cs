using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Net;
using System.IO;
using System.Security.Authentication;

public class stream : MonoBehaviour
{
    #region private members
    private TcpClient socketConnection;
    private Thread clientReceiveThread;
    #endregion
    private Texture2D texture;
    public Camera cam2;
    private RenderTexture rt;
    WaitForEndOfFrame frameEnd = new WaitForEndOfFrame();

    void Start()
    {
        socketConnection = new TcpClient("localhost", 8010);
    }


    void Update()
    {
        NetworkStream _stream_ = socketConnection.GetStream();
        StartCoroutine(streaming(_stream_));
    }


    IEnumerator streaming(NetworkStream stream)
    {
        yield return frameEnd;

        rt = new RenderTexture((int)Screen.width, (int)Screen.height, 24);
        cam2.targetTexture = rt;
        cam2.Render();
        RenderTexture.active = rt;
        texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
        texture.Apply();
        byte[] frameByte = texture.EncodeToPNG();
        cam2.targetTexture = null;
        RenderTexture.active = null;
        GameObject.Destroy(rt);
        Destroy(texture);
        Thread.Sleep(1);
        stream.Write(frameByte, 0, frameByte.Length);
    }
}
