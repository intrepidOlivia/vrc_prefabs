using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using VRCSDK2;

public class TScreen {

	public readonly int index;
	public string text;
	public string id;
	public List<SOption> options = new List<SOption>();
	public GameObject gameScreen;
	public GameObject[] gameOptions;

	TerminalData terminal;

	static readonly string SAVE_PATH = TerminalData.SAVE_PATH;
	static string SCREEN_OPTIONS_POSTFIX = "_options.txt";
	static readonly string SCREENS_PARENT = "Screens";

	string OptionListPath;

	public TScreen(TerminalData t, int index) {
		this.index = index;
		this.terminal = t;
	}

	public TScreen(TerminalData t, string text, int index) {
		this.text = text;
		this.index = index;
		this.terminal = t;
	}

	public TScreen(TerminalData t, string text, int index, string id) {
		this.text = text;
		this.index = index;
		this.id = id;
		this.terminal = t;
	}

	public void buildGameObjects(string screenPath, string optionPath){
		Transform parent = terminal.terminalObject.gameObject.transform.Find (SCREENS_PARENT);

		if (parent == null) {
			throw new MissingReferenceException ("Unable to locate " + SCREENS_PARENT + " inside of terminal object. Aborting.");
		}

		GameObject s = (GameObject)AssetDatabase.LoadAssetAtPath (screenPath, typeof(GameObject));
		if (s == null) {
			Debug.Log("No game object found at path " + screenPath + ". Aborting.");
			return;
		}
		GameObject o = (GameObject)AssetDatabase.LoadAssetAtPath(optionPath, typeof(GameObject));
		if (o == null) {
			Debug.Log ("No game object found at path " + optionPath + ". Aborting.");
		}

		// Delete any previous game object
		Transform prev = parent.Find(this.id);
		if (prev) {
			Object.DestroyImmediate (prev.gameObject);
		}

		// Instantiate screen object
		GameObject screen = Object.Instantiate (s, parent);
		this.gameScreen = screen;
		screen.name = this.id;

		// Set text property
		Text text = getTextComponent (screen.transform);
		text.text = this.text;

		// Generate Options objects
		float offset = o.GetComponent<MeshRenderer>().bounds.size.y;
		int pos = options.Count - 1;
		for (int i = 0; i < options.Count; i++) {
			GameObject optObj = Object.Instantiate (o, screen.transform);
			optObj.name = "option " + i;

			// Adjust location if needed
			optObj.transform.position = new Vector3(optObj.transform.position.x, optObj.transform.position.y + (pos-- * offset), optObj.transform.position.z);

			//Set text property
			Text optText = getTextComponent (optObj.transform);
			optText.text = options[i].text;

			// Create collider
			MeshCollider collider = optObj.AddComponent<MeshCollider>();
			collider.convex = true;
			collider.isTrigger = true;

			options [i].optionObj = optObj;
		}
	}

	public void setupTriggers(List<TScreen> screens) {
		foreach (SOption o in options) {
			if (!o.optionObj) {
				throw new MissingReferenceException ("Could not set up triggers; Game Objects for screen " + id + " have not been generated.");
			}

			VRC_EventHandler evh = o.optionObj.GetComponent<VRC_EventHandler>();
			if (!evh) {
				o.optionObj.AddComponent<VRC_EventHandler>();
			}

			// Remove any pre-existing triggers
			VRC_Trigger t = o.optionObj.GetComponent<VRC_Trigger>();
			if (t) {
				Object.DestroyImmediate (t);
			}

			// Add an event handler and trigger

			VRC_Trigger trigger = o.optionObj.AddComponent<VRC_Trigger> ();
			trigger.interactText = "Select";
			trigger.proximity = 1;

			VRC_Trigger.TriggerEvent onInteract = new VRC_Trigger.TriggerEvent();
			onInteract.TriggerType = VRC_Trigger.TriggerType.OnInteract;
			onInteract.BroadcastType = VRC_EventHandler.VrcBroadcastType.AlwaysBufferOne;

			VRC_EventHandler.VrcEvent enableDestination = new VRC_EventHandler.VrcEvent ();
			VRC_EventHandler.VrcEvent disableThisScreen = new VRC_EventHandler.VrcEvent ();

			enableDestination.EventType = VRC_EventHandler.VrcEventType.SetGameObjectActive;
			disableThisScreen.EventType = VRC_EventHandler.VrcEventType.SetGameObjectActive;

			enableDestination.ParameterBoolOp = VRC_EventHandler.VrcBooleanOp.True;
			disableThisScreen.ParameterBoolOp = VRC_EventHandler.VrcBooleanOp.False;

			enableDestination.ParameterObjects = new GameObject[1];
			disableThisScreen.ParameterObjects = new GameObject[1];

			enableDestination.ParameterObjects.SetValue(screens [o.destination].gameScreen, 0);
			if (o.screen == null) {
				throw new MissingReferenceException ("option has no screen");
			}
			if (o.screen.gameScreen == null) {
				throw new MissingReferenceException ("Screen has no game screen");
			}
			if (disableThisScreen.ParameterObjects == null ) {
				throw new MissingReferenceException ("No paraemter objects found");
			}
			disableThisScreen.ParameterObjects.SetValue(o.screen.gameScreen, 0);

			onInteract.Events.Add (enableDestination);
			onInteract.Events.Add (disableThisScreen);

			trigger.Triggers.Add (onInteract);
		}

	}

	public string getOptionsFileLine() {
		string line = "";
		foreach (SOption o in options) {
			line += o.text + "|" + o.destination + "|";
		}
		return line;
	}

	Text getTextComponent(Transform parent) {
		Transform c = parent.transform.Find ("Canvas");
		if (c == null) {
			Debug.Log ("Could not find a Canvas that was a child of " + parent.gameObject.name);
			throw new MissingReferenceException ();
		}

		Transform t = c.Find ("Text");
		if (t == null) {
			Debug.Log ("Could not find a Text object that was a child of " + parent.gameObject.name + "'s Canvas.");
			throw new MissingReferenceException ();
		}

		return t.GetComponent<Text> ();
	}
}
