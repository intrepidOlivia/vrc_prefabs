using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SOption {

	public TScreen screen;
	public int destination;
	public string text;
	public int index;
	public GameObject optionObj;

	public SOption(int index, TScreen s) {
		this.index = index;
		this.screen = s;
	}
}
