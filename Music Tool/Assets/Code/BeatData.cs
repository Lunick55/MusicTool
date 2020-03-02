using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScoobyDoo")]
public class BeatData : ScriptableObject
{
	public float[] time;
	public AudioClip song;
}
