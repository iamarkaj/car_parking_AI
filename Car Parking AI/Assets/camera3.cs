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

public class camera3 : MonoBehaviour
{
    static int stack_frames_size = 4;
    static int BATCH_SIZE = 256;

    #region private members
    private TcpClient socketConnection;
    private Thread clientReceiveThread;
    #endregion
    public string action;
    int end_of_batch = BATCH_SIZE * (stack_frames_size*2 + 3);  //x old frames + 1 action + x new frames + 1 reward + 1 extra for frameCounter reset
    string readyToExchangeData;
    byte[] readyToExchangeDataByte = new byte[1];
    int readyToExchangeDataByteLength = 0;
    byte[] frameByteLength = new byte[15];
    byte[] frameByteLength_ = new byte[15];
    byte[] actionByte = new byte[1];
    byte[] sendRewardbyte = new byte[10];
    bool sendOldFrame = false;
    bool receiveAction = false;
    bool sendNewFrame = false;
    bool sendReward = false;
    int frameCounter = 0;
    int batchCounter = 0;
    WaitForEndOfFrame frameEnd = new WaitForEndOfFrame();

    public Camera cam3;

    private Texture2D texture;
    private RenderTexture rt;

    private Texture2D texture_;
    private RenderTexture rt_;

    void Start()
    {
        socketConnection = new TcpClient("localhost", 8013);
    }


    void Update()
    {
        NetworkStream _stream_ = socketConnection.GetStream();
        StartCoroutine(streaming(_stream_));
    }


    IEnumerator streaming(NetworkStream stream)
    {
        yield return frameEnd;
        if (batchCounter == 0 || batchCounter == end_of_batch)
        {
            //Debug.Log("Start... / Stop...");
            readyToExchangeDataByteLength = stream.Read(readyToExchangeDataByte, 0, readyToExchangeDataByte.Length);
            readyToExchangeData = Encoding.ASCII.GetString(readyToExchangeDataByte, 0, readyToExchangeDataByteLength);
        }


        if (readyToExchangeData == "0")
        {
            //Debug.Log("Waiting...");

            while(!stream.CanRead)
            {
                action = "STOP";
            }

            batchCounter = 0;

            readyToExchangeDataByteLength = stream.Read(readyToExchangeDataByte, 0, readyToExchangeDataByte.Length);
            readyToExchangeData = Encoding.ASCII.GetString(readyToExchangeDataByte, 0, readyToExchangeDataByteLength);

            //Debug.Log("Resuming ... ");
        }


        batchCounter += 1;
        frameCounter += 1;

        if (frameCounter >= 1 && frameCounter <= stack_frames_size)
        {
            sendOldFrame = true;
            //Debug.Log("Send old frame signal");
        }

        else if (frameCounter == (stack_frames_size + 1))
        {
            receiveAction = true;
            //Debug.Log("Receive action frame signal");
        }

        else if (frameCounter >= (stack_frames_size + 2) && frameCounter <= (stack_frames_size + 5))
        {
            sendNewFrame = true;
            //Debug.Log("Send new frame signal");
        }

        else if (frameCounter == (stack_frames_size + 6))
        {
            sendReward = true;
            //Debug.Log("Send reward signal");
        }

        else if (frameCounter == (stack_frames_size + 7))
        {
            frameCounter = 0;
            //Debug.Log("Reset frame counter signal");
        }


        if (sendOldFrame == true)
        {
            while(!stream.CanWrite)
            {
                action = "STOP";
            }
            rt = new RenderTexture((int)Screen.width, (int)Screen.height, 24);
            cam3.targetTexture = rt;
            cam3.Render();
            RenderTexture.active = rt;
            texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
            texture.Apply();
            byte[] frameByte = texture.EncodeToPNG();
            cam3.targetTexture = null;
            RenderTexture.active = null;
            GameObject.Destroy(rt);
            Destroy(texture);
            // send frameByteLength first
            frameByteLength = Encoding.ASCII.GetBytes(frameByte.Length.ToString());
            Thread.Sleep(1);
            stream.Write(frameByteLength, 0, frameByteLength.Length);
            // now send frameByte
            Thread.Sleep(1);
            stream.Write(frameByte, 0, frameByte.Length);
            //Debug.Log("Sent old frame");

            sendOldFrame = false;
        }

        if (receiveAction == true)
        {
            while(!stream.CanRead)
            {
                action = "STOP";
            }
            int actionByteLength = 0;
            actionByteLength = stream.Read(actionByte, 0, actionByte.Length);
            action = Encoding.ASCII.GetString(actionByte, 0, actionByteLength);
            //Debug.Log("Received action");

            receiveAction = false;
        }

        if (sendNewFrame == true)
        {
            rt_ = new RenderTexture((int)Screen.width, (int)Screen.height, 24);
            cam3.targetTexture = rt_;
            cam3.Render();
            RenderTexture.active = rt_;
            texture_ = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture_.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
            texture_.Apply();
            byte[] frameByte_ = texture_.EncodeToPNG();
            cam3.targetTexture = null;
            RenderTexture.active = null;
            GameObject.Destroy(rt_);
            Destroy(texture_);
            // send frameByteLength first
            frameByteLength_ = Encoding.ASCII.GetBytes(frameByte_.Length.ToString());
            Thread.Sleep(1);
            stream.Write(frameByteLength_, 0, frameByteLength_.Length);
            // now send frameByte
            Thread.Sleep(1);
            stream.Write(frameByte_, 0, frameByte_.Length);
            //Debug.Log("Sent new frame");

            sendNewFrame = false;
        }

        if (sendReward == true)
        {
            sendRewardbyte = Encoding.ASCII.GetBytes(Math.Round(movement.reward, 4).ToString());
            Thread.Sleep(1);
            stream.Write(sendRewardbyte, 0, sendRewardbyte.Length);
            //Debug.Log("Sent reward");

            sendReward = false;
        }
    }
}
