using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomVehicleV2
{
	public class Axis : MonoBehaviour
	{
		[Range(-90,90)]
		public float wheelAngleIn=0,wheelAngleOut=0;
		[Range(0,1)]
		public float effective=0;
		public Wheel[] wheels;
		public float angleZOffset;
		
		[SerializeField]
		float topGear=3.9f;
		
		float angleN=0,_break,_handBreak,spdKMH;
		
		public void SetAngle(float normal){
			angleN = normal;
		}
		public void SetBreaks(float _break,float _handBreak){
			SetBreak(_break);
			SetHandBreak(_handBreak);
		}
		public void SetBreak(float _break){
			this._break=_break;
		}
		public void SetHandBreak(float _handBreak){
			this._handBreak=_handBreak;
		}
		public float GetSpeed(){
			return spdKMH;
		}
		public float SetRPMTorque(float engineRPM,GearBox gearbox,float engineEnertia){
			
			float callBack=0;
			float gear = topGear*gearbox.GetGear();
			engineRPM /= gear;
			gear = Mathf.Abs(gear);
			foreach (var item in wheels)
			{
				callBack+=item.SetRPMTorque(engineRPM,gear,effective,gearbox.GetClutch(),engineEnertia);
			}
			return callBack/wheels.Length;
		}
		float callBack=0;
		public float SetRPMTorque_(float engineRPM,GearBox gearbox,float engineEnertia){
			
			callBack=0;
			float gear = topGear*gearbox.GetGear();
			engineRPM /= gear;
			//Debug.Log(engineRPM);
			gear = Mathf.Abs(gear);
			float clutch = gearbox.GetClutch();
			float baseTorque;
			foreach (var item in wheels)
			{
				baseTorque = (engineRPM - item.rpm) * clutch / (item.inertia + engineEnertia);
				Debug.Log(baseTorque);
				item.driveTorque = baseTorque * engineEnertia * gear * effective;
				callBack+=baseTorque * item.inertia * effective;
			}
			return callBack/wheels.Length;
		}
		
		
		// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
		protected void FixedUpdate()
		{
			spdKMH=0;
			int lr=0;
			float angleT=0;
			float angled = wheelAngleIn-wheelAngleOut;
			if (wheels==null || wheels.Length==0)
				return;
			foreach (var item in wheels)
			{
				angleT = (wheelAngleIn-angled*Mathf.Abs(Mathf.Sign(angleN)-item.side)/2f)*angleN;
				item.transform.localRotation = Quaternion.Euler(0,angleT,angleZOffset*item.side);
				item.brake = _break;
				item.handbrake = _handBreak;
				spdKMH+=item.spdKMH;
				lr++;
			}
			spdKMH/=wheels.Length;
		}
		// This function is called when the script is loaded or a value is changed in the inspector (Called in the editor only).
		protected void OnValidate()
		{
			FixedUpdate();
		}
	}
}
