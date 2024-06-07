using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using static DebugManager;
using UnityEditor.Experimental.GraphView;
using Unity.VisualScripting.FullSerializer;

#if UNITY_EDITOR
[CustomEditor(typeof(DebugManager))]
public class DebugEditor : Editor
{
    //Debug manager reference
    public DebugManager debugManager;

    //logo
    private Texture2D logoTexture;

    //Script Properties Variables
    private SerializedProperty scriptProperty;
    private List<string> scriptVariable = new List<string>();
    private List<int> selectedVariableIndex;

    //Show each panel
    private List<bool> isShowing;

    //Update real time the panels
    private void OnEnable()
    {
        debugManager = (DebugManager)target;

        //Get the properties from the user's script
        scriptProperty = serializedObject.FindProperty("script");
        EditorApplication.update += OnUpdate;

        //Logo
        logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/DebugManager/Icons/Icon.png");

        //Create a list to show each panel configuration
        isShowing = new List<bool>();

        //Create the variable index list
        selectedVariableIndex = new List<int>(debugManager.panelConfigurations.Count);

        // VariableIndex list
        for (int i = 0; i < debugManager.panelConfigurations.Count; i++)
        {
            //add a visible panel
            isShowing.Add(true);

            //add the selected index
            selectedVariableIndex.Add(debugManager.panelConfigurations[i].variableIndex);

        }
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnUpdate;
    }

    private void OnUpdate()
    {
        Repaint();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    //Main
    public override void OnInspectorGUI()
    {
        //Update the Instance object
        serializedObject.Update();

        //Basic Inspector
        base.OnInspectorGUI();

        debugManager = (DebugManager)target;

        //========== Logo ==========
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(logoTexture, GUILayout.MaxHeight(128), GUILayout.ExpandWidth(true));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        //Canvas Object Name
        EditorGUILayout.LabelField("UI folder name: ", EditorStyles.boldLabel);
        debugManager.canvasName = EditorGUILayout.TextField("Object name: ", debugManager.canvasName);

        //Gap
        EditorGUILayout.Space(10);

        if (Application.isPlaying)
        {
            for (int i = 0; i < isShowing.Count; i++)
            {
                isShowing[i] = true;
            }
        }
        //========== Inspector Setup ==========
        for (int i = 0; i < debugManager.panelConfigurations.Count; i++)
        {
            if (i >= 0 && i <= debugManager.panelConfigurations.Count)
            {
                //Start the arrow drop
                isShowing[i] = EditorGUILayout.BeginFoldoutHeaderGroup(isShowing[i], $"Debug {i}");

                if (isShowing[i])
                {
                    //Start box
                    EditorGUILayout.BeginVertical("box");

                    //ToolBar
                    string[] tabText = { "Panel", "Gizmo" };
                    debugManager.panelConfigurations[i].currentTab = GUILayout.Toolbar(debugManager.panelConfigurations[i].currentTab, tabText);

                    //If panel is selected
                    if (debugManager.panelConfigurations[i].currentTab == 0)
                    {
                        Panel(debugManager, i);
                    }

                    //if gizmo is selected
                    if (debugManager.panelConfigurations[i].currentTab == 1)
                    {
                        Lines(debugManager, i);
                    }

                    //Gap
                    EditorGUILayout.Space(10);

                    //End box
                    EditorGUILayout.EndVertical();
                }
            }

            //End the arrow drop
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        //========== Button Field ==========

        //------ Add Panels ------
        AddButton(debugManager);

        //------ Remove the last Panel ------
        RemoveLastButton(debugManager);

        //------ Clear all panels ------
        ClearButton(debugManager);

        //Create Panels
        EditorGUILayout.Space(10);

        //Check if there is a least one panel
        bool showCreateButton = debugManager.panelConfigurations.Any(config => config.currentTab != 1);

        //if true
        if (showCreateButton)
        {
            CreateButton(debugManager);
        }


        //Update the instance
        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(target);

    }

    //========== Methods ==========

    //Panel setup
    private void Panel(DebugManager debugManager, int i)
    {
        //Gap
        EditorGUILayout.Space(10);

        //Start horizontal gap
        EditorGUI.indentLevel++;

        //Object Name
        EditorGUILayout.LabelField("Game Object", EditorStyles.boldLabel);
        debugManager.panelConfigurations[i].objectName = EditorGUILayout.TextField("Panel name: ", debugManager.panelConfigurations[i].objectName);
        debugManager.panelConfigurations[i].panelDimension = EditorGUILayout.Vector2Field("Panel Dimensions: ", debugManager.panelConfigurations[i].panelDimension);
        EditorGUILayout.Space(5);

        //Text Color
        EditorGUILayout.LabelField("Text", EditorStyles.boldLabel);
        debugManager.panelConfigurations[i].textColor = EditorGUILayout.ColorField("Text Color: ", debugManager.panelConfigurations[i].textColor);
        debugManager.panelConfigurations[i].fontSize = EditorGUILayout.FloatField("Font Size: ", debugManager.panelConfigurations[i].fontSize);
        EditorGUILayout.Space(5);

        //Script assign
        EditorGUILayout.LabelField("Script", EditorStyles.boldLabel);
        debugManager.panelConfigurations[i].script = (MonoBehaviour)EditorGUILayout.ObjectField("Script", debugManager.panelConfigurations[i].script, typeof(MonoBehaviour), true);

        //Type of variable
        if (debugManager.panelConfigurations[i].script != null)
        {
            //Select the type of variable field
            debugManager.panelConfigurations[i].variable = (DebugManager.PanelConfiguration.Variable)EditorGUILayout.EnumPopup("Variable Type", debugManager.panelConfigurations[i].variable);

            //Method that saves the type of the variable
            SaveTypeVariable(debugManager, i, true, true);

            //Variable
            DisplayScriptVariables(debugManager.panelConfigurations[i].script, debugManager.panelConfigurations[i].variable, out debugManager.panelConfigurations[i].scriptVariable, i);

            //Color
            EditorGUILayout.Space(5);
            if (debugManager.panelConfigurations[i].plotGraph == false)
            {
                EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
                debugManager.panelConfigurations[i].primaryColor = EditorGUILayout.ColorField("Primary Color(true): ", debugManager.panelConfigurations[i].primaryColor);
                debugManager.panelConfigurations[i].secondaryColor = EditorGUILayout.ColorField("Secondary Color(false): ", debugManager.panelConfigurations[i].secondaryColor);
            }
        }
        //End horizontal gap
        EditorGUI.indentLevel--;

        //for the last panel
        if (i != debugManager.panelConfigurations.Count - 1)
        {
            //Remove this panel
            RemoveButton(debugManager, i);
        }
    }

    //Line Gizmo setup
    private void Lines(DebugManager debugManager, int i)
    {
        //Gap
        EditorGUILayout.Space(10);
        //Start horizontal gap
        EditorGUI.indentLevel++;

        //========== Gizmo Type ==========

        //Select the type of gizmo title
        debugManager.panelConfigurations[i].gizmoType = (DebugManager.PanelConfiguration.GizmoType)EditorGUILayout.EnumPopup("Gizmo Type", debugManager.panelConfigurations[i].gizmoType);
        //save the selected type
        SaveTypeGizmo(debugManager, i);

        //---------- Parameters ----------      
        //If is a cube or a sphere
        if (!debugManager.panelConfigurations[i].isLine)
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Wire", EditorStyles.boldLabel);

            //create a check box to set if the object is a wire
            debugManager.panelConfigurations[i].isWire = EditorGUILayout.Toggle("Set the object to be wired", debugManager.panelConfigurations[i].isWire);
        }

        //Gap
        EditorGUILayout.Space(5);

        //Title
        EditorGUILayout.LabelField("Length", EditorStyles.boldLabel);

        //Distance(length) option
        debugManager.panelConfigurations[i].isScriptDistance = EditorGUILayout.Toggle("Use variable", debugManager.panelConfigurations[i].isScriptDistance);

        //Check if the user want's to use a variable instead of a raw number
        if (debugManager.panelConfigurations[i].isScriptDistance)
        {
            //gap
            EditorGUILayout.Space(5);

            //Show script field
            EditorGUILayout.LabelField("Script", EditorStyles.boldLabel);
            debugManager.panelConfigurations[i].script = (MonoBehaviour)EditorGUILayout.ObjectField("Script", debugManager.panelConfigurations[i].script, typeof(MonoBehaviour), true);

            //check if the script field isn't null
            if (debugManager.panelConfigurations[i].script != null)
            {
                //Select the type of variable field
                debugManager.panelConfigurations[i].variable = DebugManager.PanelConfiguration.Variable.floats;
                debugManager.panelConfigurations[i].floats = true;

                //Method that saves the type of the variable
                SaveTypeVariable(debugManager, i, false, false);

                //Method that shows the variables avaliables
                DisplayScriptVariables(debugManager.panelConfigurations[i].script, debugManager.panelConfigurations[i].variable, out debugManager.panelConfigurations[i].scriptVariable, i);

                debugManager.panelConfigurations[i].gizmoScale = (float)debugManager.panelConfigurations[i].scriptVariable;

            }
        }
        else
        {
            //if is a line
            if (debugManager.panelConfigurations[i].isLine)
            {
                //create a float field
                debugManager.panelConfigurations[i].gizmoScale = EditorGUILayout.FloatField("Scale: ", debugManager.panelConfigurations[i].gizmoScale);
            }
            else
            {
                //create a slider
                debugManager.panelConfigurations[i].gizmoScale = EditorGUILayout.Slider("Scale: ", debugManager.panelConfigurations[i].gizmoScale, 0.1f, 100);
            }

        }

        //if is a line
        if (debugManager.panelConfigurations[i].isLine)
        {
            //Show the amount of lines field        
            debugManager.panelConfigurations[i].lineAmount = EditorGUILayout.IntSlider("Amount: ", debugManager.panelConfigurations[i].lineAmount, 1, 100);
        }



        //space gap
        EditorGUILayout.Space(5);

        //---------- Transform ----------
        EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
        //Start point of the line
        debugManager.panelConfigurations[i].origin = (Transform)EditorGUILayout.ObjectField("Initial Location: ", debugManager.panelConfigurations[i].origin, typeof(Transform), true);

        //If is a line
        if (debugManager.panelConfigurations[i].isLine)
        {
            //Direction to which the line will be point
            debugManager.panelConfigurations[i].direction = (Transform)EditorGUILayout.ObjectField("Direction: ", debugManager.panelConfigurations[i].direction, typeof(Transform), true);
        }

        //Offset to adjust the line
        debugManager.panelConfigurations[i].offSet = EditorGUILayout.Vector3Field("Offset: ", debugManager.panelConfigurations[i].offSet);

        EditorGUILayout.Space(5);

        //---------- Style ----------
        EditorGUILayout.LabelField("Style", EditorStyles.boldLabel);
        //Line color
        debugManager.panelConfigurations[i].gizmoCurrentColor = EditorGUILayout.ColorField("Color: ", debugManager.panelConfigurations[i].gizmoCurrentColor);

        //========== Collision ==========
        CollisionGizmo(debugManager, i);

        //end horizontal gap
        EditorGUI.indentLevel--;

        //for the last panel
        if (i != debugManager.panelConfigurations.Count - 1)
        {
            //Remove this panel
            RemoveButton(debugManager, i);
        }
    }


    //Collision method
    private void CollisionGizmo(DebugManager debugManager, int i)
    {
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Collision", EditorStyles.boldLabel);

        debugManager.panelConfigurations[i].isCollision = EditorGUILayout.Toggle("Use collision", debugManager.panelConfigurations[i].isCollision);

        //Collider
        if (debugManager.panelConfigurations[i].isCollision)
        {
            //Save the object name
            debugManager.panelConfigurations[i].objectName = EditorGUILayout.TextField("Collider name", debugManager.panelConfigurations[i].objectName);

            EditorGUILayout.Space(5);

            //Style
            EditorGUILayout.LabelField("Style", EditorStyles.boldLabel);

            //Primary color
            debugManager.panelConfigurations[i].gizmoPrimaryColor = EditorGUILayout.ColorField("True color", debugManager.panelConfigurations[i].gizmoPrimaryColor);

            //Secondary color
            debugManager.panelConfigurations[i].gizmoSecondaryColor = EditorGUILayout.ColorField("False color", debugManager.panelConfigurations[i].gizmoSecondaryColor);

        }
    }

    //------ Buttons ------
    //Add
    private void AddButton(DebugManager debugManager)
    {
        //Add Item
        if (GUILayout.Button("Add"))
        {
            //Create a hollow pane
            PanelConfiguration newPanel = new PanelConfiguration();

            //add the hollow panel to the list
            debugManager.panelConfigurations.Add(newPanel);

            isShowing.Add(true);
            selectedVariableIndex.Add(0);
        }
    }
    //Remove
    public void RemoveButton(DebugManager debugManager, int i)
    {
        //Optimization
        PanelConfiguration config = debugManager.panelConfigurations[i];

        //Remove Item
        if (debugManager.panelConfigurations.Count > 0)
        {
            if (GUILayout.Button("Remove", GUILayout.Width(100)))
            {
                //Variables
                string objectToRemove;
                GameObject toDestroy;

                //if it is a panel
                if (config.currentTab == 0)
                {
                    //Removing the game object deleted panel 
                    objectToRemove = config.objectName + " Panel";

                    toDestroy = GameObject.Find(objectToRemove);

                    if (toDestroy != null)
                    {
                        DestroyImmediate(toDestroy);
                    }
                }
                //if is a gizmo
                else
                {
                    //Removing the game object collider
                    objectToRemove = config.objectName + " Collider";

                    toDestroy = GameObject.Find(objectToRemove);

                    if (toDestroy != null)
                    {
                        DestroyImmediate(toDestroy);
                    }
                }

                //Removing the last panel
                debugManager.panelConfigurations.Remove(config);

                isShowing.Remove(isShowing[i]);
                selectedVariableIndex.Remove(selectedVariableIndex[i]);
            }
        }
    }
    //Remove last
    private void RemoveLastButton(DebugManager debugManager)
    {
        //Remove Item
        if (debugManager.panelConfigurations.Count > 0)
        {
            if (GUILayout.Button("Remove last"))
            {
                //Variables
                string objectToRemove;
                GameObject toDestroy;

                //Optimization
                PanelConfiguration config = debugManager.panelConfigurations[debugManager.panelConfigurations.Count - 1];

                if (config.currentTab == 0)
                {
                    //Removing the game object deleted panel 
                    objectToRemove = config.objectName + " Panel";

                    toDestroy = GameObject.Find(objectToRemove);

                    if (toDestroy != null)
                    {
                        DestroyImmediate(toDestroy);
                    }
                }
                else
                {
                    //Removing the game object collider
                    objectToRemove = config.objectName + " Collider";

                    toDestroy = GameObject.Find(objectToRemove);

                    if (toDestroy != null)
                    {
                        DestroyImmediate(toDestroy);
                    }
                }

                //Removing the last panel
                debugManager.panelConfigurations.RemoveAt(debugManager.panelConfigurations.Count - 1);

                isShowing.RemoveAt(isShowing.Count - 1);
                selectedVariableIndex.RemoveAt(selectedVariableIndex.Count - 1);
            }
        }
    }
    //Clear all
    private void ClearButton(DebugManager debugManager)
    {
        if (debugManager.panelConfigurations.Count > 1)
        {
            if (GUILayout.Button("Clear all"))
            {
                for (int i = 0; i < debugManager.panelConfigurations.Count; i++)
                {
                    //Removing the game object deleted panel 
                    string objectToRemove = debugManager.panelConfigurations[i].objectName + " Panel";

                    GameObject toDestroy = GameObject.Find(objectToRemove);

                    if (toDestroy != null)
                    {
                        DestroyImmediate(toDestroy);
                    }
                }

                debugManager.panelConfigurations.Clear();
                isShowing.Clear();
                selectedVariableIndex.Clear();
            }
        }

    }
    //Create
    private void CreateButton(DebugManager debugManager)
    {
        //show the button create
        if (GUILayout.Button("Create"))
        {
            for (int i = 0; i < debugManager.panelConfigurations.Count; i++)
            {
                PanelConfiguration config = debugManager.panelConfigurations[i];

                GameObject copyObject = GameObject.Find(config.objectName + " Panel");

                if (copyObject != null && copyObject.name == config.objectName + " Panel")
                {
                    Debug.LogWarning("The Object: " + copyObject.name + " Already exist");
                    continue;
                }

                //if the current tab is a panel
                if (config.currentTab == 0)
                {
                    debugManager.CanvasSetup(i);
                }

            }
        }
    }
    //Save the selected variable type
    private void SaveTypeVariable(DebugManager debugManager, int configurationIndex, bool interval, bool plotGraph)
    {
        PanelConfiguration debugPanel = debugManager.panelConfigurations[configurationIndex];

        if (debugPanel.script != null)
        {
            //Save the current type
            switch (debugPanel.variable)
            {
                case DebugManager.PanelConfiguration.Variable.integer:
                    debugPanel.integer = true;
                    debugPanel.floats = false;
                    debugPanel.boolean = false;
                    debugPanel.strings = false;
                    debugPanel.transform = false;

                    break;
                case DebugManager.PanelConfiguration.Variable.floats:
                    debugPanel.integer = false;
                    debugPanel.floats = true;
                    debugPanel.boolean = false;
                    debugPanel.strings = false;
                    debugPanel.transform = false;

                    break;
                case DebugManager.PanelConfiguration.Variable.boolean:
                    debugPanel.integer = false;
                    debugPanel.floats = false;
                    debugPanel.boolean = true;
                    debugPanel.strings = false;
                    debugPanel.transform = false;
                    break;
                case DebugManager.PanelConfiguration.Variable.strings:
                    debugPanel.integer = false;
                    debugPanel.floats = false;
                    debugPanel.boolean = false;
                    debugPanel.strings = true;
                    debugPanel.transform = false;
                    break;
                case DebugManager.PanelConfiguration.Variable.transform:
                    debugPanel.integer = false;
                    debugPanel.floats = false;
                    debugPanel.boolean = false;
                    debugPanel.transform = true;
                    break;
                default:
                    break;
            }
        }
        else
        {
            debugPanel.integer = false;
            debugPanel.floats = false;
            debugPanel.boolean = false;
            debugPanel.strings = false;
            debugPanel.transform = false;
        }

        if ((debugPanel.floats || debugPanel.integer))
        {

            //if the user wants to set an interval
            if (interval)
            {
                //Interval Method
                ShowInterval(debugManager, configurationIndex);
            }

            if (plotGraph)
            {
                PlotGraph(debugManager, configurationIndex);
            }
        }

        if (debugPanel.transform)
        {
            TransformCheckBox(debugManager, configurationIndex);
        }

    }
    //Save the selected type of gizmo
    private void SaveTypeGizmo(DebugManager debugManager, int configurationIndex)
    {
        PanelConfiguration debugPanel = debugManager.panelConfigurations[configurationIndex];

        //Save the current type
        switch (debugPanel.gizmoType)
        {
            case DebugManager.PanelConfiguration.GizmoType.line:
                debugPanel.isLine = true;
                debugPanel.isCube = false;
                debugPanel.isSphere = false;
                break;
            case DebugManager.PanelConfiguration.GizmoType.cube:
                debugPanel.isLine = false;
                debugPanel.isCube = true;
                debugPanel.isSphere = false;
                break;
            case DebugManager.PanelConfiguration.GizmoType.sphere:
                debugPanel.isLine = false;
                debugPanel.isCube = false;
                debugPanel.isSphere = true;
                break;
            default:
                debugPanel.isLine = false;
                debugPanel.isCube = false;
                debugPanel.isSphere = false;
                break;
        }

    }
    //Plot Graph setup
    private void PlotGraph(DebugManager debugManager, int index)
    {
        // Optimization
        PanelConfiguration debugPanel = debugManager.panelConfigurations[index];

        debugPanel.plotGraph = EditorGUILayout.Toggle("Plot Graph", debugPanel.plotGraph);

        // If the plot graph is visible
        if (debugPanel.plotGraph)
        {
            // Title
            EditorGUILayout.LabelField("Graph Setup", EditorStyles.boldLabel);

            // Start indent level
            EditorGUI.indentLevel++;

            //Graph here...
            debugPanel.graphScale = EditorGUILayout.Slider("Graph Scale %", debugPanel.graphScale, 1, 1000);
            DrawGraph(debugPanel.xValues, debugPanel.yValues, debugPanel);

            // ========== Graph time interval ==========
            debugPanel.graphCustomInterval = EditorGUILayout.Toggle("Custom Interval", debugPanel.graphCustomInterval);

            // Check if the user wants to use a custom interval
            if (debugPanel.graphCustomInterval)
            {
                // Custom interval
                debugPanel.graphInterval = EditorGUILayout.IntSlider("Graph update interval: ", (int)debugPanel.graphInterval, 1, 25);
            }
            else
            {
                //debugPanel.graphInterval = 1/60;
                // Warning message
                EditorGUILayout.HelpBox("If 'custom interval' is false, the update interval will be set by the frame rate.", MessageType.Warning);
            }

            // Color
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
            debugPanel.primaryColor = EditorGUILayout.ColorField("Line color: ", debugManager.panelConfigurations[index].primaryColor);
            debugPanel.secondaryColor = EditorGUILayout.ColorField("Background color: ", debugManager.panelConfigurations[index].secondaryColor);

            // End indent level
            EditorGUI.indentLevel--;
        }
    }

    private void DrawGraph(List<float> xValues, List<float> yValues, PanelConfiguration config)
    {
        int GraphWidth = (int)(5 * config.graphScale);//Length of the math function
        const int GraphHeight = 500;//Height of the math function
        int Padding = 20;

        GUILayout.Label("Graph", EditorStyles.boldLabel);

        // Creating the rectangle to draw the graph
        Rect graphRect = GUILayoutUtility.GetRect(GraphWidth, GraphHeight);
        graphRect = EditorGUI.IndentedRect(graphRect);

        // origin of y axle
        float origin = graphRect.yMax - graphRect.height / 2;

        // x axle:
        Handles.DrawLine(new Vector3(graphRect.x, origin), new Vector3(graphRect.xMax, origin));//Line draw
        EditorGUI.LabelField(new Rect(EditorGUIUtility.currentViewWidth - 85, (origin - Padding), 100, 20), "Seconds");//title

        int xInterval = 50;

        // creating the style marking
        GUIStyle measureStyle = new GUIStyle(GUI.skin.label);
        measureStyle.fontSize = 8; // font size

        // for each space in view width
        for (int i = 0; i <= (int)EditorGUIUtility.currentViewWidth; i++)
        {
            // calculate the position of the x marks
            if (i % xInterval == 0 && i > 0)
            {
                // Drawing the lines
                Handles.DrawLine(new Vector3(i, origin - 5), new Vector3(i, origin + 5));//X lines

                // drawing the marks
                EditorGUI.LabelField(new Rect(i, origin, 100, 20), string.Format("{0:F1}s", i), measureStyle);
            }
        }

        // Drawing the Y axle
        Handles.DrawLine(new Vector3(graphRect.x, graphRect.yMax), new Vector3(graphRect.x, graphRect.y));

        // Graph Draw
        Handles.color = Color.blue;
        int numPoints = Mathf.Min(xValues.Count, yValues.Count); //make sure the smaller number doesn't exceed the larger one

        Vector3[] points = new Vector3[numPoints];

        float speed = 100;

        for (int i = 0; i < numPoints; i++)
        {
            float variableScale = config.graphScale / ((float)config.scriptVariable + 1);
            float x = graphRect.x + xValues[i];
            float y = origin - (yValues[i] * (config.graphScale * variableScale / speed));

            points[i] = new Vector3(x, y);
        }
        //Maximum treatment here...
        Handles.DrawAAPolyLine(points);
    }

    //Method that shows a interval checkbox
    private void ShowInterval(DebugManager debugManager, int configurationIndex)
    {
        PanelConfiguration debugPanel = debugManager.panelConfigurations[configurationIndex];

        //========= Interval of the float value =========
        //Show the interval checkbox if the script variable is a float
        //Create the toggle box
        debugPanel.interval = EditorGUILayout.Toggle("Interval", debugPanel.interval);

        //set the min and max values of the interval
        if (debugPanel.interval)
        {
            //Save the min and max numbers
            EditorGUILayout.BeginHorizontal();

            //If it's a float, save the minimum and maximum float values
            if (debugPanel.floats)
            {
                debugPanel.minNumber = EditorGUILayout.FloatField("Min Value", debugPanel.minNumber);
                debugPanel.maxNumber = EditorGUILayout.FloatField("Max Value", debugPanel.maxNumber);
            }
            //else, save the minimum and maximum int values
            else
            {
                debugPanel.minNumber = EditorGUILayout.IntField("Min Value", (int)debugPanel.minNumber);
                debugPanel.maxNumber = EditorGUILayout.IntField("Max Value", (int)debugPanel.maxNumber);
            }

            //----- Range treatment -----
            //if the minimum is different from the maximum
            if (debugPanel.minNumber != debugPanel.maxNumber)
            {
                //if the minimum is bigger than maximum
                if (debugPanel.minNumber > debugPanel.maxNumber)
                {
                    //minimum is equal to maximum
                    debugPanel.minNumber = debugPanel.maxNumber;
                }

                //if the maximum is smaller than minimum
                if (debugPanel.maxNumber < debugPanel.minNumber)
                {
                    //maximum is equal to minimum
                    debugPanel.maxNumber = debugPanel.minNumber;
                }
            }

            EditorGUILayout.EndHorizontal();

            //Create a reset button
            if (GUILayout.Button("Reset"))
            {
                debugPanel.minNumber = 0;
                debugPanel.maxNumber = 1;
            }
        }
    }
    //create the transform treatment
    private void TransformCheckBox(DebugManager debugManager, int configurationIndex)
    {
        DebugManager.PanelConfiguration debugPanel = debugManager.panelConfigurations[configurationIndex];

        //Add gap
        EditorGUI.indentLevel++;

        //Creating the group
        //Add gap
        EditorGUI.indentLevel++;
        //Create the checkbox for the transform options
        //Position
        debugPanel.position = EditorGUILayout.Toggle("Position", debugPanel.position);
        //Rotation
        debugPanel.rotation = EditorGUILayout.Toggle("Rotation", debugPanel.rotation);
        //Scale
        debugPanel.scale = EditorGUILayout.Toggle("Scale", debugPanel.scale);

        //Booleans treatment
        if (debugPanel.position)
        {
            debugPanel.position = true;
            debugPanel.rotation = false;
            debugPanel.scale = false;

        }
        else if (debugPanel.rotation)
        {
            debugPanel.position = false;
            debugPanel.rotation = true;
            debugPanel.scale = false;
        }
        else if (debugPanel.scale)
        {
            debugPanel.position = false;
            debugPanel.rotation = false;
            debugPanel.scale = true;
        }
        else
        {
            debugPanel.position = false;
            debugPanel.rotation = false;
            debugPanel.scale = false;
        }

        //Remove gap
        EditorGUI.indentLevel--;
    }

    #region DisplayScriptVariables
    //Method which display the variables of the user's script depending of the type
    private void DisplayScriptVariables(MonoBehaviour script, DebugManager.PanelConfiguration.Variable variableType, out object selectedVariableValue, int i)
    {
        selectedVariableValue = null;

        // if the script is null
        if (script == null)
        {
            //exit
            return;
        }

        //Get the variable properties
        System.Type scriptType = script.GetType();
        var fields = scriptType.GetFields();
        var properties = scriptType.GetProperties();

        // Create a list to store the variables
        List<string> variableNames = new List<string>();

        //Adding the variables at the list
        foreach (var field in fields)
        {
            if (IsVariableOfType(field, variableType))
            {
                variableNames.Add(field.Name);
            }
        }

        // if there's at least one variable, create a popup field
        if (variableNames.Count > 0)
        {
            // Check if the variable is in a valid interval-
            selectedVariableIndex[i] = Mathf.Clamp(selectedVariableIndex[i], 0, variableNames.Count - 1);

            GUILayout.Space(15);

            // Show the popup with the available options
            selectedVariableIndex[i] = EditorGUILayout.Popup("Select Variable", selectedVariableIndex[i], variableNames.ToArray());

            //("Index: " + selectedVariableIndex[i]);

            // Obtain the name of the selected variable
            string selectedVariableName = variableNames[selectedVariableIndex[i]];

            // Show the value of the selected variable
            foreach (var field in fields)
            {
                if (field.Name == selectedVariableName)
                {
                    //save the variable selected
                    selectedVariableValue = field.GetValue(script);

                    //saving index in Debug manager script
                    debugManager.panelConfigurations[i].variableIndex = selectedVariableIndex[i];

                    EditorGUILayout.LabelField(selectedVariableName, field.GetValue(script).ToString());

                    break;
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("No variables available");
        }
    }
    #endregion

    //Method that check the type of the variable
    public static bool IsVariableOfType(FieldInfo field, DebugManager.PanelConfiguration.Variable variableType)
    {
        switch (variableType)
        {
            case DebugManager.PanelConfiguration.Variable.integer:
                return field.FieldType == typeof(int);
            case DebugManager.PanelConfiguration.Variable.floats:
                return field.FieldType == typeof(float);
            case DebugManager.PanelConfiguration.Variable.boolean:
                return field.FieldType == typeof(bool);
            case DebugManager.PanelConfiguration.Variable.strings:
                return field.FieldType == typeof(string);
            case DebugManager.PanelConfiguration.Variable.transform:
                return field.FieldType == typeof(Transform);
            default:
                return false;
        }
    }

}
#endif

#if UNITY_EDITOR
[InitializeOnLoad]
public class DebugManagerSelection : MonoBehaviour
{
    static DebugManagerSelection()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }


    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // Find the DebugManager in the scene
            DebugManager[] debugManagers = FindObjectsOfType<DebugManager>();

            //Update each debug manager panel
            foreach (DebugManager debugManager in debugManagers)
            {
                if (debugManager != null)
                {
                    // Select the GameObject of DebugManager
                    Selection.activeGameObject = debugManager.gameObject;

                }

                for (int i = 0; i < debugManager.panelConfigurations.Count; i++)
                {

                    PanelConfiguration config = debugManager.panelConfigurations[i];

                    config.xValues.Clear();
                    config.yValues.Clear();

                    if (config.isCollision)
                    {
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


                        debugManager.CreateGameObject(startPosition, i);
                    }

                }
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            //Variables
            DebugManager debugManager = GameObject.Find("DebugManager").GetComponent<DebugManager>();
            string objectToRemove;
            GameObject toDestroy;



            //Assign debugManager script to this game object
            if (debugManager != null)
            {
                if (debugManager.panelConfigurations.Count > 0)
                {
                    for (int i = 0; i < debugManager.panelConfigurations.Count; i++)
                    {
                        PanelConfiguration debugPanel = debugManager.panelConfigurations[i];

                        //Removing the game object collider
                        objectToRemove = debugManager.panelConfigurations[i].objectName + " Collider";

                        toDestroy = GameObject.Find(objectToRemove);

                        if (toDestroy != null)
                        {
                            DestroyImmediate(toDestroy);
                        }
                    }
                }

            }
        }
    }
}
#endif
