/*
 * Document:#XMLManager.cs#
 * Author: Yuyang Qiu
 * Function:Design for task system using .xml file,  currently in progress.
 * between two point.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using System.IO;
public class Task
{
    public int ID { get; set; }
    public String Name { get; set; }

    public int Status { get; set; }
    public String Description { get; set; }
    
}


public class XMLManager : MonoBehaviour
{
    // Start is called before the first frame update
    public List<Task> tasks;
    void Start()
    {
        loadTask();
  
    }


    
    public void loadTask()
    {
        tasks = new List<Task>();
        XmlDocument xml = new XmlDocument();
        xml.Load("Assets/Tasks/Task.xml");
        XmlNodeList nodes = xml.SelectNodes("Task");

        foreach (XmlNode node in nodes)
        {
            Task task = new Task();
            task.ID = int.Parse(node.SelectSingleNode("ID").InnerText);
            task.Description = node.SelectSingleNode("Description").InnerText;
            print(task.ID);
            tasks.Add(task);
        }
    }

    public void saveTasks()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(Task));
        FileStream steam = new FileStream("Assets/Tasks/Task.xml", FileMode.Create);
        serializer.Serialize(steam,tasks[0]);
        steam.Close();
        print("saved");
    }
}
