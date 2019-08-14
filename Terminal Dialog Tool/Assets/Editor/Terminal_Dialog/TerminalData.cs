using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TerminalData {

	public static List<TerminalData> terminals = new List<TerminalData>();
	static string SCREEN_OPTIONS_POSTFIX = "screen_options.txt";
	public static readonly string SAVE_PATH = "Assets/TerminalDialog/Save Files/";

	public string id;
	public List<TScreen> screens = new List<TScreen>();
	public TerminalObject terminalObject;

	public TerminalData(TerminalObject tObj) {
		this.id = tObj.id;
		this.terminalObject = tObj;
		terminals.Add (this);
	}

	static string getScreenFileName(int index, string terminalID) {
		return SAVE_PATH + terminalID + "_screen_" + index + ".txt";
	}

	public static TerminalData loadTerminalFromFile(TerminalObject gameTerminal) {
		if (gameTerminal == null || gameTerminal.id == null) {
			return null;
		}

		TerminalData tData = terminals.Find (t => t.id == gameTerminal.id);
		if (tData == null) {
			tData = new TerminalData (gameTerminal);
		}

		// No need to check if the right directory exists because that's where the class files are stored

		// Load Screens from file
		int i = 0;
		while (File.Exists (getScreenFileName(i, gameTerminal.id))) {
			StreamReader reader = new StreamReader(getScreenFileName(i, gameTerminal.id));
			string line;
			string id;
			string text = "";
			id = reader.ReadLine();	// ID is first line of save file
			while ((line = reader.ReadLine()) != null) {
				text += line + System.Environment.NewLine;
			}

			// Create screen
			TScreen screen = new TScreen(tData, text, i, id);
			// Add screen to terminal
			TScreen oldScreen = tData.screens.Find(s => s.index == i);
			if (oldScreen == null) {
				// if screen doesn't exist, add it to screens
				tData.screens.Add(screen);
			} else {
				// if screen already exists, replace it with file screen
				oldScreen = screen;
			}

			reader.Close();
			i++;
		}

		// load all options for screens
		string screenOptionsPath = SAVE_PATH + gameTerminal.id + SCREEN_OPTIONS_POSTFIX;
		if (File.Exists(screenOptionsPath)) {
			StreamReader optionReader = new StreamReader (screenOptionsPath);
			string line;
			i = 0;
			while ((line = optionReader.ReadLine()) != null) {
				TScreen screen = tData.screens[i];
				string[] ops = line.Split('|');
				int j = 0;
				int n = 0;
				while (j < ops.Length - 1) {
					SOption newOption = new SOption (n++, screen);
					newOption.text = ops [j];
					int.TryParse (ops [j + 1], out newOption.destination);

					// Check for old version of option
					SOption oldOption = screen.options.Find(o => o.index == newOption.index);
					if (oldOption == null) {
						screen.options.Add (newOption);
					} else {
						oldOption = newOption;
					}

					j += 2;
				}
				i++;
			}
		}

		return tData;
	}

	public static void saveTerminalFiles(UnityEngine.SceneManagement.Scene scene) {
		cleanUpTerminals ();

		foreach (TerminalData t in TerminalData.terminals) {
			TerminalData.saveScreens (t);
		}
	}

	static void saveScreens(TerminalData t) {
		string screenOptionsPath = SAVE_PATH + t.id + SCREEN_OPTIONS_POSTFIX;

		createScreenFiles (t);

		Debug.Log ("Writing screens to file for terminal " + t.id);

		StreamWriter optionWriter = new StreamWriter (screenOptionsPath, true);

		for (int i = 0; i < t.screens.Count; i++) {
			// Create screen file and write to it
			TScreen screen = t.screens[i];
			StreamWriter writer = new StreamWriter(getScreenFileName(i, t.id));
			writer.WriteLine (screen.id);
			writer.Write (screen.text);
			writer.Close ();

			// Write option line for this screen
			optionWriter.WriteLine(screen.getOptionsFileLine());
		}

		optionWriter.Close ();

	}

	static void createScreenFiles(TerminalData t) {
		Stream s;

		// Create all screen files
		foreach(TScreen screen in t.screens) {
			string filename = getScreenFileName (screen.index, t.id);
			File.Delete (filename);
			s = File.Create(filename);
			s.Close ();
		}

		// Create options file
		string screenOptionsPath = SAVE_PATH + t.id + SCREEN_OPTIONS_POSTFIX;
		File.Delete (screenOptionsPath);
		s = File.Create (screenOptionsPath);
		s.Close ();
	}

	/// <summary>
	/// Removes all terminals that do not have an associated game object
	/// </summary>
	static void cleanUpTerminals() {
		List<TerminalData> toDelete = new List<TerminalData> ();

		for (int i = 0; i < terminals.Count; i++) {
			if (terminals [i].terminalObject == null) {
				toDelete.Add (terminals [i]);
			}
		}

		foreach (TerminalData t in toDelete) {
			terminals.Remove (t);
		}
	}

	public void generateGameScreens(string screenPath, string optionPath) {
		foreach (TScreen s in screens) {
			s.buildGameObjects (screenPath, optionPath);
		}
	}

	public void generateGameTriggers (){
		foreach (TScreen s in screens) {
			s.setupTriggers (screens);
		}
		Debug.Log ("All VRC Triggers generated for terminal " + id);
	}
}
