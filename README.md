# DebugManager
Unity Custom Editor for DebugManager enhances Inspector with a user-friendly interface, enabling dynamic debug panel configuration and variable selection


**This repository is only to showcase the code. To use this tool properly, download the Unity Package file and import it into your project, or access the Unity Asset Store:https://assetstore.unity.com/packages/tools/gui/debug-manager-ui-tool-275743.**

**Instructions**:
- The "Demo" folder serves as an example and is not necessary for regular
use (recommended for initial testing).
- If you intend to use Gizmo for debugging, ensure that the Gizmos option
is activated in Unity's Game window.
- The Debug Manager updates monitored variables only when selected during
play mode.
- When the collision option is activated in the Gizmo menu, the Debug
Manager will create a temporary game object with a collision system upon
entering play mode.

**Setup**:
- If you already have a Canvas, enter the folder name in the "Ui folder
name" field; otherwise, leave it empty.
- To add a new panel, click the "Add" button. To remove a panel, click
"Remove". For removing the last created panel, click "Remove last".
Alternatively, remove all panels by clicking "Clear".
- Assign a script for monitoring by entering the script in the designated
field and selecting the variable type. Then choose the specific variable
of that type.
- Colors indicate the true or false condition for booleans and intervals.
Standard floats and integers are always represented by the primary color.
- Gizmos created with collision will always be configured as a trigger.
**Update Panels**:
- To update an existing panel, simply fill in the desired fields without
the need to delete and recreate the panel.
- Remember to keep the Debug Manager selected while in play mode.
