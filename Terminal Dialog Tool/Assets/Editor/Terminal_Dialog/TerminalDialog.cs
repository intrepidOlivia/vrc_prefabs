using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VRCSDK2;

public class TerminalDialog : EditorWindow
{

    enum windowState { init, loaded, creatingScreen };

    GameObject terminal;
    static GameObject currentTerminal;
    TerminalData terminalData;

    TScreen screen;
    SOption option;
    static UnityEngine.SceneManagement.Scene currentScene;
    
	// UI scrolling and colors
	Vector2 scrollPos;
	GUIStyle redFont = new GUIStyle();

	// Paths and Prefabs
	string screenPrefabPath = "Assets/TerminalDialog/Prefabs/Screen.prefab";
	string optionPrefabPath = "Assets/TerminalDialog/Prefabs/Option.prefab";
    Object screenPrefab;
    Object optionPrefab;
    static readonly string SAVE_PATH = TerminalData.SAVE_PATH;
    string screenIndexPath;
    string screenListPath;

    int screenIndex;
    int optionIndex;
    int destinationScreen;
    string newScreenID; // If not a class variable it will refresh on every GUI update

    windowState state = windowState.init;

    [InitializeOnLoadMethod]
    static void Init()
    {
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += TerminalData.saveTerminalFiles;
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += onSceneOpen;
    }

    [MenuItem("Tools/TerminalDialog")]
    static void InitWindow()
    {
        // Get existing open window or if none, make a new one:
        TerminalDialog.currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        TerminalDialog window = (TerminalDialog)EditorWindow.GetWindow(typeof(TerminalDialog));
        window.Show();
    }

    static void CloseWindow()
    {
        TerminalDialog window = (TerminalDialog)EditorWindow.GetWindow(typeof(TerminalDialog));
        window.Close();
    }


    void OnGUI()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(TerminalDialog.currentScene);

        GUILayout.Label("Specify Prefabs to use:", EditorStyles.boldLabel);
        screenPrefabPath = (string)EditorGUILayout.TextField("Path to Screen Prefab", screenPrefabPath);
        optionPrefabPath = (string)EditorGUILayout.TextField("Path to Option Prefab", optionPrefabPath);
        GUILayout.Label("Drag Terminal From Scene", EditorStyles.boldLabel);
        terminal = (GameObject)EditorGUILayout.ObjectField("Terminal Object:", terminal, typeof(GameObject), true);

        // If player changes terminal in the middle of editing
        if (terminal != currentTerminal)
        {
            currentTerminal = terminal;
            this.state = windowState.init;
        }

        if (terminalData != null)
        {
            EditorGUILayout.LabelField("TerminalID", terminalData.id);
        }

        if (terminal != null)
        {
            if (state == windowState.init)
            {
                loadTerminalFromFile();
            }

            onTerminalGUI();
            GUILayout.Label("Save scene to save all terminal dialog settings.", EditorStyles.boldLabel);
            return;
        }

        GUILayout.Label("Please add the Terminal component to your game object and give it an ID", EditorStyles.wordWrappedLabel);
        resetGUI();
    }

    void resetGUI()
    {
        state = windowState.init;
        terminalData = null;
        terminal = null;
    }

    void onTerminalGUI()
    {

		scrollPos = GUILayout.BeginScrollView(scrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

        GUILayout.Label("Edit Screen", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        string[] screenSelect = terminalData.screens.Select(s => s.id).ToArray();

        screenIndex = EditorGUILayout.Popup("Select screen to edit:", screenIndex, screenSelect);
        if (GUILayout.Button("+"))
        {
            state = windowState.creatingScreen;
        }
        EditorGUILayout.EndHorizontal();

        onScreenCreateGUI();

        TScreen screenEdit = terminalData.screens.Find(s => s.index == screenIndex);
        onScreenEditGUI(screenEdit);

        GUILayout.EndScrollView();

        if (state == windowState.loaded)
        {
            GUILayout.Space(30);

            if (screenEdit != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Generate Game Objects for screen " + screenEdit.id))
                {
                    screenEdit.buildGameObjects(screenPrefabPath, optionPrefabPath);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Generate all Game Objects for terminal " + terminalData.id))
            {
                terminalData.generateGameScreens(screenPrefabPath, optionPrefabPath);
            }


            if (GUILayout.Button("Generate all VRCTriggers for terminal " + terminalData.id))
            {
                terminalData.generateGameTriggers();
            }
            GUI.enabled = true;
        }
    }

    void onScreenCreateGUI()
    {
        if (state == windowState.creatingScreen)
        {
            // Creating a new screen

            GUILayout.Label("Create Screen", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(30);
            newScreenID = EditorGUILayout.TextField("New screen ID:", newScreenID);
            if (GUILayout.Button("Add Screen"))
            {
                addNewScreen(newScreenID, terminalData);
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Cancel"))
            {
                resetNewScreen();
            }
        }
    }

    void onScreenEditGUI(TScreen screen)
    {
        // Default view for editing screens

        if (screen == null || state != windowState.loaded)
        {
            return;
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        EditorGUILayout.PrefixLabel("screen text:");
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginHorizontal();
		GUILayout.Space(50);
		screen.text = EditorGUILayout.TextArea(screen.text, GUILayout.Height(64), GUILayout.Width(position.width - 75));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        Color origcolor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.8f, 0.4f, 0.4f);
        if (GUILayout.Button("Delete " + screen.id))
        {
            terminalData.screens.Remove(screen);
        }
        GUI.backgroundColor = origcolor;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        string[] optionSelect = screen.options.Select(o => o.index.ToString()).ToArray();

        EditorGUILayout.Space();
        GUILayout.Label("Edit Option", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        optionIndex = EditorGUILayout.Popup("Select option to edit:", optionIndex, optionSelect);
        if (GUILayout.Button("+"))
        {
            addNewOption(screen);
        }
        EditorGUILayout.EndHorizontal();

        SOption optionEdit = screen.options.Find(o => o.index == optionIndex);
        onOptionEditGUI(optionEdit);
    }

    void addNewScreen(string id, TerminalData t)
    {
        EditorGUILayout.Space();

        // Create new screen
        int newIndex = t.screens.Count;
        TScreen newScreen = new TScreen(t, "", newIndex, id);
        terminalData.screens.Add(newScreen);

        // Select newly created screen
        screenIndex = newIndex;

        resetNewScreen();
    }

    void resetNewScreen()
    {
        state = windowState.loaded;
        newScreenID = null;
    }

    void onOptionEditGUI(SOption option)
    {
        if (option == null || state != windowState.loaded)
        {
            return;
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.Label("option text:", EditorStyles.label);
        option.text = EditorGUILayout.TextField(option.text);
        EditorGUILayout.EndHorizontal();

		string[] destinations = terminalData.screens.Select(s => {
			if (s.index == option.screen.index) {
				return s.id + " [CURRENT SCREEN]";
			}
			return s.id;
		}).ToArray();

        EditorGUILayout.BeginHorizontal();
        option.destination = EditorGUILayout.Popup("Destination screen:", option.destination, destinations);
        EditorGUILayout.EndHorizontal();

		if (option.destination == option.screen.index) {
			redFont.normal.textColor = Color.red;
			EditorGUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			GUILayout.Label ("Destination is set to the current screen.", redFont);
			GUILayout.FlexibleSpace ();
			EditorGUILayout.EndHorizontal ();
		}

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        Color origcolor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.8f, 0.4f, 0.4f);
        if (GUILayout.Button("Delete option " + option.index))
        {
            option.screen.options.Remove(option);
        }
        GUI.backgroundColor = origcolor;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    void addNewOption(TScreen s)
    {
        int newIndex = s.options.Count;
        SOption newOption = new SOption(newIndex, s);
        s.options.Add(newOption);

        // Select newly created option
        optionIndex = newIndex;
    }

    void loadTerminalFromFile()
    {
        // Load terminal from file data
        TerminalObject gameTerminal = terminal.gameObject.GetComponent<TerminalObject>();
        terminalData = TerminalData.loadTerminalFromFile(gameTerminal);

        if (terminalData == null)
        {
            return;
        }

        state = windowState.loaded;
    }

    static void onSceneOpen(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
    {
        TerminalDialog.CloseWindow();
    }
}
