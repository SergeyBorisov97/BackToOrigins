/**
 * 
 * Copyright © 2022 Citenys Games 
 * http://www.citenys-games.de
 * Plakhotniuk Sergey (aka borisov97)
 * 
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CitenysMenuItem : MonoBehaviour
{
	public Image background;
	public int index;
	/// <summary>
	/// Whether the menu item is lit up or not. Don't mix with the <code>GameObject.active</code> variable.
	/// </summary>
	public bool activated;
	public Color deselectedBackColor = new Color(0.878f, 0.141f, 0.141f, 0f);
	public Color selectedBackColor = new Color(0.878f, 0.141f, 0.141f, 1f);

	public UnityEvent OnSelect = new UnityEvent();

	public void Deactivate()
	{
		background.color = deselectedBackColor;
		activated = false;
		background.GraphicUpdateComplete();
	}

	public void Activate()
	{
		background.color = selectedBackColor;
		activated = true;
		background.GraphicUpdateComplete();
	}

	public string GetText()
	{
		return GetComponentInChildren<Text>().text;
	}

	public void SetText(string text)
	{
		GetComponentInChildren<Text>().text = text;
	}

	private void Start()
	{
		background = GetComponent<Image>();
		activated = false;
	}

	private void OnDestroy()
	{
		GC.Collect();
	}
}