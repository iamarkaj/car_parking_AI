using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

public class movement1 : MonoBehaviour
{
    public Transform car1;
    float forward = 0;
    float left = 0;
    bool gameEnd = false;
    string key;
    public static double reward = 0.0;

    Vector3[] defaultPos;
    Vector3[] defaultScale;
    Quaternion[] defaultRot;

    Transform[] models;

    Vector3 car1_posi;
    Quaternion car1_rot;

    //void Awake()
    //{
    //    QualitySettings.vSyncCount = 0;  // VSync must be disabled
    //   Application.targetFrameRate = 20;
    //}

    void Start()
    {
        backUpTransform();
        car1_posi = car1.position;
        car1_rot = car1.rotation;
    }

    void backUpTransform()
    {
        GameObject[] tempModels = GameObject.FindGameObjectsWithTag("Props1");
        defaultPos = new Vector3[tempModels.Length];
        defaultScale = new Vector3[tempModels.Length];
        defaultRot = new Quaternion[tempModels.Length];
        models = new Transform[tempModels.Length];
        for (int i = 0; i < tempModels.Length; i++)
        {
            models[i] = tempModels[i].GetComponent<Transform>();
            defaultPos[i] = models[i].position;
            defaultScale[i] = models[i].localScale;
            defaultRot[i] = models[i].rotation;
        }
    }

    void resetTransform()
    {
        for (int i = 0; i < models.Length; i++)
        {
            models[i].position = defaultPos[i];
            models[i].localScale = defaultScale[i];
            models[i].rotation = defaultRot[i];
        }
    }

    void Update()
    {

        if (gameEnd == false)
        {
            reward -= 0.0001;

            String a = "1230";
            int rr= UnityEngine.Random.Range(0,4);
            key = a[rr].ToString();

            //key = GameObject.Find("Camera (1)").GetComponent<camera1>().action;

            //forward
            if (key == "0")
            {
                forward = 5;
                left = 0;
            }
            //backward
            if (key == "1")
            {
                forward = -2;
                left = 0;
            }
            //left
            if (key == "2")
                left = -10;
            //right
            if (key == "3")
                left = 10;
            //brake
            if (key == "STOP")
                forward = 0;


            transform.Translate(0, 0, forward * Time.deltaTime);
            transform.Rotate(0, left * forward * Time.deltaTime, 0);

            // maximum negative rewards
            if (reward < -20.0)
            {
                reward = -20.0;
            }

            // boundary conditions
            if (car1.position.x < -11.54 || car1.position.x > 5.95 || car1.position.z < 12.34 || car1.position.z > 37.52)
                car1.position = car1_posi;
                car1.rotation = car1_rot;

            // intermediate reward
            if (car1.position.x < -2.0 && car1.position.x > -3.0 && car1.position.z > 21.5 && car1.position.z < 22.5)
            {
                reward = 10.0;
            }

            // won game
            if (car1.position.x < -5.82 && car1.position.x > -7.39 && car1.position.z > 26.14 && car1.position.z < 27.25)
            {
                reward = 20.0;
                gameEnd = true;
            }
        }
        else
        {
            reward = 0.0;
            car1.position = car1_posi;
            car1.rotation = car1_rot;
            for (int i = 0; i < models.Length; i++)
            {
                models[i].position = defaultPos[i];
                models[i].localScale = defaultScale[i];
                models[i].rotation = defaultRot[i];
            }
            gameEnd = false;
        }

    }

    // lose game
    void OnCollisionEnter(Collision collision_info)
    {
        if (collision_info.collider.name != "Base")
        {
            reward -= 2.0;
        }
    }
}
