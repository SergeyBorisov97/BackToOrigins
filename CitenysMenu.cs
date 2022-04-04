/**
 * 
 * Copyright © 2022 Citenys Games 
 * http://www.citenys-games.de
 * Plakhotniuk Sergey (aka borisov97)
 * 
 **/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Events;

public class CitenysMenu : MonoBehaviour
{
    public List<CitenysMenuItem> items = new List<CitenysMenuItem>();
    public ScrollRect container;
    public CitenysMenuItem selectedItem;
    public int selectedIndex = 0;
    
    private void Awake()
    {
        if(!container)
            container = gameObject.GetComponent<ScrollRect>();
    }

    private void Clear()
	{
        for(int i = 0; i < items.Count; i++)
		{
            Destroy(items[i].gameObject);
            items[i] = null;
		}
        items.Clear();
        selectedIndex = -1;
    }

    private void AddItem(string value, int index)
	{
        Image prefab = Resources.Load<Image>("FrontEnd/Prefabs/ListItem");
        Image instance = Instantiate<Image>(prefab);

        instance.GetComponent<CitenysMenuItem>().index = index;
        instance.rectTransform.SetParent(container.content.transform, false);
        instance.GetComponentInChildren<Text>().text = value;
        instance.gameObject.name += index;

        items.Add(instance.GetComponent<CitenysMenuItem>());
    }

    private void DeselectAll()
	{
        if(container.content.childCount > 0)
		{
            foreach(CitenysMenuItem item in items)
                item.Deactivate();
		}
        selectedItem = null;
        selectedIndex = -1;
    }

    public void ClearSelection()
	{
        DeselectAll();
        container.GraphicUpdateComplete();
	}

    public void SelectItem(int index)
	{
        DeselectAll();
        if(index == -1)
            return;
        if(index >= 0 && index < items.Count)
		{
            selectedItem = items[index];
            selectedIndex = index;
            items[index].Activate();
        }
	}

    public void Fill(IEnumerable<string> values)
	{
        if(items != null)
            Clear();
        for(int i = 0; i < values.Count(); i++)
		{
            AddItem(values.ElementAt(i), i);
		}
	}
}
