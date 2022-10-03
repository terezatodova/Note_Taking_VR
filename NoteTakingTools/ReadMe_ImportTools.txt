How to import note-taking tools into a scene in EDive

All prefabs of the note-taking tools are stored in the "FinalPrefabs" folder. 

Drag and drop all of them info an existing scene. 
The "Tool Manager" is needed always, even if only some of thew tools are used. 
Before use, the Tool Manager needs to be set-up with references to the controllers, headset.
Drag and drop these references from the Controls Manager - headset.
On the child objects of the Tool Manager, drag and drop the reference for each controller
to view their descriptions in game. 

The input actions for opening, closing a menu (defaultly secondary button press and release) and for starting and ending 
the drawing/recording (defaultly trigger press and release) can also be changed through the editor.

Drag and drop the rest of the prefabs (if all) - 3dDrawingBundle, StickyNoteBundle and VoiceRecordingBundle. 
All of them contain a manager component, which requires a reference to the Tool Manager. 
Also, the tool manager requires a reference to these components.

Last but not least, if only selected tools want to be used, simply edit the ToolManager script, ActivateMenuChange function, 
which manages what tools can be called.