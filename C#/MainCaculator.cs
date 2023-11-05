/*
 * Document:#MainCaculator.cs#
 * Author: Yuyang Qiu
 * Function:Caculate planet orbit or flight line
 * between two point.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Mapbox.Unity.Utilities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class MainCaculator : MonoBehaviour
{
    // Start is called before the first frame update
    //public GameObject latitudeBox;
    //public GameObject longtitudeBox;
    public GameObject cenObj;
    private const float DegreesToRadians = Mathf.PI / 180f;
    private  float EarthRadius ;
    private float originLatitude = 40.7128f;
    private float originLongitude = -74.0060f;
    private float radius = 1.3f;
    private GameObject testBall;
    private Queue<GameObject> balls;
    public  Queue<Vector3> locations;
    
    public float upDistancce = 1f;
    private LineRenderer lr;
    public List<Vector3> spherePostions;
    // Update is called once per frame
    private void Start()
    {
        EarthRadius = cenObj.transform.localScale.x * 12f;
        generatePosition(40,116);
        generatePosition(43,-79);
        lr = GetComponent<LineRenderer>();
        testBall = GameObject.Find("TestBall");
        locations = new Queue<Vector3>();
        balls = new Queue<GameObject>();
    }

     void Update()
    {
        GeneratePosition();
  
    }

    public Vector3 GPSToWorldPosition(float latitude, float longitude)
    {
        
        float latRad = latitude  * DegreesToRadians;
        float lonRad = longitude * DegreesToRadians;
        
        float originLatRad = originLatitude * DegreesToRadians;
        float originLonRad = originLongitude * DegreesToRadians;
       // Calculate the X, Y, Z coordinates in meters
       float x = EarthRadius * Mathf.Cos(latRad) * Mathf.Cos(lonRad) - EarthRadius * Mathf.Cos(originLatRad) * Mathf.Cos(originLonRad);
       float y = EarthRadius * Mathf.Cos(latRad) * Mathf.Sin(lonRad) - EarthRadius * Mathf.Cos(originLatRad) * Mathf.Sin(originLonRad);
       float z = EarthRadius * Mathf.Sin(latRad) - EarthRadius * Mathf.Sin(originLatRad);

        
        // Create a Vector3 for the world position
        Vector3 worldPosition = new Vector3(x, y, z);

        return worldPosition;
    }


    public void generatePosition(double lat,double longti)
    {
        //float lat    = float.Parse(latitudeBox.GetComponent<TMP_InputField>().text);
        //float longti = float.Parse(longtitudeBox.GetComponent<TMP_InputField>().text);

        Vector3 pos = Conversions.GeoToWorldGlobePosition(lat, longti, EarthRadius);
        Instantiate(GameObject.Find("TestBall"), pos, quaternion.identity);
    }


    void GeneratePosition()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = new Ray();
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit,1000f, 1<<6))
            {
                GameObject curBall = Instantiate(testBall, hit.point, quaternion.identity);

                if (locations.Count == 2)
                {
                    locations.Dequeue();
                }

                if (balls.Count == 2)
                {
                    Destroy(balls.Dequeue());
                }

                balls.Enqueue(curBall);
                locations.Enqueue(hit.point);
            }
        }
    }

    void radiusTester()
    {
        testBall.transform.position = (Camera.main.transform.position - cenObj.transform.position).normalized * radius;
    }

    public void drawLine()
    {
        spherePostions.Clear();

        if (!lr.enabled)
        {
            lr.enabled = true;
        }

        int num = lr.positionCount;
        Vector3[] positions = locations.ToArray();
        
        
        Vector3 centerNew = (positions[0] + positions[1]) * 0.5f;
        Vector3 dir = (centerNew - cenObj.transform.position).normalized;

        upDistancce = -1 + ((Vector3.Distance(positions[0], positions[1]) - 2.5f) * 0.25f );
        centerNew += dir * upDistancce;
        
        Vector3 newPos01 = positions[0] - centerNew ;
        Vector3 newPos02 = positions[1] - centerNew ;
        float n = 1.0f / (num - 1);
        
        for (int i=0;i<num;i++)
        {
            Vector3 pos = Vector3.Slerp(newPos01, newPos02, n * i);
            //Instantiate(GameObject.Find("TestBall"), pos, quaternion.identity);
            pos += centerNew;
            lr.SetPosition(i,pos);
           // Instantiate(GameObject.Find("TestBall"), pos, quaternion.identity);
            spherePostions.Add(pos);
        }
       
       
       
    }
}
