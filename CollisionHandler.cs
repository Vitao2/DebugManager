using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using static DebugManager;

public class CollisionHandler : MonoBehaviour
{
    DebugManager debugManager;

    //========== Collision Trigger ==========
    //Enter method
    private void OnTriggerEnter(Collider other)
    {
        Debug.LogWarning("Enter Collisor!");

        // Check if the collided object is one of the objects in panelConfigurations
        string collidedObject = gameObject.name;
        
        //Current panel to be tested
        PanelConfiguration config;

        //for each gizmo created
        for (int i = 0; i < debugManager.panelConfigurations.Count; i++)
        {
            //saving the current gizmo
            config = debugManager.panelConfigurations[i];

            //if is the collided object name
            if (collidedObject == config.objectName + " Collider")
            {
                //change the current gizmo color
                config.gizmoCurrentColor = config.gizmoPrimaryColor;
            }
        }
    }
    //Exit method
    private void OnTriggerExit(Collider other)
    {
        Debug.LogWarning("Exit Collisor!");

        // Check if the collided object is one of the objects in panelConfigurations
        string collidedObject = gameObject.name;

        //Current panel to be tested
        PanelConfiguration config;

        //for each gizmo created
        for (int i = 0; i < debugManager.panelConfigurations.Count; i++)
        {
            //saving the current gizmo
            config = debugManager.panelConfigurations[i];

            //if is the collided object name
            if (collidedObject == config.objectName + " Collider")
            {
                //change the current gizmo color
                config.gizmoCurrentColor = config.gizmoSecondaryColor;
            }
        }
    }

    private void Start()
    {
        debugManager = GameObject.Find("DebugManager").GetComponent<DebugManager>();

        //Assign debugManager script to this game object
        if (debugManager != null)
        {
            Debug.Log("DebugManager found");

            for(int i = 0;i < debugManager.panelConfigurations.Count; i++)
            {
                PanelConfiguration config = debugManager.panelConfigurations[i];

                config.gizmoCurrentColor = config.gizmoSecondaryColor;
            }
        }
        else
        {
            Debug.LogError("DebugManager not found");
            Debug.LogError("Change the DebugManager prefab's name to 'DebugManager'");
        }
    }

    private void Update()
    {
        //For each gizmo
        for (int i = 0; i < debugManager.panelConfigurations.Count; i++)
        {
            //Assign the current gizmo
            PanelConfiguration config = debugManager.panelConfigurations[i];
            
            //if it has an origin transform
            if (config.origin != null)
            {
                //update this gameobjet position to it
                transform.position = config.origin.position + config.offSet;

                transform.rotation = config.origin.rotation;

                if (config.isLine)
                {
                    Vector3 startPoint = transform.position;

                    RaycastHit hit;
                    Ray collisionRay;                   

                    collisionRay = new Ray(startPoint, transform.forward);

                    if (Physics.Raycast(collisionRay, out hit, config.gizmoScale))
                    {
                        Debug.LogWarning("Collided with: " + hit.collider.name);
                        config.gizmoCurrentColor = config.gizmoPrimaryColor;
                    }
                    else
                    {
                        config.gizmoCurrentColor = config.gizmoSecondaryColor;
                    }
                    
                }

            }
        }
    }
}
