/*
 * Document:#MainController.cs#
 * Author: Yuyang Qiu
 * Function:Controll the main character rotate and related action.
 */



using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainController : MonoBehaviour
{
    /**
     * Character status enum.
     */
    public enum Status
    {
        Normal,
        Climb,
        Destroy
    }
    
    /**
     * Character rotation direction enum.
     */
    public enum RotDirection
    {
        Forward,
        Backward,
        Left,
        Right
    }

        public Status currentStatus;
        private RotDirection currentRotDirection;
        public Transform forward;
        public Transform backward;
        public Transform left;
        public Transform right;
        public GameObject sleepParticles;
        
        [HideInInspector]public GameObject localBall;
        [HideInInspector]public GameObject rotBall;
        [HideInInspector]public GameObject startposBall;

        private Transform allCubes;
        private Rigidbody rigidbody;
        private AudioSource _audioSource;
        private GameObject curSleepParticel;
        private GameObject testBall;
        private ParticleSystem parti;
        private Transform[] VertPositions;
        private Transform[] HoriPositons;
        private Queue<Transform> HoriPosQueue;
        private Queue<Transform> VertPosQueue;
        
        
        private float sleepedTime = 0;
        public float sleepTimePeriod;
        public float rotSpeed;
        public float targetAngle;
        private float stepLength;
        private float vericalAngle    = 0;
        private float horizontalAngle = 0;
        private bool hasDecal  = false;
        private int step       = 0;
        private float waitTime = 0.5f;
        private GameObject curDecal;
        
        private bool isGen = false;
        private bool isOnDecalCube = false;
        private bool isOnWall = false;
        
        private bool startForwardRot  = false;
        private bool startBackwardRot = false;
        private bool startLeftRot     = false;
        private bool startRightRot    = false;
        private bool isFinishedRot    = true;
        private bool isStartRot       = false;

        private bool isSleeping = true;
        private bool horiDir = false;
        private bool vertDir = false;
        private float currentAngle;
        private Vector3 centerPostion;
        private bool canHoriClimb = false;
        private bool canVertClimb = false;
        private void Start()
        {
            initialize();
        }

        private void Update()
        {
            rotationControll();
            
            if (!isSleeping)
            {
                sleepedTime += Time.deltaTime;
            }

            if (sleepedTime >= sleepTimePeriod && sleepedTime <sleepTimePeriod+0.1f)
            {
                awakeParicle();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                centeredPosition();
            }
        }
        
        
        /**
         * switch Character's verticle postion.
         */
        IEnumerator changeVericalPosition()
        {   
            VertPositions[1] = VertPosQueue.Dequeue();
            VertPositions[0] = VertPosQueue.Peek();
            VertPosQueue.Enqueue(VertPositions[1]);
        
            yield return new WaitForSecondsRealtime(0.5f);
           
        }
        
        /**
         * switch Character's horizontal position.
         */
        IEnumerator changeHorizontalPosition()
        {   
            HoriPositons[1] = HoriPosQueue.Dequeue();
            HoriPositons[0] = HoriPosQueue.Peek();
            HoriPosQueue.Enqueue(HoriPositons[1]);
            
            print("hori Switched");
            
            yield return new WaitForSecondsRealtime(0.5f);
            //centeredPosition();
        }
        
        /**
         * Initialize this script.
         * Character status
         * horizontal queue and vertical queue.
         * get debug gameobject.
         */
        private void initialize()
        {
            
            awakeParicle();
            rigidbody = GetComponent<Rigidbody>();
            //rigidbody.constraints = RigidbodyConstraints.;
            _audioSource = GetComponent<AudioSource>();
            testBall = GameObject.Find("TESTBALL");
            allCubes = GameObject.Find("Cubes").transform;
            currentStatus = Status.Normal;
            VertPositions = new Transform[2];
            HoriPositons  = new Transform[2];
            HoriPosQueue  = new Queue<Transform>();
            VertPosQueue  = new Queue<Transform>();
            
            VertPositions[0] = left;
            VertPositions[1] = right;
            HoriPositons[0]  = forward;
            HoriPositons[1]  = backward;
            
            HoriPosQueue.Enqueue(forward);
            HoriPosQueue.Enqueue(backward);
            VertPosQueue.Enqueue(left);
            VertPosQueue.Enqueue(right);
        }
        
        /**
         * Function for controll the Character.
         */
        void rotationControll()
        {
            if (isStartRot)
            {
                currentAngle += rotSpeed * Time.deltaTime;
                if (startForwardRot)
                {
                    horizontalAngle += rotSpeed * Time.deltaTime;
                    int dir = vertDir == false ? 1 : -1;
                    transform.RotateAround(HoriPositons[0].position,HoriPositons[0].right,(rotSpeed * dir) * Time.deltaTime );
                }

                if (startBackwardRot)
                {
                    horizontalAngle += rotSpeed * Time.deltaTime;
                    int dir = vertDir == false ? 1 : -1;
                    transform.RotateAround(HoriPositons[1].position,HoriPositons[1].right,(-rotSpeed * dir) * Time.deltaTime );
                }
            
                if (startLeftRot)
                {
                    vericalAngle += rotSpeed * Time.deltaTime;
                    int dir = horiDir == false ? 1 : -1;
                    transform.RotateAround(VertPositions[0].position,VertPositions[0].forward,(rotSpeed * dir) * Time.deltaTime );
                }
            
                if (startRightRot)
                {
                    vericalAngle += rotSpeed * Time.deltaTime;
                    int dir = horiDir == false ? 1 : -1;
                    transform.RotateAround(VertPositions[1].position,VertPositions[1].forward,(-rotSpeed * dir) * Time.deltaTime );
                }
            }

            

            if (vericalAngle >= targetAngle)
            {
                startLeftRot  = false;
                startRightRot = false;
                //isFinishedRot = true;
                StartCoroutine(OnfinishRotation());
                isStartRot    = false;
                vertDir = vertDir == false ? true : false;
               // if(canVertClimb)  rigidbody.constraints -= RigidbodyConstraints.FreezePosition;
                vericalAngle = 0;
                StartCoroutine(changeVericalPosition());
                if(!isOnWall) resetRigidbody();
                
            }

            if (horizontalAngle >= targetAngle)
            {
                startForwardRot  = false;
                startBackwardRot = false;
                StartCoroutine(OnfinishRotation());
                //isFinishedRot = true;
                isStartRot    = false;
                horiDir = horiDir == false ? true : false;
                //if(canHoriClimb)  rigidbody.constraints -= RigidbodyConstraints.FreezePosition;
                horizontalAngle  = 0;
                StartCoroutine(changeHorizontalPosition());
                if(!isOnWall) resetRigidbody();
                
            }


            if (Input.GetKeyDown(KeyCode.W))  {startForwardRot  = true;  currentRotDirection = RotDirection.Forward;  if (isFinishedRot) OnstartRotation();}
            if (Input.GetKeyDown(KeyCode.S))  {startBackwardRot = true;  currentRotDirection = RotDirection.Backward; if (isFinishedRot) OnstartRotation();}
            if (Input.GetKeyDown(KeyCode.A))  {startLeftRot     = true;  currentRotDirection = RotDirection.Left;     if (isFinishedRot) OnstartRotation();}
            if (Input.GetKeyDown(KeyCode.D))  {startRightRot    = true;  currentRotDirection = RotDirection.Right;    if (isFinishedRot) OnstartRotation();}
            

            if (transform.position.y < -14f)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
        
        /**
         * Freeze Character rotation.
         */
        public void freezeRotation()
        {
            //rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationY ;
        }

        /**
         * Unfreeze Character rotation.
         */
        public void UnfreezeRotation()
        {
            rigidbody.constraints -= RigidbodyConstraints.FreezeRotationY ;
        }
        
        /**
         * Called when the Character finished current rotation.
         */
        IEnumerator OnfinishRotation()
        {
            
            yield return new WaitForSecondsRealtime(waitTime);
            isFinishedRot = true;
        }

        /// <summary>
        /// Set character postion to center of current cube.
        /// </summary>
        
        void centeredPosition()
        {
            Vector3 centeredPos = new Vector3(getCenteredCoord(transform.position.x),
                getCenteredCoord(transform.position.y)-0.95f,
                getCenteredCoord(transform.position.z));
            
                transform.position = centeredPos;
      
        }
        
        /// <summary>
        /// Get rounded Integer.
        /// </summary>
        /// <param name="num">float number</param>
        /// <returns>Rounded Integer</returns>
        int getCenteredCoord(float num)
        {
            if (num >-1 & num < 1) return 0;
            int dir = num > 0 ? 1 : -1
            int finalNum = ((int)num) % 2 == 0 ? (int)num : (int)num + 1 * dir;
            
            return finalNum;
         
        }
        
        
        /// <summary>
        /// Determine whether the cube at this position is climbable.
        /// </summary>
        /// <param name="position"></param>
        /// <returns>True if is climbable, false if not.</returns>
        bool isClimbable(Vector3 position)
        {
            foreach (Transform cube in allCubes)
            {
                if (cube.transform.position == position)
                {
                    print("canClimb");
                    return true;
                }
            }
            
            return false;
        }
        
        
        /// <summary>
        /// Detect if there is a climbable cube
        /// in the target rotation direction.
        /// </summary>
       
        void detectCube()
        {
                //onStartCheckRotation();
                Vector3 a = (HoriPositons[0].position  - HoriPositons[1].position).normalized;
                Vector3 b = (VertPositions[1].position - VertPositions[0].position).normalized;
                Vector3 c = Vector3.Cross(a, b);
                Vector3 startPosition = transform.position + c;
                
                centerPostion = new Vector3(getCenteredCoord(startPosition.x,0), getCenteredCoord(startPosition.y,1),
                    getCenteredCoord(startPosition.z,0));
                
                float dis = 1.5f;
                float wallAngle = 88.5f;

                Vector3 wldCenteredPos   = Vector3.zero;
                Vector3 localCenteredPos = Vector3.zero;
                Vector3 addDir;
                int horiAdd = HoriPositons[1].position.y  > HoriPositons[0].position.y  ? -1 : 1;
                int verAdd  = VertPositions[0].position.y > VertPositions[1].position.y ? -1 : 1;
                switch (currentRotDirection)
                {
                        
                    case RotDirection.Forward:
                        addDir = isOnWall ? new Vector3(0, 2 * horiAdd, 0) : new Vector3(0, 0, 2);
                        localCenteredPos = centerPostion + addDir;
                        wldCenteredPos   = isOnWall? localCenteredPos + new Vector3(0, 0, 2) : localCenteredPos;
                        break;
                    case RotDirection.Backward:
                        addDir = isOnWall ? new Vector3(0, -2 * horiAdd, 0) : new Vector3(0, 0, -2);
                        localCenteredPos = centerPostion + addDir;
                        wldCenteredPos   = isOnWall? localCenteredPos + new Vector3(0, 0, -2) : localCenteredPos;
                        break;
                    case RotDirection.Left:
                        addDir = isOnWall ? new Vector3(0, -2 * verAdd, 0) : new Vector3(-2, 0, 0);
                        localCenteredPos = centerPostion + addDir;
                        wldCenteredPos   = isOnWall? localCenteredPos + new Vector3(-2, 0, 0) : localCenteredPos;
                        break;
                    case RotDirection.Right: 
                        addDir = isOnWall ? new Vector3(0, 2 * verAdd, 0) : new Vector3(2, 0, 0);
                        localCenteredPos = centerPostion + addDir;
                        wldCenteredPos   = isOnWall? localCenteredPos + new Vector3(2, 0, 0) : localCenteredPos;
                        break;
                }
                //print("targetclib:"+localCenteredPos);
                print("wallClimb: "+ wldCenteredPos);
               
                if (currentStatus == Status.Climb)
                {
                    targetAngle = 170;
                    if (isOnWall)
                    {
                        bool canC = false;
                        if (isClimbable(wldCenteredPos))
                        {
                            targetAngle = 180;
                            canC = true;
                        }
                        else
                        {
                            canC = false;
                            targetAngle = 220;
                            isOnWall = false;
                           
                        }
                        if (canC)
                        {
                            if (isClimbable(localCenteredPos))
                            {
                               
                                targetAngle = wallAngle;
                                isOnWall = false;
                            }
                            else
                            {
                               
                                targetAngle = 180f;
                            }

                        }
                    }
                    

                    if (!isOnWall && isClimbable(localCenteredPos) )
                    {
                        //print("Climb");
                        rigidbody.useGravity  = false; 
                        rigidbody.isKinematic = true;
                        targetAngle = wallAngle;
                        isOnWall = true;
                    }
                  
                }
                else
                {
                    targetAngle = 170f;
                    if (isClimbable(localCenteredPos))
                    {
                        targetAngle = 80;
                        if ( currentRotDirection == RotDirection.Backward || currentRotDirection == RotDirection.Forward)
                        {
                            canHoriClimb = true;
                           
                        }
                        if (currentRotDirection == RotDirection.Left || currentRotDirection == RotDirection.Right)
                        {
                            canVertClimb = true;
                            
                        }

                    }

                }

        }

        
        /// <summary>
        /// Change the material on character depend on current status.
        /// </summary>
        public void switchMaterial()
        {
            Color targetColor;
            Material mat = null;
            switch (currentStatus)
            {
                case Status.Normal:
                    mat = transform.GetChild(0).GetComponent<MeshRenderer>().material = GetComponent<materialHelper>().normalMat;
                    
                    break;
                case Status.Climb:
                    mat = transform.GetChild(0).GetComponent<MeshRenderer>().material = GetComponent<materialHelper>().climbMat;
                    
                    break;
                case Status.Destroy:
                    mat = transform.GetChild(0).GetComponent<MeshRenderer>().material = GetComponent<materialHelper>().destoryMat;
                    break;
            }
            targetColor = mat.GetColor("_MainColor");
            transform.GetChild(0).GetComponent<MeshRenderer>().materials[1].SetColor("_MainColor",targetColor);
           
        }
        
        /// <summary>
        /// Reset rigidbody to original parameter.
        /// </summary>
       
        void resetRigidbody()
        {
            rigidbody.useGravity = true;
            rigidbody.isKinematic = false;
           
        }
        
        
        /// <summary>
        /// Calls when character start to rotate.
        /// </summary>
        
        void OnstartRotation()
        {
            detectCube();
            isFinishedRot = false;
            isStartRot    = true;
            isSleeping    = false;
            sleepedTime   = 0;
            _audioSource.Play();
            Destroy(curSleepParticel);
        }

        /// <summary>
        /// Awake particle when character sleep.
        /// </summary>
        
        public void awakeParicle()
        {
            isSleeping = true;
            curSleepParticel = Instantiate(sleepParticles, transform.position, quaternion.identity);
            curSleepParticel.transform.parent = this.transform;
            curSleepParticel.transform.rotation = sleepParticles.transform.rotation;
            sleepedTime = 0;
        }

        private void OnTriggerEnter(Collider other)
        {
            float dotResult = Vector3.Dot(transform.up, Vector3.up);
            if (!hasDecal && other.CompareTag("DecalCube") && (dotResult >= 0.95f||dotResult <= -0.95f))
            {
                GameObject decal = other.transform.GetChild(0).gameObject;
                curDecal = Instantiate(decal, transform.position, Quaternion.LookRotation(Vector3.up));
                curDecal.transform.parent = this.transform;
                step = 4;
                stepLength = 1.0f / step;
                hasDecal = true;
            }

            
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("DecalCube"))
            {
                isOnDecalCube = true;
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("DecalCube"))
            {
                isOnDecalCube = false;
            }
        }

        private void OnCollisionStay(Collision other)
        {
            if (other.gameObject.CompareTag("Ground") && step>0)
            {
                
                float dotResult = MathF.Abs(Vector3.Dot(transform.up, Vector3.up));
                if (dotResult <= 0.1)
                {
                    isGen = false;
                }

                bool isGround = dotResult >0.8f;

                if (step == 0){
                        Destroy(curDecal);
                        hasDecal = false;
                    }

                
            }
        }

      

}
