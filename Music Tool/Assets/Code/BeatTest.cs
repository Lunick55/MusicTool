using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatTest : MonoBehaviour
{
    public BeatData beat;
    float timer = 0;
    int index = 0;

    public AudioSource aud;

    // Start is called before the first frame update
    void Start()
    {
        timer = 0;
        aud.clip = beat.song;
        aud.Play();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (index < beat.time.Length)
        {
            if (timer >= beat.time[index])
            {
                Debug.Log("BEAT");
                index++;
            }
        }
    }
}
