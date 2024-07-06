using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomVehicleV2
{
	public class GearBox : MonoBehaviour
	{
		[Range(0,1)]
		public float clutch=0;
		
		[SerializeField]
		int gearFirst=1;
		[SerializeField]
		float[] gearRatio=new float[]{-3.53f,1,3.67f,2.1f,1.36f,1f,0.82f};
		
		public int gear;
		
		public void SetGear(int id){
			gear=id;
		}
		public bool isNeitrale{
			get{
				return (gear+gearFirst)==gearFirst;
			}
		}
		public int GetNeitralGear(){
			return gearFirst;
		}
		public int maxFrontGear{
			get{
				return gearRatio.Length-1-gearFirst;
			}
		}
		public int maxRearGear{
			get{
				return -gearFirst;
			}
		}
		public float GetGear(){
			return isNeitrale?1:gearRatio[Mathf.Clamp(gear+gearFirst,0,gearRatio.Length-1)];
		}
		public float GetClutch(){
			return (isNeitrale)?0:(1f-clutch);
		}
		public float GetSide(){
			return Mathf.Sign(GetGear());
		}
	}
}
