using UnityEngine; 

namespace Mod
{
    public class Mod
    {
        public static void Main()
        {
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Metal Cube"),
                    NameOverride = "Attack Drone",
                    DescriptionOverride = "It flies.",
                    CategoryOverride = ModAPI.FindCategory("Vehicles"),
                    ThumbnailOverride = ModAPI.LoadSprite("thumb.png"),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("droneOff.png");
						Instance.GetComponent<Transform>().localScale = new Vector3(1f, 1f, 1f);
						Instance.GetComponent<BoxCollider2D>().size= new Vector2(1.75f, 0.4f);
						Instance.AddComponent<Drone>();
						Instance.GetComponent<Drone>().offSprite=ModAPI.LoadSprite("droneOff.png");
						Instance.GetComponent<Drone>().onSprite=ModAPI.LoadSprite("drone.png");

						var clip = ModAPI.LoadSound("droneFly.mp3");
						Instance.AddComponent<AudioSource>().clip=clip;
						Instance.GetComponent<AudioSource>().loop=true;
						Instance.GetComponent<AudioSource>().volume=0.25f;

						Instance.AddComponent<UseEventTrigger>().Action = Instance.GetComponent<Drone>().ToggleOn;
                    }
                }
            );
        }
    }

	public class Drone : MonoBehaviour
	{
		const float speed = 70f;
		const float rotSpeed = 10f;

		public Sprite offSprite;
		public Sprite onSprite;
		private Sprite gunSprite;

		public bool on = false;
		public bool canAccelerate=true;

		private int currentFirearm = 0;
		
		private Rigidbody2D rb;
		private FirearmBehaviour firearm;
		private BeamCannonBehaviour beam;
		private FirearmBehaviour blaster;
		private Transform currentWeapon;
		private Camera cam;
		
		private void Awake()
		{
			rb = GetComponent<Rigidbody2D>();
			rb.angularDrag=1f;
			cam=Camera.main;

			gunSprite=ModAPI.LoadSprite("gun.png");

			firearm = GameObject.Instantiate(ModAPI.FindSpawnable("Pistol").Prefab, transform.position, transform.rotation).GetComponent<FirearmBehaviour>();
			firearm.GetComponent<PhysicalBehaviour>().rigidbody.isKinematic=true;
			foreach(Collider2D c in firearm.GetComponent<PhysicalBehaviour>().colliders){c.enabled=false;}
			firearm.Cartridge.Recoil*=4;
			firearm.IgnoreUse=true;

			firearm.GetComponent<SpriteRenderer>().sprite = gunSprite;
			firearm.transform.parent=transform;
			firearm.transform.localPosition=new Vector3(-0.015f, -0.25f, 0f);
			firearm.transform.localScale=new Vector3(0.25f, 0.25f, 0.25f);
			firearm.transform.localEulerAngles=new Vector3(0f, 0f, 270f);

			Transform pivot = new GameObject().transform;
			pivot.position = firearm.transform.position+new Vector3(0f, 0.25f, 0f);
			firearm.transform.parent = pivot;
			firearm.transform.parent.parent=transform;
			currentWeapon=firearm.transform;

			blaster = GameObject.Instantiate(ModAPI.FindSpawnable("Sniper Rifle").Prefab, firearm.transform.position, firearm.transform.rotation).GetComponent<FirearmBehaviour>();
			blaster.GetComponent<PhysicalBehaviour>().rigidbody.isKinematic=true;
			foreach(Collider2D c in blaster.GetComponent<PhysicalBehaviour>().colliders){c.enabled=false;}
			blaster.Cartridge.Damage*=3;
			blaster.Cartridge.Recoil*=5;
			blaster.Cartridge.ImpactForce*=3;
			blaster.transform.localScale=new Vector3(0.5f, 0.5f, 0.5f);
			blaster.transform.parent=pivot;
			blaster.GetComponent<SpriteRenderer>().sprite = gunSprite;
			blaster.gameObject.SetActive(false);

			beam = GameObject.Instantiate(ModAPI.FindSpawnable("Detached Beam Cannon").Prefab, firearm.transform.position, firearm.transform.rotation).GetComponent<BeamCannonBehaviour>();
			beam.GetComponent<PhysicalBehaviour>().rigidbody.isKinematic=true;
			foreach(Collider2D c in beam.GetComponent<PhysicalBehaviour>().colliders){c.enabled=false;}
			beam.OverchargeThreshold=0f;
			beam.transform.localScale=new Vector3(0.75f, 0.75f, 0.75f);
			beam.transform.parent=pivot;
			beam.GetComponent<SpriteRenderer>().sprite = gunSprite;
			beam.gameObject.SetActive(false);
		}
		private void Update()
		{
			if(!on){return;}

			float camDis = cam.transform.position.y - currentWeapon.parent.position.y;
			Vector3 mouse = cam.ScreenToWorldPoint (new Vector3 (Input.mousePosition.x, Input.mousePosition.y, camDis));
			float AngleRad = Mathf.Atan2 (mouse.y - currentWeapon.parent.position.y, mouse.x - currentWeapon.parent.position.x);
			currentWeapon.parent.eulerAngles = new Vector3(0f, 0f, ((180 / Mathf.PI) * AngleRad)+90);

			if (Input.GetMouseButtonDown(0))//left click
			{
				switch(currentFirearm)
				{
					case 0:
						firearm.Shoot();
						break;
					case 1:
						blaster.Shoot();
						break;
					case 2:
						beam.Shoot();
						rb.AddForce(currentWeapon.right*-1500f*Time.deltaTime, ForceMode2D.Impulse); //for some reason the beam cannon does not apply recoil to the drone on its own
						break;
				}
			}

			if(Input.GetKeyDown(KeyCode.H))
			{
				if(rb.gravityScale==0)
				{
					rb.gravityScale=0.75f;
					canAccelerate=true;
				}
				else
				{
					rb.gravityScale=0f;
					canAccelerate=false;
					rb.angularVelocity=0f;
				}
			}

			if(Input.GetKeyDown(KeyCode.R))
			{
				currentFirearm+=currentFirearm>2?-3:1;
				LoadFirearm(currentFirearm);
			}
			
			if(!canAccelerate)
			{
				rb.velocity=Vector3.Lerp(rb.velocity, Vector3.zero, 2*Time.deltaTime);
			}

			if (canAccelerate && Input.GetKey(KeyCode.W))
			{
				rb.AddForce(transform.up*speed*Time.deltaTime, ForceMode2D.Impulse);
			}
			if (Input.GetKey(KeyCode.A))
			{
				//rb.AddForce(transform.right*-speed*Time.deltaTime, ForceMode2D.Impulse);
				rb.AddTorque(rotSpeed*Time.deltaTime, ForceMode2D.Impulse);
			}
			if (Input.GetKey(KeyCode.D))
			{
				//rb.AddForce(transform.right*speed*Time.deltaTime, ForceMode2D.Impulse);
				rb.AddTorque(-rotSpeed*Time.deltaTime, ForceMode2D.Impulse);
			}
		}

		public void ToggleOn()
		{
			on = !on;
			if(on)
			{
				GetComponent<AudioSource>().Play();
				GetComponent<SpriteRenderer>().sprite = onSprite;
				rb.gravityScale=0.5f;
			}
			else
			{
				LoadFirearm(3);
				currentFirearm=3;
				GetComponent<AudioSource>().Stop();
				GetComponent<SpriteRenderer>().sprite = offSprite;
				rb.gravityScale=1f;
				canAccelerate=true;
			}
		}

		private void LoadFirearm(int f)
		{
			if(f==0)
			{
				firearm.gameObject.SetActive(true);
				blaster.gameObject.SetActive(false);
				beam.gameObject.SetActive(false);
				currentWeapon=firearm.transform;
			}
			else if(f==1)
			{
				firearm.gameObject.SetActive(false);
				blaster.gameObject.SetActive(true);
				beam.gameObject.SetActive(false);
				currentWeapon=blaster.transform;
			}
			else if(f==2)
			{
				firearm.gameObject.SetActive(false);
				blaster.gameObject.SetActive(false);
				beam.gameObject.SetActive(true);
				currentWeapon=beam.transform;
			}
			else
			{
				firearm.gameObject.SetActive(false);
				blaster.gameObject.SetActive(false);
				beam.gameObject.SetActive(false);
			}
		}
	}
}