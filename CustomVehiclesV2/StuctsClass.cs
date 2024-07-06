using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomVehicleV2
{
	[System.Serializable]
	public class Raycasting{
		
		[SerializePrivateVariables]
		MeshCollider collider;
		[SerializePrivateVariables]
		Rigidbody body;
		[SerializePrivateVariables]
		Transform transform;
		Mesh wheelMesh;
		
		public void InitUpdate(Transform transform,float mass,float radius,float width,float offsetX=0,float round=0.05f,int details=6,int detailsWidth=1){
			wheelMesh=Extension.GenerateWheel(radius,width,round,details,detailsWidth,true);
			if (body==null || this.transform!=transform){
				body=new GameObject("body").AddComponent<Rigidbody>();
				body.transform.SetParent(transform,false);
				body.gameObject.hideFlags = HideFlags.HideAndDontSave;
				body.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
				body.detectCollisions=false;
				body.isKinematic=true;
				collider = body.gameObject.AddComponent<MeshCollider>();
				collider.enabled=true;
				collider.convex=true;
				collider.isTrigger=true;
				collider.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
				this.transform = transform;
			}
			body.gameObject.layer = transform.gameObject.layer;
			body.transform.localPosition=new Vector3(offsetX,0,0);
			body.mass = mass;
			if (collider!=null && collider.sharedMesh!=null && collider.sharedMesh!=wheelMesh)
				Extension.FreeMesh(collider.sharedMesh);
			collider.sharedMesh = wheelMesh;
		}
		
		public void Destroy(){
			Extension.FreeMesh(collider.sharedMesh);
			MonoBehaviour.DestroyImmediate(body.gameObject);
		}
		public bool Raycast(Vector3 localDirection,out RaycastHit hit,float distance){
			return body.SweepTest(body.rotation * localDirection,out hit,distance);
		}
		public bool RaycastGlobal(Vector3 globalDirection,out RaycastHit hit,float distance){
			return body.SweepTest(globalDirection,out hit,distance);
		}
		public RaycastHit[] RaycastAll(Vector3 localDirection,float distance){
			//localDirection = localDirection * body.rotation;
			return body.SweepTestAll(body.rotation * localDirection,distance);
		}
		#if UNITY_EDITOR
		public void DrawWireMesh(float distance){
			Color restore = Gizmos.color;
			Gizmos.color = Color.green;
			if (collider!=null && collider.sharedMesh!=null)
				Gizmos.DrawWireMesh(wheelMesh,body.transform.position-transform.up*distance,transform.rotation,transform.lossyScale);
			Gizmos.color = restore;
		}
		#endif
		
		public void GetPosRot(out Vector3 pos,out Quaternion rot,float dist,float rotation){
			pos = body.transform.position-body.transform.up*dist;
			rot = body.transform.rotation*Quaternion.Euler(rotation,0,0);
		}

		public void IgnoreCollision(Collider collider){
			Physics.IgnoreCollision(collider, this.collider);
		}
		
	}
	/// <summary>
	/// В будущем повторить пружину реалестичнее
	/// https://habr.com/ru/post/497456/
	/// </summary>
	[System.Serializable]
	public class Suspension
	{
		public float restLength=0.5f;
		public float springTravel=.5f;
		public float springStiggness=15000;
		public float damperStiggness=1500;
	
		public float springLength{get;private set;}
		public float springTargetLength;
		public float springForce;
	
		[SerializeField,Range(0,0.99f)]
		private float _springTravelOffset=.5f;
		public float minLength{get;private set;}
		public float maxLength{get;private set;}
		
		//public float valueThreshold = 0.01f;
		//public float velocityThreshold = 0.01f;
		
		public void UpdateSetting(){
			if (restLength<=0)
				restLength=0;
			springTravel = Mathf.Clamp(springTravel,0.01f,restLength);
			minLength = restLength - springTravel*(1f-springTravelOffset);
			maxLength = restLength + springTravel*(springTravelOffset);
			#if UNITY_EDITOR
			if (!UnityEditor.EditorApplication.isPlaying)
				springLength=maxLength;
			#endif
		}
		
		public void ResetForce(){
			springLength = restLength;
			springTargetLength = springLength;
			springForce=0;
		}
	
		public float springTravelOffset{
			get {
				return _springTravelOffset;
			}
			set {
				_springTravelOffset = value;
				UpdateSetting();
			}
		}
		
		public void Update(bool isGround,float mass,float bodyMass,float deltaTime,float contactDist,float addForce,out float forceOut){
			float lastLength = springLength;
			float dampingFactor = Mathf.Max(0, 1f - damperStiggness / springStiggness);
			springLength = contactDist;
			float _springForce = springStiggness * (restLength - springLength)/(restLength-minLength);
			float _damperForce = damperStiggness * (lastLength - springLength);
			//springForce += accelerationY * deltaTime;
			forceOut = (_springForce + _damperForce / deltaTime);//Mathf.Max(,0);
			if (!isGround || _springForce<0)
				forceOut *= mass/bodyMass;
			springForce = springForce * dampingFactor + forceOut * deltaTime;
			
			springLength = Mathf.Clamp(contactDist,minLength,maxLength);
			float forceDT = springForce/mass*deltaTime;
			float target = springLength+forceDT;
			if (target <= minLength || target >= maxLength){
				springForce*=0.25f;
				springTargetLength = springLength+springForce/mass*deltaTime;
			}else
				springTargetLength = target;
			
			/*float lastLength = springLength;
			springLength = Mathf.Clamp(contactDist,minLength,maxLength);
			float _springForce = springStiggness * (restLength - springLength)/(restLength-minLength);
			float _damperForce = damperStiggness * (lastLength - springLength);
			Debug.Log((_springForce + _damperForce / deltaTime));
			forceOut = Mathf.Max((_springForce + _damperForce / deltaTime),0);
			if (!isGround)
			forceOut*=mass/bodyMass;*/
			
			
			/*float dampingFactor = Mathf.Max(0, 1f - damperStiggness / springStiggness);
			float acceleration = _springForce;
			springForce = springForce * dampingFactor + (acceleration+_damperForce)* Time.fixedDeltaTime;
			springLength += springForce / mass * Time.fixedDeltaTime;
			
			if (Mathf.Abs(springLength - restLength) < valueThreshold && Mathf.Abs(springForce) < velocityThreshold)
			{
			springLength = restLength;
			springForce = 0f;
			}*/
			
		}
	}
	
	
	public static class Extension{
		
		public static PhysicMaterial phyDefault=new PhysicMaterial();
		static Dictionary<string,Mesh> meshes=new Dictionary<string, Mesh>();
		static Dictionary<Mesh,ulong> usedCount=new Dictionary<Mesh, ulong>();
		public static Mesh GenerateWheel(float radius,float width,float round=0.05f,int detailWheel=11,int detal=0,bool cache=true){
			Mesh wheel=null;
			detailWheel = Mathf.Max(4,detailWheel);
			string name = radius+":"+width+":"+detailWheel+":"+detal+":"+round;
			if (meshes.TryGetValue(name,out wheel)){
				usedCount[wheel]+=1;
				return wheel;
			}else{
				wheel = new Mesh();
				wheel.name = name;
				List<Vector3> vertex=new List<Vector3>();
				List<int> triangles = new List<int>();
				float angle=-90;
				detal++;
			
				float rad = radius;
				for (int d = 0; d <= detal; d++) {
					rad = radius*((1f-round)+(Mathf.Abs(Mathf.Sin(d/(float)detal*Mathf.PI))*round));
					for (int i = 0; i < detailWheel; i++) {
						vertex.Add(new Vector3(-width/2f+(width*(d/(float)detal)),Mathf.Sin(angle*Mathf.Deg2Rad)*rad,Mathf.Cos(angle*Mathf.Deg2Rad)*rad));
						angle+=360f/detailWheel;
					}
				}
				int div=detailWheel-1;
				int point = 0;
				for (int i = point; i < point+div; i++) {
					triangles.Add(point);
					triangles.Add(i);
					triangles.Add(i+1);
				}
				point = vertex.Count-detailWheel;
				for (int i = point; i < point+div; i++) {
					triangles.Add(point);
					triangles.Add(i+1);
					triangles.Add(i);
				}
				int p=0;
				for (int d = 0; d < detal; d++) {
					p=d*detailWheel;
					for (int i = 0; i < detailWheel; i++) {
						triangles.Add(p+(i%detailWheel));
						triangles.Add(p+(i+detailWheel));
						triangles.Add(p+(i+1)%detailWheel);
					
						triangles.Add(p+((i+1)%detailWheel+detailWheel));
						triangles.Add(p+(i+1)%detailWheel);
						triangles.Add(p+(i+detailWheel));
					}
				}
				wheel.SetVertices(vertex);
				wheel.SetTriangles(triangles,0);
				wheel.RecalculateNormals();
			
				if (cache){
					meshes.Add(name,wheel);
					usedCount[wheel]=1;
				}
				return wheel;
			}
		}
		public static bool FreeMesh(Mesh wheelMesh){
			ulong c;
			if (usedCount.TryGetValue(wheelMesh,out c)){
				if (c<=1){
					meshes.Remove(wheelMesh.name);
					usedCount.Remove(wheelMesh);
					MonoBehaviour.DestroyImmediate(wheelMesh);
				}else{
					usedCount[wheelMesh] = c--;
				}
				return true;
			}
			return false;
		}
		
		public static float GetFactorBounciness(this PhysicMaterial aM1, PhysicMaterial aM2)
		{
			if (aM2.bounceCombine == PhysicMaterialCombine.Maximum)
				return Mathf.Max(aM1.bounciness, aM2.bounciness);
			if (aM2.bounceCombine == PhysicMaterialCombine.Multiply)
				return aM1.bounciness * aM2.bounciness;
			if (aM2.bounceCombine == PhysicMaterialCombine.Minimum)
				return Mathf.Min(aM1.bounciness, aM2.bounciness);
			return (aM1.bounciness + aM2.bounciness)*0.5f;
		}
	
		public static float GetFactorDynamic(this PhysicMaterial aM1, PhysicMaterial aM2)
		{
			if (aM2.frictionCombine == PhysicMaterialCombine.Maximum)
				return Mathf.Max(aM1.dynamicFriction, aM2.dynamicFriction);
			if (aM2.frictionCombine == PhysicMaterialCombine.Multiply)
				return aM1.dynamicFriction* aM2.dynamicFriction;
			if (aM2.frictionCombine == PhysicMaterialCombine.Minimum)
				return Mathf.Min(aM1.dynamicFriction, aM2.dynamicFriction);
			return (aM1.dynamicFriction+ aM2.dynamicFriction)*0.5f;
		}
		public static float GetFactorStatic(this PhysicMaterial aM1, PhysicMaterial aM2)
		{
			if (aM2.frictionCombine == PhysicMaterialCombine.Maximum)
				return Mathf.Max(aM1.staticFriction, aM2.staticFriction);
			if (aM2.frictionCombine == PhysicMaterialCombine.Multiply)
				return aM1.staticFriction* aM2.staticFriction;
			if (aM2.frictionCombine == PhysicMaterialCombine.Minimum)
				return Mathf.Min(aM1.staticFriction, aM2.staticFriction);
			return (aM1.staticFriction+ aM2.staticFriction)*0.5f;
		}
		public static float GetFactorDynamicStatic(this PhysicMaterial aM1, PhysicMaterial aM2)
		{
			if (aM2.frictionCombine == PhysicMaterialCombine.Maximum)
				return Mathf.Max(aM1.dynamicFriction, aM2.staticFriction);
			if (aM2.frictionCombine == PhysicMaterialCombine.Multiply)
				return aM1.dynamicFriction* aM2.staticFriction;
			if (aM2.frictionCombine == PhysicMaterialCombine.Minimum)
				return Mathf.Min(aM1.dynamicFriction, aM2.staticFriction);
			return (aM1.dynamicFriction+ aM2.staticFriction)*0.5f;
		}
	}
}

