using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Dot_Truck : System.Object
{
	public WheelCollider leftWheel;
	public GameObject leftWheelMesh;
	public WheelCollider rightWheel;
	public GameObject rightWheelMesh;
	public bool motor;
	public bool steering;
	public bool reverseTurn; 
}

public class Dot_Truck_Controller : MonoBehaviour {

    public int m_PlayerNumber = 1;
    public float maxMotorTorque;
	public float maxSteeringAngle;
	public List<Dot_Truck> truck_Infos;
    public float nitroForce = 200000f;
    public float boostedMotorTorque = 30f;
    public float hookForce = 3f;
    public int isBoosted = 0;
    public bool hookMode = false;
    float boostTime = 2f;
    float hookTime = 0.5f;
    public float fixedMotorTorque;

    public float speed = 0f;
    public float currentDirection = 0f;

    public Transform carMeshPrefab;
    Vector3 carMeshBasicPositon;
    public static int maxFlyHeight = 3;
    Rigidbody rb;
    public Rigidbody victimRB;

    float scaleFactor;
    private string verticalButton;
    private string horizontalButton;
    private string brakeButton;
    private string driftButton;

    [Header("AI Stuff")]
    public int fastCheckPointNumber = 0;
    [SerializeField] GameObject optimalCheckPointsObject;
    Transform[] optimalCheckPoints;
    public float forwardDirection = 1f;
    public float horizontalDirection = 0f;

    public float brakeForce = 0f;
    public bool driftingMode = false;

    public bool importantObjectSpotted = false;
    public bool fatalCollision = false;
    Coroutine recoveringFromCollision;
    [SerializeField] float recoveryTime = 0.5f;
    public float recoverDirection = 0f;

    public bool isBot = false;
    public bool isChasingCheckPoint = true;
    public bool warpMode = false;

    public Transform nonCheckPointTarget;

    private void Start()
    {
        carMeshBasicPositon = carMeshPrefab.localPosition;
        rb = GetComponent<Rigidbody>();
        scaleFactor = transform.localScale.z * 1.5f;
        rb.centerOfMass -= Vector3.up * scaleFactor;
        fixedMotorTorque = maxMotorTorque + 30f;
        if(!isBot)
        {
            verticalButton = "Vertical" + m_PlayerNumber;
            horizontalButton = "Horizontal" + m_PlayerNumber;
            brakeButton = "Brake" + m_PlayerNumber;
            driftButton = "Drift" + m_PlayerNumber;
        }
        else
        {
            optimalCheckPoints = new Transform[optimalCheckPointsObject.transform.childCount + 1];
            for(int i = 1; i < optimalCheckPointsObject.transform.childCount + 1; i++)
            {
                optimalCheckPoints[i] = optimalCheckPointsObject.transform.GetChild(i-1);
            }
        }
    }
    public void VisualizeWheel(Dot_Truck wheelPair)
	{
		Quaternion rot;
		Vector3 pos;
		wheelPair.leftWheel.GetWorldPose ( out pos, out rot);
        
        wheelPair.leftWheelMesh.transform.position = pos;
        wheelPair.leftWheelMesh.transform.rotation = rot;

		wheelPair.rightWheel.GetWorldPose ( out pos, out rot);
        
        wheelPair.rightWheelMesh.transform.position = pos;
        wheelPair.rightWheelMesh.transform.rotation = rot;
	}

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Car")
        {
            if (other.gameObject.GetComponent<UsePower>().shieldActive)
            {
                Shield potentialDangerShield = other.gameObject.GetComponent<UsePower>().activatedShield.GetComponent<Shield>();
                if (potentialDangerShield.knockUpForce > 0)
                {
                    print("strongshield");
                    //Transform attackerTransform = potenialDangerShield.GetComponentInParent<Transform>();
                    KnockUpSpin(potentialDangerShield);
                    
                }
            }
        }
    }

    private void KnockUpSpin(Shield potentialDangerShield)
    {
        rb.velocity = Vector3.zero;
        Vector3 direction = -4 * potentialDangerShield.transform.forward + 12 * Vector3.up;
        rb.AddForceAtPosition(direction * potentialDangerShield.knockUpForce * rb.mass, rb.transform.position);
        rb.AddRelativeTorque(0f, potentialDangerShield.torque * 10000 * rb.mass, 0f, ForceMode.VelocityChange);
    }

    public void Update()
    {
        speed = Vector3.Magnitude(rb.velocity);
        SmoothSpeedUp();
        float brakeTorque;
        float steering;
        if(!isBot)
        {
            currentDirection = Input.GetAxis(verticalButton);
            steering = maxSteeringAngle * Input.GetAxis(horizontalButton);
            brakeTorque = Mathf.Abs(Input.GetAxis(brakeButton));
        }
        else
        {
            TurnToCorrectTarget();
            steering = maxSteeringAngle * horizontalDirection;
            ApplyCorrectForwardForce();
            brakeTorque = brakeForce;
        }

        float motor = maxMotorTorque * currentDirection;

        if (isBoosted == 1)
        {
            if (boostTime <= 0f)
            {
                isBoosted = 0;
            }

            NitroBoost(nitroForce * 0.9f);
            boostTime -= Time.deltaTime;
        }

        if (isBoosted == 2)
        {
            if (boostTime <= 0f)
            {
                isBoosted = 0;
            }

            NitroBoost(nitroForce * 0.75f);
            boostTime -= Time.deltaTime;
        }

        if (isBoosted == 3)
        {
            if (boostTime <= 0f)
            {
                isBoosted = 0;
                carMeshPrefab.localPosition = carMeshBasicPositon;
            }

            NitroBoost(nitroForce * 0.25f);
            boostTime -= Time.deltaTime;
        }

        if ( hookMode)
        {   
            if(hookTime <= 0f)
            {
                hookMode = false;
                fixedMotorTorque += 15;
            }
            float distanceBetween = Vector3.Magnitude(transform.position - victimRB.position);

            float boostFactor = 5 + Mathf.Pow(distanceBetween, 0.5f);
            HookBoost(boostFactor);
            HookSlowDown(boostFactor);
            hookTime -= Time.deltaTime;
        }


        if(!isBot)
        {
            bool drift = Input.GetButton(driftButton);
            if (drift)
            {
                truck_Infos[1].steering = true;
            }
            else
            {
                truck_Infos[1].steering = false;
                truck_Infos[1].leftWheel.steerAngle = 0;
                truck_Infos[1].rightWheel.steerAngle = 0;
            }
        }

        if (brakeTorque > 0.001)
        {
            brakeTorque = maxMotorTorque;
            motor = 0;
        }
        else
        {
            brakeTorque = 0;
        }

        foreach (Dot_Truck truck_Info in truck_Infos)
        {
            if (truck_Info.steering == true)
            {
                float brakeBalance = 1f;
                // check when on drift mode
                if (truck_Info.reverseTurn == true)
                    brakeBalance = 1.2f;
                truck_Info.leftWheel.steerAngle = truck_Info.rightWheel.steerAngle = ((truck_Info.reverseTurn) ? -1 : 1) * steering / brakeBalance;
            }

            if (truck_Info.motor == true)
            {
                truck_Info.leftWheel.motorTorque = motor;
                truck_Info.rightWheel.motorTorque = motor;
            }

            truck_Info.leftWheel.brakeTorque = brakeTorque;
            truck_Info.rightWheel.brakeTorque = brakeTorque;

            if(isBoosted != 3)
            VisualizeWheel(truck_Info);
        }

    }

    private void SmoothSpeedUp()
    {
        if(isBoosted != 0)
        {
            maxMotorTorque = boostedMotorTorque;
        }
        else
        {
            if (speed < 1.5f)
                maxMotorTorque = fixedMotorTorque;
            else if (speed < 3f)
                maxMotorTorque = fixedMotorTorque - 15f;
            else
            {
                maxMotorTorque = fixedMotorTorque - 30f;
            }
        }
    }

    void NitroBoost(float boostForce)
    {
        float forceToAdd = (boostForce * (boostTime/2 + 0.5f) * rb.mass) / speed;
        forceToAdd = Mathf.Clamp(forceToAdd, 0f, 50000f);
        rb.AddForce(transform.forward * forceToAdd * boostTime);
        if(isBoosted == 3)
        {
            if(boostTime < 1)
            {
                carMeshPrefab.transform.localPosition -= Vector3.up * 3 * Time.deltaTime;
            }
            else if(carMeshPrefab.transform.localPosition.y < maxFlyHeight)
            carMeshPrefab.transform.localPosition += Vector3.up * 2 * Time.deltaTime;
        }
        //rb.AddForce(Vector3.down * forceToAdd / boostTime * speed);
    }

    void HookBoost(float distance)
    {
        float pullFactor = currentDirection + 1f;
        float forceToAdd = ( (Mathf.Pow(victimRB.velocity.magnitude, 0.6f) + distance) * victimRB.mass * hookForce * pullFactor)  ;
        forceToAdd = Mathf.Clamp(forceToAdd, 0f, 200000f);
        rb.AddForce(transform.forward * forceToAdd);

    }

    void HookSlowDown(float distance)
    {
        float pullFactor = currentDirection + 2f;
        float forceToAdd = ( (Mathf.Pow(victimRB.velocity.magnitude, 0.6f) + distance) * victimRB.mass * 3 * hookForce / pullFactor);
        forceToAdd = Mathf.Clamp(forceToAdd, 0f, 200000f);
        victimRB.AddForce(-transform.forward * forceToAdd);
    }

    public void SetBoostTime(float time)
    {
        boostTime = time;
    }

    public void SetHookTime(float time)
    {
        hookTime = time;
    }

    private void ApplyCorrectForwardForce()
    {
        if(!warpMode)
        {
            if (speed <= 0.3f && fatalCollision)
            {
                if (recoveringFromCollision == null)
                    recoveringFromCollision = StartCoroutine(RecoverFromFatalCollision(recoveryTime));
            }
            else if (recoveringFromCollision == null)
            {
                currentDirection = forwardDirection;
            }
        }
        else
        {
            currentDirection = 0f;
        }
    }
    private void TurnToCorrectTarget()
    {
        if (!importantObjectSpotted)
        {
            Vector3 rawDirection;
            if(isChasingCheckPoint)
            {
                int checkPointIndex = fastCheckPointNumber + 1;
                if (checkPointIndex > optimalCheckPointsObject.transform.childCount)
                {
                    checkPointIndex = 1;
                }
                rawDirection = optimalCheckPoints[checkPointIndex].position - transform.position;
                rawDirection.Normalize();
                //if (Random.Range(0f, 100f) < 10)
                //{
                //    print("R" + rawDirection.ToString());
                //    print(transform.forward.ToString());
                //}
            }
            else
            {
                rawDirection = nonCheckPointTarget.position - transform.position;
                rawDirection.Normalize();
            }
            CalculatingCorrectTurn(rawDirection);
        }
    }
    private void CalculatingCorrectTurn(Vector3 rawDirection)
    {
        float determinantValue = transform.forward.x * rawDirection.z - transform.forward.z * rawDirection.x;
        horizontalDirection = -determinantValue;
        
        if (recoveringFromCollision != null)
        {
            horizontalDirection = recoverDirection;
        }

        if (isBoosted > 0)
        {
            //horizontalDirection *= isBoosted;
            currentDirection = -1;
        }
    }

    public IEnumerator AchivingImportantGoal(float valueOfTurn, float customPathTime)
    {
        importantObjectSpotted = true;
        horizontalDirection = valueOfTurn;
        yield return new WaitForSeconds(customPathTime);
        importantObjectSpotted = false;
    }

    public IEnumerator RecoverFromFatalCollision(float customPathTime)
    {
        currentDirection = -forwardDirection;
        fatalCollision = true;
        print("pocz");
        yield return new WaitForSeconds(customPathTime);
        print("end");
        fatalCollision = false;
        recoveringFromCollision = null;
    }
    public IEnumerator ChangeChaseTarget(Transform newTarget)
    {
        isChasingCheckPoint = false;
        nonCheckPointTarget = newTarget;
        yield return new WaitForSeconds(0.5f);
        if (isChasingCheckPoint == false)
            isChasingCheckPoint = true;
    }

}