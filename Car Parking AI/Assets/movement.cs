using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

public class movement : MonoBehaviour
{
    public Transform car;
    float forward = 0;
    float left = 0;
    bool gameEnd = false;
    string key;
    public static double reward = 0.0;
    Vector3[] defaultPos;
    Vector3[] defaultScale;
    Quaternion[] defaultRot;
    Transform[] models;
    Vector3 car_posi;
    Quaternion car_rot;

    //void Awake()
    //{
    //    QualitySettings.vSyncCount = 0;  // VSync must be disabled
    //   Application.targetFrameRate = 20;
    //}

    void Start()
    {
        GameObject[] tempModels = GameObject.FindGameObjectsWithTag("Props");
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
        car_posi = car.position;
        car_rot = car.rotation;
    }

    void Update()
    {

        if (gameEnd == false)
        {
            reward -= 0.0001;

            // ########### Manual Controls ###########
            /*if (Input.GetKey(KeyCode.W))
            {
                forward = 5;
                left = 0;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                forward = -2;
                left = 0;
            }

            if (Input.GetKey(KeyCode.A))
                left = -10;
            else if (Input.GetKey(KeyCode.D))
                left = 10;

            if (Input.GetKey(KeyCode.P))
                forward = 0;*/
            
            // #################################

            // ########### Auto Controls ###########
            String a = "2310";
            int rr= UnityEngine.Random.Range(0,4);
            key = a[rr].ToString();

            key = GameObject.Find("Camera").GetComponent<camera>().action;
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

            // #################################

            transform.Translate(0, 0, forward * Time.deltaTime);
            transform.Rotate(0, left * forward * Time.deltaTime, 0);

            // maximum negative rewards
            if (reward < -20.0)
            {
                reward = -20.0;
            }
            // boundary conditions
            if (car.position.x < -11.54 || car.position.x > 5.95 || car.position.z < 12.34 || car.position.z > 37.52)
            {
                car.position = car_posi;
                car.rotation = car_rot;
            }

            // intermediate reward
            if (car.position.x < -2.0 && car.position.x > -3.0 && car.position.z > 21.5 && car.position.z < 22.5)
            {
                reward = 10.0;
            }

            // won game
            if (car.position.x < -5.82 && car.position.x > -7.39 && car.position.z > 26.14 && car.position.z < 27.25)
            {
                reward = 20.0;
                gameEnd = true;
            }
        }
        else
        {
            reward = 0.0;
            car.position = car_posi;
            car.rotation = car_rot;
            for (int i = 0; i < models.Length; i++)
            {
                models[i].position = defaultPos[i];
                models[i].localScale = defaultScale[i];
                models[i].rotation = defaultRot[i];
            }
            gameEnd = false;
        }
    }

    void OnCollisionEnter(Collision collision_info)
    {
        if (collision_info.collider.name != "Base")
        {
            reward -= 2.0;
        }
    }
}
