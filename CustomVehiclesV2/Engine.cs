using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomVehicleV2
{
	public class Engine : MonoBehaviour
	{
		public AnimationCurve torqueCurve,throttleAuto;
		
		[SerializeField]
		public float rpm=0;
		[SerializeField]
		uint rpmCross=4150;
		public float torqueMax=126,backTorque=2300,wheelTorque=0;
		
		public float inertia = 0.216f;
		[Range(0,1)]
		public float throttle=0,throttleMultiply=1f;
		[Range(0.04f,1)]
		public float throttleDelta=0.2f;
		public float starterPower=60;
		public bool starter=false;
		float _throttle;
		
		
		public float startFriction=50,angularVelocity;
		
		#if UNITY_EDITOR
		[Header("Editor"),SerializeField]
		AnimationCurve powerCurve;
		// This function is called when the script is loaded or a value is changed in the inspector (Called in the editor only).
		protected void OnValidate()
		{
			if (torqueCurve==null || torqueCurve.length==0)
				return;
			powerCurve=new AnimationCurve();
			float f=0;
			float t = torqueCurve.keys[torqueCurve.length-1].time;
			while(f<t){
				
				Keyframe kf=new Keyframe();
				kf.time = f;
				kf.inWeight=0;
				kf.outWeight=0;
				kf.tangentMode = 0;
				kf.weightedMode = WeightedMode.Both;
				kf.value=GetPower(f*rpmCross)/torqueMax;
				powerCurve.AddKey(kf);
				f+=0.05f;
			}
			rpmToRad=Mathf.PI*2f/60f;
			radToRPM=1f/rpmToRad;
		}
		#endif
		
		float GetTorqueRPM(){
			return torqueCurve.Evaluate(rpm/rpmCross)*torqueMax;
		}
		
		float GetTorqueRPM(float gear){
			return torqueCurve.Evaluate(rpm/rpmCross/Mathf.Abs(gear))*torqueMax;
		}
		
		float GetPower(float RPM){
			return (torqueCurve.Evaluate(RPM/rpmCross)*torqueMax)*RPM/rpmCross;
		}
		
		float GetTorque(float throttle){
			return (torqueCurve.Evaluate(rpm/rpmCross)*torqueMax)*throttle;
		}
		float GetAutoThrottle(float throttle){
			float autoThr = throttleAuto.Evaluate(rpm/rpmCross);
			float autoThr2 = 1f-Mathf.Abs(autoThr);
			return Mathf.Clamp(throttle*autoThr2+autoThr,-1f,1f);
		}
		
		float GetCurrentFriction(){
			return startFriction+rpm/rpmCross*backTorque;
		}
		
		float GetCurrentMaxTorque(float dtime){
			
			float Throttle = GetAutoThrottle(throttle);
			_throttle = Mathf.Lerp(_throttle,Throttle,dtime/throttleDelta);
			float torq = GetTorque(_throttle)/inertia;
			
			float friction = GetCurrentFriction();
			return (GetTorqueRPM()+friction)*_throttle-(friction+wheelTorque);
		}
		
		float ProgressRPM(float Throttle,float dtime,float wheelTorque)
		{
			Throttle = GetAutoThrottle(Throttle);
			_throttle = Mathf.Lerp(_throttle,Throttle,dtime/throttleDelta);
			float torq = GetTorque(_throttle)-wheelTorque;
			return (torq - Mathf.Pow(1.0f - (_throttle), 2) * (backTorque*dtime));
		}
		
		// Update is called every frame, if the MonoBehaviour is enabled.
		public void UpdateRPM(float dtime)
		{
			if (starter && throttleAuto.Evaluate(rpm/rpmCross)>0)
			{
				rpm+=starterPower;
			}
			rpm=Mathf.Clamp(rpm+ProgressRPM(throttle*throttleMultiply,dtime,wheelTorque),0,rpmCross*3);
		}
		public float rpmToRad;
		public float radToRPM;
		public float trq;
		// Update is called every frame, if the MonoBehaviour is enabled.
		public void UpdateRPM_(float dtime)
		{
			if (starter)
			{
				angularVelocity+=starterPower*rpmToRad;
			}
			trq = GetCurrentMaxTorque(dtime);
			float torquDelta = trq/inertia*dtime;
			angularVelocity = Mathf.Max(angularVelocity+torquDelta,0);//Mathf.Clamp(angularVelocity+torquDelta,50*rpmToRad,5400*rpmToRad);
			rpm = angularVelocity*radToRPM;
		}
		// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
		protected void FixedUpdate()
		{
			UpdateRPM(Time.fixedDeltaTime);
		}
	}
}
