using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomVehicleV2
{
	public class CarController : ControllerVehicle
	{
		public bool actived=false;
		public Rigidbody body;
		public Transform COM;
		public Engine engine;
		public GearBox gearBox;
		public Axis[] axis;
		public float _break,_handBreak;
		public Vector3 localForce;
	    // Start is called before the first frame update
	    void Start()
		{
			if (COM && COM.gameObject.active)
			    body.centerOfMass = COM.localPosition;
	    }
		public float spdKMH;
		[Range(-1,1)]
		public float wheel=0;
		//public float angleCorrect=0;
	    // Update is called once per frame
		void FixedUpdate()
		{
			//return;
			//WheelGearRPM = (engine.rpm/gearBox.GetGearOut());
			//float gearOut = Mathf.Abs(gearBox.GetGearOut());
			float backTorque=0;
			float _spdKMH=0;
			if (axis != null && axis.Length > 0)
			{
				foreach (var item in axis)
				{
					item.SetAngle(wheel);
					backTorque += item.SetRPMTorque(engine.rpm, gearBox, engine.inertia);
					item.SetBreaks(_break, _handBreak);
					_spdKMH += item.GetSpeed();
				}
				spdKMH = Mathf.Lerp(spdKMH, _spdKMH / axis.Length, Time.fixedDeltaTime / .25f);
			}
			engine.wheelTorque = backTorque * gearBox.GetSide();
			localForce = transform.InverseTransformDirection(body.velocity)*60*60/1000f;
			//angleCorrect = (Mathf.Atan(localForce.x)*Mathf.Rad2Deg/60f)-wheel;
		}
		
		int gear;
		public float powWheel=2;
		// Update is called every frame, if the MonoBehaviour is enabled.
		protected void Update()
		{
			if (!actived)
				return;
			engine.starter = Input.GetKey(KeyCode.E);
			float tb = Input.GetAxis("Vertical");
			engine.throttle = Mathf.Clamp01(tb);
			float _wheel = Input.GetAxis("Horizontal");
			wheel = Mathf.Pow(Mathf.Abs(_wheel),powWheel)*Mathf.Sign(_wheel);
			_break = Mathf.Clamp01(-tb);
			_handBreak=Input.GetAxis("Jump");
			if (Input.GetKey(KeyCode.Joystick1Button0)){
				_handBreak=1;
			}
			if (Input.GetKeyDown(KeyCode.Joystick1Button4)){
				gear=Mathf.Clamp(gear-1,gearBox.maxRearGear,gearBox.maxFrontGear);
				gearBox.SetGear(gear);
			}
			if (Input.GetKeyDown(KeyCode.Joystick1Button5)){
				gear=Mathf.Clamp(gear+1,gearBox.maxRearGear,gearBox.maxFrontGear);
				gearBox.SetGear(gear);
			}
			
			if (Input.GetKeyDown(KeyCode.Alpha0)){
				gearBox.SetGear(gear=0);
			}
			if (Input.GetKeyDown(KeyCode.Alpha1)){
				gearBox.SetGear(gear=1);
			}
			if (Input.GetKeyDown(KeyCode.Alpha2)){
				gearBox.SetGear(gear=2);
			}
			if (Input.GetKeyDown(KeyCode.Alpha3)){
				gearBox.SetGear(gear=3);
			}
			if (Input.GetKeyDown(KeyCode.Alpha4)){
				gearBox.SetGear(gear=4);
			}
			if (Input.GetKeyDown(KeyCode.Alpha5)){
				gearBox.SetGear(gear=5);
			}
			if (Input.GetKeyDown(KeyCode.Alpha6)){
				gearBox.SetGear(gear=-1);
			}
		}
		public override float GetEngineRPM()
		{
			return engine.rpm;
		}
		public override int GetGear()
		{
			return gearBox.gear;
		}
		public override float GetSpeedKMH()
		{
			return spdKMH;
		}
		public override Transform GetCOM()
		{
			return COM;
		}
	}
	
}
