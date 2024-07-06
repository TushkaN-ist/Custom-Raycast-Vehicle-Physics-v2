using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ControllerVehicle : MonoBehaviour,IController {
	
	public abstract float GetEngineRPM();
	public abstract float GetSpeedKMH();
	public abstract int GetGear();
	public abstract Transform GetCOM();
}

public interface IController
{
	float GetEngineRPM();
	float GetSpeedKMH();
	int GetGear();
}
