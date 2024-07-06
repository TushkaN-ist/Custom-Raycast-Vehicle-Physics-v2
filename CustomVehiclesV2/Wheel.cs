using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CustomVehicleV2
{
	public class Wheel : MonoBehaviour
	{
		
		[SerializePrivateVariables]
		Rigidbody rigidbody;
		bool isGround;
		[SerializePrivateVariables]
		Raycasting raycasting=new CustomVehicleV2.Raycasting();
		
		[Header("Graphic")]
		public Transform graphic;
		
		[Header("Parametres")]
		public float mass = 15f;
		public float width = 0.2f;
		public float radius = 0.32f;
		[Range(-1,1)]
		public int side=0;
		public float offsetX =0;
		public Vector2 center;
		[Range(0,1)]
		public float round = 0.05f;
		[Range(4,255)]
		public int detail=5;
		public int detailWidth=1;
		[Header("Suspension")]
		public Transform pointSuspension=null;
		public Suspension suspension=new Suspension();
		
		static PhysicMaterial defaultPhyMat;
		
		[Header("Friction and Forces")]
		public PhysicMaterial physicMaterial;
		public float angularVelocity,rpm,spdKMH;
		public float inertia = 2.2f;
		
		public WheelFrictionCurveSource forwardFriction=new WheelFrictionCurveSource(){ExtremumSlip=0.4f,ExtremumValue=1,AsymptoteSlip=0.8f,AsymptoteValue=0.5f,Stiffness=3};
		public WheelFrictionCurveSource sideFriction=new WheelFrictionCurveSource(){ExtremumSlip=0.2f,ExtremumValue=1,AsymptoteSlip=0.5f,AsymptoteValue=0.75f,Stiffness=1.4f};
		
		float rotation;
		
		[Header("Inputs")]
		// engine torque applied to this wheel
		public float driveTorque = 0;
		// engine braking and other drivetrain friction torques applied to this wheel
		public float driveFrictionTorque = 0;
		// brake input
		public float brake = 0;
		// handbrake input
		public float handbrake = 0;
		// drivetrain inertia as currently connected to this wheel
		public float drivetrainInertia = 0;
		// suspension force externally applied (by anti-roll bars)
		public float suspensionForceInput = 0;
		
		// Maximal braking torque (in Nm)
		public float brakeFrictionTorque = 400;
		// Maximal handbrake torque (in Nm)
		public float handbrakeFrictionTorque = 600;
		// Base friction torque (in Nm)
		public float frictionTorque = 10;
		
		RaycastHit hit;
		public Vector3 wheelLocalVelocity;
		
		float _offsetX;
		
		// This function is called when the script is loaded or a value is changed in the inspector (Called in the editor only).
		protected void OnValidate()
		{
			if (pointSuspension==null)
				pointSuspension=transform;
			mass = Mathf.Max(mass,0);
			inertia = (mass*radius*radius);
			_offsetX=((center.x*width*.5f+offsetX)*side);
			rigidbody = GetComponentInParent<Rigidbody>();
			suspension.UpdateSetting();
			raycasting.InitUpdate(transform,mass,radius,width,offsetX*side,round,detail,detailWidth);
		}
		#if UNITY_EDITOR
		// Implement OnDrawGizmos if you want to draw gizmos that are also pickable and always drawn.
		protected void OnDrawGizmos()
		{
			float minRadius = Mathf.Min(radius,width);
			Gizmos.color = Color.magenta;
			Gizmos.DrawWireSphere(transform.position-transform.up*(suspension.springLength+radius*center.y)+transform.right*_offsetX,minRadius/2f);
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(transform.position-transform.up*suspension.restLength,minRadius/4f);
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(transform.position-transform.up*suspension.springLength,minRadius/5f);
			/*Vector3 dir = -transform.up*suspension.minLength;
			Gizmos.DrawRay(transform.position,dir);
			Gizmos.color = Color.yellow;
			Gizmos.DrawRay(transform.position+dir,-transform.up*(suspension.springLength-suspension.minLength));
			*/
			Gizmos.color = Color.yellow;
			Vector3 posPrev = transform.position;
			Vector3 posNext = -transform.up*suspension.minLength;
			Gizmos.DrawRay(posPrev,posNext);
			
			Gizmos.color = Color.green;
			posPrev += posNext;
			posNext = -transform.up*suspension.springTravel;
			Gizmos.DrawRay(posPrev,posNext);
			if (isGround){
				Vector3 pos = hit.point;
				Gizmos.color = Color.green;
				Gizmos.DrawRay(pos,transform.up * wheelLocalVelocity.y);
				Gizmos.color = Color.blue;
				Gizmos.DrawRay(pos,-transform.forward * wheelLocalVelocity.z);
				Gizmos.color = Color.red;
				Gizmos.DrawRay(pos,-transform.right * wheelLocalVelocity.x);
			}
			raycasting.DrawWireMesh(suspension.springLength);
			if ((!UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isPaused) && graphic!=null)
				GetGraphic(graphic);
		}
		#endif
		
		public void GetGraphic(Transform wheelObj){
			Vector3 pos;Quaternion rot;
			raycasting.GetPosRot(out pos,out rot,suspension.springLength,rotation);
			wheelObj.position = pos;wheelObj.rotation = rot;
		}
		
		// Start is called before the first frame update
		void Start()
		{
			if (defaultPhyMat==null)
				defaultPhyMat=new PhysicMaterial();
			OnValidate();
			suspension.ResetForce();
			if (physicMaterial==null)
				physicMaterial=defaultPhyMat;
			foreach(Collider item in Physics.OverlapSphere(transform.position,radius+ Mathf.Abs(_offsetX))){
				if (item.attachedRigidbody==rigidbody){
					raycasting.IgnoreCollision(item);
				}
			}
		}
	    
		// Update is called every frame, if the MonoBehaviour is enabled.
		protected void LateUpdate()
		{
			rotation = (rotation+(angularVelocity*Time.deltaTime*Mathf.Rad2Deg))%360f;
			if (graphic!=null)
				GetGraphic(graphic);
		}
		
		public float SetRPMTorque(float targetRPM,float gear,float effective,float clutch,float engineEnertia){
			float baseTorque = (targetRPM - rpm) * clutch / (inertia + engineEnertia);
			driveTorque = baseTorque * engineEnertia * gear * effective;
			//Debug.Log(transform.name+":"+Mathf.Clamp(wheelLocalVelocity.y/damperStiggness,0,10f));
			return baseTorque * inertia * effective;
		}
		
		Vector3 slips;
		Vector2 slipDyn=new Vector2();
		public Vector3 gripForce=Vector3.one;
		public float slipAnglePeek=30f;
		public float slipAngularPeek=10f;
		public float grip = 1;
		public Vector2 relaxationLength=new Vector2(0.02f,0.0125f);
		
		WheelHit GetWheelHit(Quaternion rotation,RaycastHit rhit,float suspensionForce){
			WheelHit hit=new WheelHit();
			hit.collider = rhit.collider;
			hit.normal = rhit.normal;
			hit.point = rhit.point;
			hit.force = suspensionForce;
			Vector3 vectorF = rotation*Vector3.forward;
			Vector3 vectorU = rotation*Vector3.up;
			float num = -Vector3.Dot(vectorF, hit.normal);
			float num2 = Vector3.Dot(vectorU, hit.normal);
			if (num2 > 1E-06f)
			{
				Vector3 b = vectorU * (num / num2);
				hit.forwardDir = (vectorF + b).normalized;
			}
			else
			{
				hit.forwardDir = ((num >= 0f) ? vectorU : (-vectorU));
			}
			hit.sidewaysDir = Vector3.Cross(hit.normal, hit.forwardDir);
			return hit;
		}
		
		
		void FixedUpdate(){
			
			PhysicsV1();
		}
		
		// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
		protected void PhysicsV1()
		{
			float totalInertia = inertia + drivetrainInertia;
			float driveAngularDelta = driveTorque * Time.fixedDeltaTime / totalInertia;
			float totalFrictionTorque = brakeFrictionTorque * brake + handbrakeFrictionTorque * handbrake + frictionTorque + driveFrictionTorque;
			float frictionAngularDelta = totalFrictionTorque * Time.fixedDeltaTime / totalInertia;
			
			float dist = suspension.springTargetLength;
			if (isGround = raycasting.Raycast(Vector3.down,out hit,dist)){
				dist = hit.distance;
			}
			suspension.Update(isGround,mass,rigidbody.mass,Time.fixedDeltaTime,dist,Vector3.Dot(-transform.up,Physics.gravity),out suspensionForceInput);
			//suspensionForceInput = Mathf.Max(0,suspensionForceInput);
			suspensionForceInput*=gripForce.y;
			Vector3 suspensionForce = transform.up*suspensionForceInput;
			rigidbody.AddForceAtPosition(suspensionForce,pointSuspension.position);
			float delta=0;
			if (isGround){
				Vector3 pos = transform.position-transform.up*(suspension.springLength+radius*center.y)+transform.right*_offsetX;
				
				Vector3 wheelVelo=rigidbody.GetPointVelocity(hit.point);
				if (hit.rigidbody!=null){
					wheelVelo-=hit.rigidbody.GetPointVelocity(hit.point);
				}
				wheelLocalVelocity = transform.InverseTransformDirection(wheelVelo);
				float angularDelta = wheelLocalVelocity.z-angularVelocity*radius;
				float friction = GetFriction();
				wheelLocalVelocity.y = suspensionForceInput/suspension.springStiggness;
				wheelLocalVelocity.z = GetZForce(angularDelta,friction)*wheelLocalVelocity.y;
				wheelLocalVelocity.x = GetXForce(angularDelta,friction)*wheelLocalVelocity.y;
				
				delta = (slips.z/totalInertia/radius);
				delta-=Mathf.Clamp(delta,-frictionAngularDelta,frictionAngularDelta);
				
				wheelVelo=GetSimpleTireForce(slips.x,slips.z);
				//wheelLocalVelocity.z = -angularDelta;
				//wheelLocalVelocity*=5f;
				wheelVelo = ProjectionVector(wheelVelo);
				rigidbody.AddForceAtPosition(wheelVelo,pos);
				if (hit.rigidbody){
					float d = Mathf.Clamp01(hit.rigidbody.mass / (rigidbody.mass+mass));
					hit.rigidbody.AddForceAtPosition(-wheelVelo-suspensionForce*d,hit.point);
				}
			}else{
				wheelLocalVelocity = Vector3.zero;
			}
			angularVelocity+=driveAngularDelta-delta;
			//delta = Mathf.Clamp01(Mathf.Abs(angularVelocity)/frictionAngularDelta);
			//frictionAngularDelta/=delta;
			angularVelocity-=Mathf.Clamp(angularVelocity,-frictionAngularDelta,frictionAngularDelta);
			
			rpm = ((angularVelocity*Time.fixedDeltaTime * Mathf.Rad2Deg)/360f)*(60*60);
			spdKMH = rpm*radius*2*100*0.001885f;
		}
		
		float ClampRange(float t,float a,float b){
			float c = b-a;
			return Mathf.Clamp01((t-a)/c);
		}
		
		float GetXForce(float angularDelta,float friction){
			
			Vector3 force = new Vector3(-wheelLocalVelocity.x,0,angularDelta);
			
			float forward = force.z;
			float coeff = Mathf.Clamp01(Mathf.Abs(force.x)/relaxationLength.x*Time.fixedDeltaTime);
			float slip = forward!=0?Mathf.Atan((force.x)/Mathf.Abs(forward))*Mathf.Rad2Deg:0;
			slip = Mathf.Lerp(slipAnglePeek*Mathf.Sign(force.x),slip,ClampRange(force.magnitude,3,6));
			slipDyn.x+=(slip-slipDyn.x)*coeff;
			float result = (slipDyn.x/slipAnglePeek)*friction;
			float max = sideFriction.Evaluate(result);
			slips.x = Mathf.Clamp(result,-max,max)*gripForce.x;
			return (slips.x/gripForce.x-result)/max;
		}
		
		float GetZForce(float angularDelta,float friction){
			Vector3 force = new Vector3(-wheelLocalVelocity.x,0,-angularDelta);
			float side = force.x;
			float coeff = Mathf.Clamp01(Mathf.Abs(force.z)/relaxationLength.y*Time.fixedDeltaTime);
			float slipAngle = side!=0?Mathf.Atan((force.z)/Mathf.Abs(side))*Mathf.Rad2Deg:0;
			slipAngle = Mathf.Lerp(slipAngularPeek*Mathf.Sign(force.z),slipAngle,ClampRange(force.magnitude,3,6));
			slipDyn.y += (slipAngle-slipDyn.y)*coeff;
			float result = (slipDyn.y/slipAngularPeek)*friction;
			float max = forwardFriction.Evaluate(result);
			slips.z = Mathf.Clamp(result,-max,max)*gripForce.z;
			return (slips.z/gripForce.z-result)/max;
		}
		
		Vector3 GetSimpleTireForce(float forceX,float forceZ){
			Vector3 force=new Vector3(forceX,0,forceZ);
			//Vector3 rightNormalPlane = Vector3.ProjectOnPlane(transform.right,hit.normal).normalized;
			//Vector3 forwardNormalPlane = Vector3.ProjectOnPlane(transform.forward,hit.normal).normalized;
			//Vector3 force = forwardNormalPlane+rightNormalPlane;
			float suspensionForceGrip = suspensionForceInput;
			force = grip*Vector3.ClampMagnitude(force*suspensionForceGrip,suspensionForceGrip);
			force.y = suspensionForceGrip;
			return force;
		}
		
		Vector3 ProjectionVector(Vector3 force){
			Vector3 rightNormalPlane = Vector3.ProjectOnPlane(transform.right,hit.normal).normalized*force.x;
			Vector3 forwardNormalPlane = Vector3.ProjectOnPlane(transform.forward,hit.normal).normalized*force.z;
			return rightNormalPlane+forwardNormalPlane;
		}
		
		//friction materials
		float GetFriction(){
			PhysicMaterial hitMat=hit.collider.sharedMaterial?hit.collider.sharedMaterial:defaultPhyMat;
			if (hit.transform.gameObject.isStatic)
				return physicMaterial.GetFactorDynamicStatic(hitMat);
			else
				return physicMaterial.GetFactorDynamic(hitMat);
		}
	}
}
