using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRCSDK2;

public class SlideshowCreator : EditorWindow {

	GameObject slideshow;
	string materialsPath;
	private static readonly char SLASH = Path.DirectorySeparatorChar;
	private static readonly string SLASH_STRING = SLASH.ToString();

	[MenuItem("Tools/Slideshow Creator")]
	public static void showWindow() {
		SlideshowCreator window = (SlideshowCreator)EditorWindow.GetWindow(typeof(SlideshowCreator));
		window.Show();
	}

	void OnGUI() {

		GUILayout.Label("Drag Slideshow From Scene", EditorStyles.boldLabel);
		slideshow = (GameObject)EditorGUILayout.ObjectField ("Slideshow Asset", slideshow, typeof(GameObject), true);
		GUILayout.Label ("Click to select folder of images to add to the slideshow:", EditorStyles.label);

		if (GUILayout.Button ("Select directory with images")) {
			if (slideshow == null) {
				Debug.Log ("Please select slideshow asset first.");
				return;
			}


			string path = EditorUtility.OpenFolderPanel("Load png Textures", "", "");

			// Set up relative path of texture image
			string[] pathArray = path.Split ('/');	// OpenFolderPanel always uses "/" instead of OS directory separator.
			string[] relativePathArray = new string[0];
			int j = 0;
			bool pathing = false;
			for (int i = 0; i < pathArray.Length; i++) {
				if (pathArray [i] == "Assets") {
					pathing = true;
					relativePathArray = new string[pathArray.Length - i];
				}

				if (pathing) {
					relativePathArray [j] = pathArray [i];
					j++;
				}
			}

			string imagesDirPath = string.Join (SLASH_STRING, relativePathArray);
			string[] files = Directory.GetFiles(path);

			// Create Materials folder
			materialsPath = imagesDirPath + SLASH_STRING + "Materials";
			if (!AssetDatabase.IsValidFolder (materialsPath)) {
				Debug.Log ("No Materials folder found");
				try {
					AssetDatabase.CreateFolder (imagesDirPath, "Materials");
				} catch (IOException e) {
					Debug.Log ("Exception while creating Materials Directory!");
					Debug.Log (e.Message);
					return;
				}
			}
				
			GameObject screen = slideshow.transform.Find("screen").gameObject;
			GameObject remote = slideshow.transform.Find("remote_control").gameObject;
			GameObject sharedQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);


			int n = 1;
			int imageCount = 0;
			Array.Sort (files, new AlphanumComparatorFast ());
			foreach (string file in files) {
				if (endsWithImageExt(file)) {
					imageCount++;

					// Create new material
					string[] filepathArray = file.Split(SLASH);
					string filename = filepathArray [filepathArray.Length - 1];
					Material newMaterial = new Material (Shader.Find ("Standard"));


					// Set new material's color and texture
					newMaterial.color = new Color(0, 0, 0);
					newMaterial.EnableKeyword ("_EMISSION");
					newMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
					Texture tex = (Texture)AssetDatabase.LoadAssetAtPath (imagesDirPath + SLASH_STRING + filename, typeof(Texture));
					newMaterial.SetTexture ("_EmissionMap", tex);
					newMaterial.SetColor ("_EmissionColor", Color.white);

					// Save new material to file
					AssetDatabase.CreateAsset (newMaterial, materialsPath + SLASH_STRING + filename + ".mat");

					// Create image panel for each file
					GameObject imgObj = initScreenImage("image" + n, screen, sharedQuad, newMaterial);
					if (n != 1) {
						imgObj.SetActive (false);
					}

					// Create remote triggers for each file
					GameObject forwardButton = initRemoteButton("forwardTo" + (n + 1), remote);
					if (n != 1) {
						forwardButton.SetActive (false);
					}
					GameObject backButton = initRemoteButton ("backTo" + (n - 1), remote);
					backButton.SetActive (false);

					backButton.GetComponent<BoxCollider>().center = new Vector3 (-0.0325f, -0.0247f, -0.002f);
					forwardButton.GetComponent<BoxCollider>().center = new Vector3 (0.0325f, -0.0247f, -0.002f);
					n++;
				}
			}

			n = 1;
			for (int i = 0; i < imageCount; i++) {
				// Set up triggers now that all game objects are created;
				GameObject backButton = remote.transform.Find("backTo" + (n - 1)).gameObject;
				GameObject forwardButton = remote.transform.Find ("forwardTo" + (n + 1)).gameObject;

				VRC_Trigger backTrigger = backButton.GetComponent<VRC_Trigger> ();
				VRC_Trigger forwardTrigger = forwardButton.GetComponent<VRC_Trigger> ();

				setupForwardTrigger (forwardTrigger, n, screen, remote);
				setupBackTrigger (backTrigger, n, screen, remote);

				n++;
			}

			Debug.Log ("Set up " + imageCount + " images in the slideshow.");
			DestroyImmediate (sharedQuad);
		}
	}

	void setupForwardTrigger(VRC_Trigger forwardTrigger, int n, GameObject screen, GameObject remote) {
		forwardTrigger.interactText = "Forward";

		VRC_EventHandler.VrcEvent forwardEventTrue = new VRC_EventHandler.VrcEvent ();
		VRC_EventHandler.VrcEvent forwardEventFalse = new VRC_EventHandler.VrcEvent ();
		forwardEventTrue.EventType = VRC_EventHandler.VrcEventType.SetGameObjectActive;
		forwardEventFalse.EventType = VRC_EventHandler.VrcEventType.SetGameObjectActive;
		forwardEventTrue.ParameterBoolOp = VRC_EventHandler.VrcBooleanOp.True;
		forwardEventFalse.ParameterBoolOp = VRC_EventHandler.VrcBooleanOp.False;

		int i;

		Transform nextImage = screen.transform.Find ("image" + (n + 1));
		if (nextImage == null) {
			DestroyImmediate (forwardTrigger);
			return;
		}

		Transform currentImg = screen.transform.Find("image" + (n));
		if (currentImg == null) {
			DestroyImmediate (forwardTrigger);
			return;
		}

		// Setting Game Objects True
		int trueParamCount = 3;

		Transform nextForward = remote.transform.Find ("forwardTo" + (n + 2));
		Transform next2Img = screen.transform.Find ("image" + (n + 2));
		if (next2Img == null || nextForward == null) {
			trueParamCount--;
		}

		Transform nextBack = remote.transform.Find ("backTo" + n);
		if (nextBack == null) {
			trueParamCount--;
		}

		i = 0;	
		forwardEventTrue.ParameterObjects = new GameObject [trueParamCount];
		forwardEventTrue.ParameterObjects.SetValue (nextImage.gameObject, i++);
		if (nextForward != null && next2Img != null) {
			forwardEventTrue.ParameterObjects.SetValue (nextForward.gameObject, i++);
		}
		if (nextBack != null) {
			forwardEventTrue.ParameterObjects.SetValue(nextBack.gameObject, i++);
		}

		// Setting Game Objects False
		int falseParamCount = 3;

		Transform currentBack = remote.transform.Find("backTo" + (n - 1));
		if (currentBack == null) {
			falseParamCount--;
		}

		i = 0;
		forwardEventFalse.ParameterObjects = new GameObject[falseParamCount];
		forwardEventFalse.ParameterObjects.SetValue (currentImg.gameObject, i++);
		forwardEventFalse.ParameterObjects.SetValue (forwardTrigger.gameObject, i++);
		if (currentBack != null) {
			forwardEventFalse.ParameterObjects.SetValue(currentBack.gameObject, i++);
		}

		// Add both type of actions to Trigger
		forwardTrigger.Triggers [0].Events.Add (forwardEventTrue);
		forwardTrigger.Triggers [0].Events.Add (forwardEventFalse);
	}

	void setupBackTrigger(VRC_Trigger backTrigger, int n, GameObject screen, GameObject remote) {
		if (backTrigger == null) {
			return;
		}

		backTrigger.interactText = "Back";

		int i;

		VRC_EventHandler.VrcEvent backEventTrue = new VRC_EventHandler.VrcEvent ();
		VRC_EventHandler.VrcEvent backEventFalse = new VRC_EventHandler.VrcEvent ();

		backEventTrue.EventType = VRC_EventHandler.VrcEventType.SetGameObjectActive;
		backEventFalse.EventType = VRC_EventHandler.VrcEventType.SetGameObjectActive;

		backEventTrue.ParameterBoolOp = VRC_EventHandler.VrcBooleanOp.True;
		backEventFalse.ParameterBoolOp = VRC_EventHandler.VrcBooleanOp.False;

		Transform prevImage = screen.transform.Find ("image" + (n - 1));
		if (prevImage == null) {
			DestroyImmediate(backTrigger);
			return;
		}

		Transform currentImg = screen.transform.Find ("image" + n);
		if (currentImg == null) {
			DestroyImmediate(backTrigger);
			return;
		}

		// Setting game objects true
		int trueParamCount = 3;

		Transform nextPrev = remote.transform.Find ("backTo" + (n - 2));
		Transform prev2Img = screen.transform.Find ("image" + (n - 2));
		if (prev2Img == null || nextPrev == null) {
			trueParamCount--;
		}

		Transform prevForward = remote.transform.Find ("forwardTo" + (n));
		if (prevForward == null) {
			trueParamCount--;
		}

		i = 0;
		backEventTrue.ParameterObjects = new GameObject[trueParamCount];
		backEventTrue.ParameterObjects.SetValue (prevImage.gameObject, i++);
		if (prev2Img != null && nextPrev != null) {
			backEventTrue.ParameterObjects.SetValue (nextPrev.gameObject, i++);
		}
		if (prevForward != null) {
			backEventTrue.ParameterObjects.SetValue (prevForward.gameObject, i++);
		}

		// Setting game objects false
		int falseParamCount = 3;

		Transform currentForward = remote.transform.Find ("forwardTo" + (n + 1));
		if (currentForward == null) {
			falseParamCount--;
		}

		i = 0;
		backEventFalse.ParameterObjects = new GameObject[falseParamCount];
		backEventFalse.ParameterObjects.SetValue (currentImg.gameObject, i++);
		backEventFalse.ParameterObjects.SetValue (backTrigger.gameObject, i++);
		if (currentForward != null) {
			backEventFalse.ParameterObjects.SetValue (currentForward.gameObject, i++);
		}

		// Add both types of actions to trigger
		backTrigger.Triggers[0].Events.Add(backEventTrue);
		backTrigger.Triggers [0].Events.Add (backEventFalse);
	}

	GameObject initScreenImage(string name, GameObject screen, GameObject quad, Material mat) {
		GameObject imgObj = new GameObject (name);
		imgObj.transform.parent = screen.transform;
		imgObj.transform.localPosition = new Vector3 (0.0f, -0.03f, 1.0f);
		imgObj.transform.localRotation = new Quaternion (0, 0.7071068f, 0.7071068f, 0);
		imgObj.transform.localScale = new Vector3 (2.8f, 1.8f, 0.3f);
		MeshFilter filter = imgObj.AddComponent<MeshFilter> ();
		filter.sharedMesh = quad.GetComponent<MeshFilter>().sharedMesh;
		imgObj.AddComponent<MeshCollider> ();
		MeshRenderer renderer = imgObj.AddComponent<MeshRenderer> ();
		renderer.sharedMaterial = mat;

		return imgObj;
	}

	GameObject initRemoteButton(string name, GameObject remote) {
		GameObject button = new GameObject (name);
		button.transform.parent = remote.transform;
		button.transform.localPosition = new Vector3 (0, 0, 0);
		button.transform.localRotation = new Quaternion (0, 0, 0, 0);
		button.transform.localScale = new Vector3 (1, 1, 1);
		BoxCollider collider = button.AddComponent<BoxCollider> ();
		collider.isTrigger = true;
		collider.size = new Vector3 (0.01f, 0.05f, 0.02f);

		button.AddComponent<VRC_EventHandler> ();
		VRC_Trigger trigger = button.AddComponent<VRC_Trigger> ();
		VRC_Trigger.TriggerEvent onInteract = new VRC_Trigger.TriggerEvent ();
		onInteract.TriggerType = VRC_Trigger.TriggerType.OnInteract;
		onInteract.BroadcastType = VRC_EventHandler.VrcBroadcastType.AlwaysBufferOne;

		trigger.Triggers.Add (onInteract);

		return button;
	}

	bool endsWithImageExt(String filename) {
		string[] validExtensions = {
			".png",
			".jpg",
			".tif",
			".bmp",
			".gif"
		};
		foreach (string ext in validExtensions) {
			if (filename.EndsWith(ext, true, null)) {
				Debug.Log (filename + " found to be an image file.");
				return true;
			}
		}
		Debug.Log (filename + " not found to be an image file.");
		return false;
	}

}

// NOTE: The following code was developed by Dot Net Pearls and is free to use in any program.
// It provides alphanumeric sorting in a way that makes sense to humans ("2" < "10")
public class AlphanumComparatorFast : IComparer
{
	public int Compare(object x, object y)
	{
		string s1 = x as string;
		if (s1 == null)
		{
			return 0;
		}
		string s2 = y as string;
		if (s2 == null)
		{
			return 0;
		}

		int len1 = s1.Length;
		int len2 = s2.Length;
		int marker1 = 0;
		int marker2 = 0;

		// Walk through two the strings with two markers.
		while (marker1 < len1 && marker2 < len2)
		{
			char ch1 = s1[marker1];
			char ch2 = s2[marker2];

			// Some buffers we can build up characters in for each chunk.
			char[] space1 = new char[len1];
			int loc1 = 0;
			char[] space2 = new char[len2];
			int loc2 = 0;

			// Walk through all following characters that are digits or
			// characters in BOTH strings starting at the appropriate marker.
			// Collect char arrays.
			do
			{
				space1[loc1++] = ch1;
				marker1++;

				if (marker1 < len1)
				{
					ch1 = s1[marker1];
				}
				else
				{
					break;
				}
			} while (char.IsDigit(ch1) == char.IsDigit(space1[0]));

			do
			{
				space2[loc2++] = ch2;
				marker2++;

				if (marker2 < len2)
				{
					ch2 = s2[marker2];
				}
				else
				{
					break;
				}
			} while (char.IsDigit(ch2) == char.IsDigit(space2[0]));

			// If we have collected numbers, compare them numerically.
			// Otherwise, if we have strings, compare them alphabetically.
			string str1 = new string(space1);
			string str2 = new string(space2);

			int result;

			if (char.IsDigit(space1[0]) && char.IsDigit(space2[0]))
			{
				int thisNumericChunk = int.Parse(str1);
				int thatNumericChunk = int.Parse(str2);
				result = thisNumericChunk.CompareTo(thatNumericChunk);
			}
			else
			{
				result = str1.CompareTo(str2);
			}

			if (result != 0)
			{
				return result;
			}
		}
		return len1 - len2;
	}
}
