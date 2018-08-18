﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class UsePower : MonoBehaviour
    {
        public int m_PlayerNumber = 1;              // Used to identify the different players.
        public Rigidbody m_Shell;                   // Prefab of the shell.
        public Transform m_FireTransform;           // A child of the tank where the shells are spawned.
        public Slider m_AimSlider;                  // A child of the tank that displays the current launch force.
        public AudioSource m_ShootingAudio;         // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
        public AudioClip m_ChargingClip;            // Audio that plays when each shot is charging up.
        public AudioClip m_FireClip;                // Audio that plays when each shot is fired.
        public float m_MinLaunchForce = 15f;        // The force given to the shell if the fire button is not held.
        public float m_MaxLaunchForce = 30f;        // The force given to the shell if the fire button is held for the max charge time.
        public float m_MaxChargeTime = 0.75f;

        private string m_FireButton;                // The input axis that is used for launching shells.
        private float m_CurrentLaunchForce;         // The force that will be given to the shell when the fire button is released.
        private float m_ChargeSpeed;                // How fast the launch force increases, based on the max charge time.
        private bool m_Fired;                       // Whether or not the shell has been launched with this button press.

        public int currentBoostType = 0;
        public int whiteBlocksNumber = 0;
        public bool shieldActive = false;
        public GameObject trapPrefab;
        public float boostTime = 2f;

        public Rigidbody anchorPrefab;

        public Rigidbody explosionBarrelPrefab;

        public Sprite[] blockImages;
        public Image shownImage;

        Dot_Truck_Controller carMovement;

        public void SetBoost(int value)
        {
            currentBoostType = value;
            shownImage.sprite = blockImages[value-1];
            shownImage.color = new Color(255, 255, 255, 255);
        }
        
        void ResetBoost()
        {
            currentBoostType = 0;
            whiteBlocksNumber = 0;
            shownImage.color = new Color(255, 255, 255, 0);
        }

        private void OnEnable()
        {
            // When the tank is turned on, reset the launch force and the UI
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_AimSlider.value = m_MinLaunchForce;
            //gameObject.transform.GetChild(0).gameObject.SetActive(false);
    }


        private void Start()
        {
            // The fire axis is based on the player number.
            m_FireButton = "Fire" + m_PlayerNumber;

            // The rate that the launch force charges up is the range of possible forces by the max charge time.
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;

            carMovement = GetComponent<Dot_Truck_Controller>();
        }
        

        private void Update()
        {
        if (currentBoostType == 1)
        {
            if (whiteBlocksNumber == 1)
                AnchorLanuch();
            else
                BombLanuch();
        }
        else if (currentBoostType == 2)
        {
            if (whiteBlocksNumber == 1)
                GreenShield();
            else
                BlueShield();

        }
        else if (currentBoostType == 3)
        {
            if (whiteBlocksNumber == 1)
                GunpowderBarrel();
            else
                PlantTrap();
        }
        else if (currentBoostType == 4)
        {
            if (whiteBlocksNumber == 1)
                DoubleTurboBoost();
            else
                TurboBoost();
        }
    }

    private void TurboBoost()
    {
        if(Input.GetButtonDown(m_FireButton))
        {
            carMovement.SetBoostTime(boostTime);
            carMovement.isBoosted = true;
            gameObject.transform.GetChild(2).gameObject.SetActive(true);
            ResetBoost();
        }
    }

    private void DoubleTurboBoost()
    {
        if (Input.GetButtonDown(m_FireButton))
        {
            print("work");
            carMovement.SetBoostTime(boostTime * 1.2f);
            carMovement.isDoubleBoosted = true;
            gameObject.transform.GetChild(3).gameObject.SetActive(true);
            ResetBoost();
        }
    }
    private void PlantTrap()
    {   
        if(Input.GetButtonDown(m_FireButton))
        {
            Instantiate(trapPrefab, transform.position, transform.rotation);
            ResetBoost();
        }
    }

    private void GunpowderBarrel()
    {
        if (Input.GetButtonDown(m_FireButton))
        {
            FireBarrel();
            ResetBoost();
        }
    }

    private void FireBarrel()
    {
        Rigidbody barrel =
                Instantiate(explosionBarrelPrefab, m_FireTransform.position + new Vector3(0f, -1f, -2f), transform.rotation) as Rigidbody;

        barrel.velocity = m_MinLaunchForce * -transform.forward;
        BarrelExplosion explosionBarrel = barrel.GetComponent<BarrelExplosion>();
        explosionBarrel.gravityScale = -8;
    }

    void GreenShield()
    {
        if (Input.GetButtonDown(m_FireButton))
        {
            StartCoroutine(ShieldDuration(whiteBlocksNumber, 7f) );
            ResetBoost();
        }

    }

    void BlueShield()
    {
        if (Input.GetButtonDown(m_FireButton))
        {
            StartCoroutine(ShieldDuration(whiteBlocksNumber, 5f));
            ResetBoost();
        }

    }

    IEnumerator ShieldDuration(int childNumber, float duration)
    {
        gameObject.transform.GetChild(childNumber).gameObject.SetActive(true);
        shieldActive = true;
        yield return new WaitForSeconds(duration);
        gameObject.transform.GetChild(childNumber).gameObject.SetActive(false);
        shieldActive = false;
    }

    void AnchorLanuch()
    {
        if (Input.GetButton(m_FireButton))
        {
            Transform target = m_FireTransform.GetComponent<RotateToTarget>().LookAt();
            Rigidbody anchorInstance;
            if (target != null)
            {
                anchorInstance =
                Instantiate(anchorPrefab, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;

                anchorInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward * 2f;
                AnchorMovement anchor = anchorInstance.GetComponent<AnchorMovement>();
                anchor.gravityScale = -11;
                anchor.target = target;
                anchor.orginCar = carMovement;
            }

            ResetBoost();
        }
    }
    private void BombLanuch()
        {
            m_AimSlider.value = m_MinLaunchForce;

            // If the max force has been exceeded and the shell hasn't yet been launched...
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                // ... use the max force and launch the shell.
                m_CurrentLaunchForce = m_MaxLaunchForce;
                FireBomb();
            }
            // Otherwise, if the fire button has just started being pressed...
            else if (Input.GetButtonDown(m_FireButton))
            {
                // ... reset the fired flag and reset the launch force.
                m_Fired = false;
                m_CurrentLaunchForce = m_MinLaunchForce;

                // Change the clip to the charging clip and start it playing.
                m_ShootingAudio.clip = m_ChargingClip;
                m_ShootingAudio.Play();
            }
            // Otherwise, if the fire button is being held and the shell hasn't been launched yet...
            else if (Input.GetButton(m_FireButton) && !m_Fired)
            {
                // Increment the launch force and update the slider.
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;

                m_AimSlider.value = m_CurrentLaunchForce;
            }
            // Otherwise, if the fire button is released and the shell hasn't been launched yet...
            else if (Input.GetButtonUp(m_FireButton) && !m_Fired)
            {
                // ... launch the shell.
                FireBomb();
            }
        }

        private void FireBomb()
        {
            // Set the fired flag so only Fire is only called once.
            m_Fired = true;

        Rigidbody shellInstance;
        Transform target = m_FireTransform.GetComponent<RotateToTarget>().LookAt();
        if (target != null)
        {
            print("Aim");
            //m_FireTransform.Rotate(-2f, 0f, 0f);
            shellInstance =
                Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;

            shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward;
            ShellExplosion bomb = shellInstance.GetComponent<ShellExplosion>();
            bomb.gravityScale = -11;
            bomb.target = target;
        }
        else
        {
            print("No");
            shellInstance =
                Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
            shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward;
            shellInstance.GetComponent<ShellExplosion>().gravityScale = -15;
        }
        // Change the clip to the firing clip and play it.
        m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();

        // Reset the launch force.  This is a precaution in case of missing button events.
        ResetBoost();
        }
        
    }