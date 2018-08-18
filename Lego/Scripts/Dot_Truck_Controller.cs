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
    public float nitroForce = 200f;
    public float hookForce = 3f;
    public bool isBoosted = false;
    public bool isDoubleBoosted = false;
    public bool hookMode = false;
    float boostTime = 2f;
    float hookTime = 0.5f;
    float fixedMotorTorque;

    public float speed = 0f;
    public float currentDirection = 0f;

    Rigidbody rb;
    public Rigidbody victimRB;
    private string verticalButton;
    private string horizontalButton;
    private string brakeButton;
    private string driftButton;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass -= Vector3.up;
        fixedMotorTorque = maxMotorTorque + 1000f;
        verticalButton = "Vertical" + m_PlayerNumber;
        horizontalButton = "Horizontal" + m_PlayerNumber;
        brakeButton = "Brake" + m_PlayerNumber;
        driftButton = "Drift" + m_PlayerNumber;
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

    public void Update()
    {
        speed = Vector3.Magnitude(rb.velocity);
        SmoothSpeedUp();
        currentDirection = Input.GetAxis(verticalButton);
        float motor = maxMotorTorque * currentDirection;
        float steering = maxSteeringAngle * Input.GetAxis(horizontalButton);
        float brakeTorque = Mathf.Abs(Input.GetAxis(brakeButton));

        if (isBoosted)
        {
            if (boostTime <= 0f)
            {
                isBoosted = false;
                gameObject.transform.GetChild(2).gameObject.SetActive(false);
            }

            NitroBoost(nitroForce);
            boostTime -= Time.deltaTime;
        }

        if (isDoubleBoosted)
        {
            if (boostTime <= 0f)
            {
                isDoubleBoosted = false;
                gameObject.transform.GetChild(3).gameObject.SetActive(false);
            }

            NitroBoost(nitroForce * 1.2f);
            boostTime -= Time.deltaTime;
        }

        if ( hookMode)
        {   
            if(hookTime <= 0f)
            {
                hookMode = false;
            }
            float distanceBetween = Vector3.Magnitude(transform.position - victimRB.position);
            HookBoost(distanceBetween);
            HookSlowDown(distanceBetween);
            hookTime -= Time.deltaTime;
        }



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

            VisualizeWheel(truck_Info);
        }

    }

    private void SmoothSpeedUp()
    {
        if (speed < 15f)
            maxMotorTorque = fixedMotorTorque;
        else if (speed < 20f)
            maxMotorTorque = fixedMotorTorque - 500f;
        else
        {
            maxMotorTorque = fixedMotorTorque - 1000f;
        }
    }

    void NitroBoost(float boostForce)
    {
        float forceToAdd = (boostForce * boostTime) / speed;
        forceToAdd = Mathf.Clamp(forceToAdd, 0f, 2000000f);
        rb.AddForce(transform.forward * forceToAdd);
    }

    void HookBoost(float distance)
    {
        float pullFactor = currentDirection + 1f;
        float forceToAdd = ( (victimRB.velocity.magnitude + distance) * victimRB.mass * hookForce * pullFactor)  ;
        forceToAdd = Mathf.Clamp(forceToAdd, 0f, 200000f);
        rb.AddForce(transform.forward * forceToAdd);
    }

    void HookSlowDown(float distance)
    {
        float pullFactor = currentDirection + 2f;
        float forceToAdd = ( (victimRB.velocity.magnitude + distance) * victimRB.mass * hookForce / pullFactor);
        forceToAdd = Mathf.Clamp(forceToAdd, 0f, 200000f);
        victimRB.AddForce(-victimRB.transform.forward * forceToAdd);
    }

    public void SetBoostTime(float time)
    {
        boostTime = time;
    }

    public void SetHookTime(float time)
    {
        hookTime = time;
    }
}