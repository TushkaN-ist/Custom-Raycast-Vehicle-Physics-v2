using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TireSFX : MonoBehaviour
{
	public CustomVehicleV2.Wheel wheel;
	public AudioClip[] skidLvl;
	public Vector2[] skidPitch;
	public AnimationCurve skidMarge;
	public float minSkidForce=0;
	public float maxSkidForce=1;
	public float smooth=.5f;
	public float pow=1.25f;
	Vector3 vel;
	AudioSource[] sources;
    // Start is called before the first frame update
    void Start()
    {
	    sources=new AudioSource[skidLvl.Length];
	    AudioSource src;
	    for (int i = 0; i < sources.Length; i++) {
	    	src = gameObject.AddComponent<AudioSource>();
	    	src.loop=true;
	    	src.playOnAwake=false;
	    	src.clip=skidLvl[i];
	    	src.spatialBlend=1;
	    	//src.Play();
	    	sources[i] = src;
	    }
    }
    // Update is called once per frame
    void Update()
	{
		vel = Vector3.Lerp(vel,wheel.wheelLocalVelocity,Time.deltaTime/smooth);
		vel.y = 0;
		int itemID = 1;
		float v;
		Vector2 pitch;
		foreach (var item in sources)
		{
			v=Mathf.Pow(Mathf.Clamp01(1f-Mathf.Abs(skidMarge.Evaluate(Mathf.Max(0,vel.magnitude-minSkidForce)/maxSkidForce*sources.Length)-itemID)),pow);
			item.volume = v;
			pitch = skidPitch[itemID-1];
			item.pitch = Mathf.Lerp(pitch.x,pitch.y,v);
			if (item.volume <= 0.1f)
			{
				if (item.isPlaying)
					item.Stop();
			}else{
				
				if (!item.isPlaying)
					item.Play();
			}
			itemID++;
		}
    }
}
