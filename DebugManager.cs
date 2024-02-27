using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.UI;

public class DebugManager : MonoBehaviour
{
    [Header("Fill with your existing canvas object")]
    [HideInInspector] public string canvasName;

    [System.Serializable]
    public class PanelConfiguration
    {
        #region Variables
        //Game Objects
        [HideInInspector] public GameObject myGO;
        [HideInInspector] public GameObject debugFolder;
        [HideInInspector] public GameObject myText;

        //Tabs
        [HideInInspector] public int currentTab;

        #region Gizmo Lines
        //========== Lines ==========
        //Gizmo type
        [HideInInspector] public enum GizmoType { line, cube, sphere };
        [HideInInspector] public GizmoType gizmoType;

        //Bools
        [HideInInspector] public bool isScriptDistance, isLine, isCube, isSphere;
        [HideInInspector] public bool isWire = true;

        //Collider
        [HideInInspector] public bool isCollision;

        //Color
        [HideInInspector] public Color gizmoPrimaryColor = Color.green, gizmoSecondaryColor = Color.red;
        [HideInInspector] public Color gizmoCurrentColor = Color.white;

        //Int
        [HideInInspector] public int lineAmount = 30;

        //Float
        [HideInInspector] public float gizmoScale = 5;
        [HideInInspector] public float lineDuration = 1;

        //Transform
        [HideInInspector] public Transform origin;
        [HideInInspector] public Transform direction;

        //Vector3
        [HideInInspector] public Vector3 offSet;

        #endregion

        //Panel
        [HideInInspector] public GameObject panel;
        [HideInInspector] public Image myImage;
        [HideInInspector] public TextMeshProUGUI text;

        //Rect Transform
        [HideInInspector] public RectTransform rectTransform;
        public Vector2 panelDimension = new Vector2(200, 100);

        //=============================
        [Header("Object")]
        public string objectName;
        [Header("Text")]
        public Color textColor = Color.black;
        public float fontSize = 40;
        [Header("Variables")]

        //Scripts
        [HideInInspector] public MonoBehaviour script;
        [HideInInspector] public int variableIndex = 0;

        //Variables types
        [HideInInspector] public enum Variable { boolean, floats, integer, strings, transform };
        [HideInInspector] public Variable variable;
        public object scriptVariable;

        //save the type of the chosen variable
        [HideInInspector] public bool boolean, floats, integer, strings, transform;

        //Float
        [HideInInspector] public bool interval;
        [HideInInspector] public float minNumber = 0, maxNumber = 1;

        //Transform
        [SerializeField] public bool position, rotation, scale;

        //Colors
        [HideInInspector] public Color primaryColor = Color.green;
        [HideInInspector] public Color secondaryColor = Color.red;

        #endregion
    }


    [HideInInspector] public List<PanelConfiguration> panelConfigurations = new List<PanelConfiguration>();

    #region Setup
    //Canvas setup
    public void CanvasSetup(int configurationIndex)
    {
        if (configurationIndex < 0 || configurationIndex >= panelConfigurations.Count)
        {
            Debug.Log("Invalid configuration index");
            return;
        }

        PanelConfiguration config = panelConfigurations[configurationIndex];

        //========== Canvas ==========
        // Check if there's already a Canvas
        GameObject existingCanvas = GameObject.Find(canvasName);
        // Check if there's already a debug folder
        GameObject existingFolder = GameObject.Find("Debug");

        // if there's no canvas. Create one.
        if (existingCanvas == null)
        {
            // Creating game object
            config.myGO = new GameObject();
            config.myGO.name = canvasName;
            config.myGO.AddComponent<Canvas>();
            // Creating canvas
            Canvas myCanvas = config.myGO.GetComponent<Canvas>();
            myCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            config.myGO.AddComponent<CanvasScaler>();
            config.myGO.AddComponent<GraphicRaycaster>();

        }
        else
        {
            //Debug Message
            Debug.LogWarning("There's already an existing Canvas");
            // Assigning the existing Canvas to the MyGO game object
            config.myGO = existingCanvas.gameObject;
        }

        //if there's no debug folder. Create one.
        if (existingFolder == null)
        {
            // creates the gameObject
            config.debugFolder = new GameObject();
            config.debugFolder.name = "Debug";
        }
        // if there's already a folder
        else
        {
            // assign the existing folder
            config.debugFolder = existingFolder.gameObject;
        }

        // make the debug folder gameObject son of the canvas gameObject
        config.debugFolder.transform.parent = config.myGO.transform;
        config.debugFolder.transform.localPosition = Vector3.zero;
        config.debugFolder.transform.localScale = Vector3.one;

        //========== Panel ==========
        // panel setup
        config.panel = new GameObject(); //creating object
        config.panel.name = config.objectName + " Panel"; //naming
        config.panel.AddComponent<Image>(); //adding the Image component
        config.panel.transform.parent = config.debugFolder.transform; //setting as the son of the debug folder
        // transform
        config.panel.transform.localPosition = Vector3.zero;
        config.panel.transform.localScale = Vector3.one;
        config.rectTransform = config.panel.GetComponent<RectTransform>();
        config.rectTransform.sizeDelta = config.panelDimension;

        // image settings
        config.myImage = config.panel.GetComponent<Image>();
        config.myImage.color = config.primaryColor;


        //========== Text ==========
        config.myText = new GameObject();
        config.myText.transform.parent = config.panel.transform;
        config.myText.name = config.objectName + " Text";
        config.myText.AddComponent<TextMeshProUGUI>();
        config.text = config.myText.GetComponent<TextMeshProUGUI>();
        config.text.text = config.objectName;
        config.text.fontSize = config.fontSize;
        config.text.color = config.textColor;
        config.text.transform.localScale = Vector3.one;
        config.text.alignment = TextAlignmentOptions.Center;
        config.text.alignment = TextAlignmentOptions.Midline;

        //Text position
        config.rectTransform = config.text.GetComponent<RectTransform>();
        config.rectTransform.localPosition = new Vector3(0, 0, 0);
        config.rectTransform.sizeDelta = config.panelDimension;
    }
    #endregion

    #region Functions

    //Update each frame
    private void Update()
    {
        UpdatePanel();

        for (int configurationIndex = 0; configurationIndex < panelConfigurations.Count; configurationIndex++)
        {
            //if is a line and the panel is set to gizmo
            if (panelConfigurations[configurationIndex].isLine && panelConfigurations[configurationIndex].currentTab == 1)
            {
                DrawLines(configurationIndex);
            }
            
        }
    }

    //Gizmo update
    private void OnDrawGizmos()
    {
        for(int i = 0; i < panelConfigurations.Count; i++)
        {
            if (panelConfigurations[i].currentTab == 1)
            {
                if (panelConfigurations[i].isCube)
                {
                    DrawCube(i);
                }

                if (panelConfigurations[i].isSphere)
                {
                    DrawSphere(i);
                }
            }
        
        }
        
    }

    //Draw line Method
    private void DrawLines(int configurationIndex)
    {
        PanelConfiguration config = panelConfigurations[configurationIndex];

        //Vectors
        Vector3 startPoint, endPoint;

        //check if the draw line option is true
        if (config.currentTab == 1)
        {
            //check if the origin isn't null
            if(config.origin != null)
            {
                //Saving the end point location
                startPoint = config.origin.position + config.offSet;

                //if the direction was set
                if(config.direction != null)
                {
                    endPoint = startPoint - config.direction.forward * config.gizmoScale;
                }
                else
                {
                    //direction is the start point forward
                    endPoint = startPoint + config.origin.forward * config.gizmoScale;
                }

                config.lineDuration = (config.lineAmount - 1) * Time.deltaTime;
                
                //Draw the lines
                Debug.DrawLine(startPoint, endPoint, config.gizmoCurrentColor, config.lineDuration);

            }
            else
            {
                Debug.LogWarning("Line origin is null");
            }
        }
    }

    //Draw cube method
    private void DrawCube(int configurationIndex)
    {
        //Panel class reference
        PanelConfiguration config = panelConfigurations[configurationIndex];    

        //Gizmo setup
        Gizmos.color = config.gizmoCurrentColor;       

        //initial position;
        Vector3 startPosition;
       
        //Check if there's a transform variable
        if (config.origin != null)
        {
            startPosition = config.origin.position + config.offSet;
        }
        else
        {
            startPosition = config.offSet;
        }

        if (config.isWire)
        {
            Gizmos.DrawWireCube(startPosition, 0.05f * config.gizmoScale * Vector3.one);
        }
        else
        {
            Gizmos.DrawCube(startPosition, 0.05f * config.gizmoScale * Vector3.one);
        }

    }

    //Draw Sphere method
    private void DrawSphere(int configurationIndex)
    {
        //Panel class reference
        PanelConfiguration config = panelConfigurations[configurationIndex];

        //Gizmo setup

        //Color
        Gizmos.color = config.gizmoCurrentColor;

        //initial position;
        Vector3 startPosition;

        //Check if there's a transform variable
        if (config.origin != null)
        {
            startPosition = config.origin.position + config.offSet;
        }
        else
        {
            startPosition = config.offSet;
        }

        if (config.isWire)
        {           
            Gizmos.DrawWireSphere(startPosition, config.gizmoScale * 0.05f);
        }
        else
        {
            Gizmos.DrawSphere(startPosition, config.gizmoScale * 0.05f);
        }

    }

    //GameObject setup
    public void CreateGameObject(Vector3 startPosition, int i)
    {
        //Panel class reference
        PanelConfiguration config = panelConfigurations[i];

        GameObject newGameObject;

        newGameObject = new GameObject();

        //name
        newGameObject.name = config.objectName + " Collider";

        //position
        newGameObject.transform.position = startPosition;

        //scale
        newGameObject.transform.localScale = Vector3.one * (config.gizmoScale * 0.05f);

        //collider
        if (config.isCube)
        {
            BoxCollider collider = newGameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.transform.localScale = Vector3.one * (config.gizmoScale * 0.05f);
        }
        else if(config.isSphere)
        {
            SphereCollider collider = newGameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.transform.localScale = Vector3.one * (config.gizmoScale * 0.1f);
        }

        //collision script
        newGameObject.AddComponent<CollisionHandler>();
    }

    #region Panel Update Methods

    //========== Main Method ==========
    private void UpdatePanel()
    {
        for(int configurationIndex = 0; configurationIndex < panelConfigurations.Count; configurationIndex++)
        {
            PanelConfiguration config = panelConfigurations[configurationIndex];

            //check if the variable and the image panel isn't null 
            if (config.scriptVariable != null && config.myImage != null && config.currentTab == 0)
            {
                print("Value: " + config.scriptVariable);
                UpdateBool(configurationIndex);
                UpdateInt(configurationIndex);
                UpdateFloat(configurationIndex);
                UpdateString(configurationIndex);
                UpdateTransform(configurationIndex);
                
            }
        }
    }

    //========== Bool Treatment ==========
    private void UpdateBool(int configurationIndex)
    {
        PanelConfiguration config = panelConfigurations[configurationIndex];

        if (config.scriptVariable.GetType() == typeof(bool))
        {
            if ((bool)config.scriptVariable)
            {
                config.myImage.color = config.primaryColor;
            }
            else
            {
                config.myImage.color = config.secondaryColor;
            }
        }   
    }

    //========== Int Treatment ==========
    private void UpdateInt(int configurationIndex)
    {
        PanelConfiguration config = panelConfigurations[configurationIndex];

        if (config.scriptVariable.GetType() == typeof(int))
        {
            //save the variable value
            float value = (int)config.scriptVariable;

            if (config.text != null)
            {
                config.text.text = config.objectName + ": " + config.scriptVariable.ToString();

                //if the interval is true
                if (config.interval)
                {
                    //if the value is between the minimum and maximum values
                    if (config.minNumber <= value && value <= config.maxNumber)
                    {
                        //set the panel color to the primary color
                        config.myImage.color = config.primaryColor;
                    }
                    else
                    {
                        //set the panel color to the secondary color
                        config.myImage.color = config.secondaryColor;
                    }
                }
            }
            else
            {
                print("Text is null");
            }
            
        }
    }

    //========== Float Treatment ==========
    private void UpdateFloat(int configurationIndex)
    {
        PanelConfiguration config = panelConfigurations[configurationIndex];

        //If the type of the variable is float
        if(config.scriptVariable.GetType() == typeof(float))
        {
            //save the variable value
            float value = (float)config.scriptVariable;

            //check is the text isn't null
            if(config.text != null)
            {
                //change the panel text to the variable value text
                config.text.text = config.objectName + ": " + ((float)config.scriptVariable).ToString("F2");
                
                //if the interval is true
                if (config.interval)
                {
                    //if the value is between the minimum and maximum values
                    if (config.minNumber <= value && value <= config.maxNumber)
                    {
                        //set the panel color to the primary color
                        config.myImage.color = config.primaryColor;
                    }
                    else
                    {
                        //set the panel color to the secondary color
                        config.myImage.color = config.secondaryColor;
                    }
                }
                
            }
            else
            {
                print("Text is null");
            }
        }
    }
    //========== String Treatment ==========
    private void UpdateString(int configurationIndex)
    {
        PanelConfiguration config = panelConfigurations[configurationIndex];

        //If the type of the variable is float
        if(config.scriptVariable.GetType() == typeof(string))
        {
            //check if the text isn't null
            if(config.text != null)
            {
                config.text.text = config.objectName + ": " + config.scriptVariable.ToString();
            }
        }
    }

    //========== Transform Treatment ==========
    private void UpdateTransform(int configurationIndex)
    {
        PanelConfiguration config = panelConfigurations[configurationIndex];

        //If the type of the variable is float
        if(config.scriptVariable.GetType() == typeof(Transform))
        {
            //check if the text isn't null
            if (config.text != null)
            {
                //Position
                if (config.position)
                {
                    //change the panel text to the variable value text
                    config.text.text = config.objectName + ": " + ((Transform)config.scriptVariable).position.ToString();

                }

                //Rotation
                if (config.rotation)
                {
                    //change the panel text to the variable value text
                    config.text.text = config.objectName + ": " + ((Transform)config.scriptVariable).rotation.ToString();
                }
                
                //Scale
                if (config.scale)
                {
                    //change the panel text to the variable value text
                    config.text.text = config.objectName + ": " + ((Transform)config.scriptVariable).localScale.ToString();
                }
                               
            }
            else
            {
                print("Text is null");
            }
        }

    }
    #endregion
    #endregion
}
