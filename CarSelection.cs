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
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class CarSelection : MonoBehaviour
{
	private List<RCC_CarControllerV3> spawnedVehicles = new List<RCC_CarControllerV3>(); // whole vehicle list
	private List<int> idsInCategory = new List<int>(); // IDs of cars that are spawned in a certain category
	private CrashTimeVehiclesDB.VehicleConfig currentConfig;
	public List<string> categories = new List<string>();
	public List<string> namesList = new List<string>(); // car name list from the loaded category
	public string selectedCategory; // category name
	public int selectedCategoryID; // ID of selected category
	private Dictionary<string, List<int>> mapCatsToIDLists = new Dictionary<string, List<int>>();

	public Transform spawnPosition;
	public bool spawned = false;
	private float decay;
	public List<ContactPoint> wheelContactPoints = new List<ContactPoint>();

	public CitenysMenu menu;

	public int selectedIndex = 0;
	public int selectedIndexInCat = 0;

	public Text categoryText;
	public ScrollRect carNameList;

	public GameObject playerCar;
	public float rotateSpeed = 20f;
	public Vector3 lastRotation;

	public Camera mainCamera;
	private Vector3 initialCameraPos = new Vector3(170f, 182f, -105f);
	private Quaternion initialCameraRot = new Quaternion(0f, 0f, 0f, 1f);

	public string nextScene;

	#region Characteristics progressbars
	public ProgressBar speedBar;
	public ProgressBar accelBar;
	public ProgressBar handlingBar;
	public ProgressBar ratingBar;
	#endregion

	#region Color swatches and key icons
	public RectTransform colorSelection;
	[Range(0, 9)]
	public int currentColor;
	private Image[] colors;
	#endregion

	public LoadingScreenManager loadingScreen;

	public UnityEvent prevColBtnPressed = new UnityEvent();
	public UnityEvent nextColBtnPressed = new UnityEvent();
	public UnityEvent selectBtnPressed = new UnityEvent();
	public UnityEvent prevVehicleBtnPressed = new UnityEvent();
	public UnityEvent nextVehicleBtnPressed = new UnityEvent();
	public UnityEvent prevCatBtnPressed = new UnityEvent();
	public UnityEvent nextCatBtnPressed = new UnityEvent();

	void Start()
	{
		if(!loadingScreen)
			loadingScreen = FindObjectOfType<LoadingScreenManager>();
		spawned = false;

		if(!mainCamera)
			mainCamera = FindObjectOfType<Camera>();

		mainCamera.transform.position = initialCameraPos;
		mainCamera.transform.rotation = initialCameraRot;

		colors = GameObject.Find("CarColors").GetComponentsInChildren<Image>();
		
		prevColBtnPressed.AddListener(PreviousColor);
		nextColBtnPressed.AddListener(NextColor);
		selectBtnPressed.AddListener(SelectVehicle);
		prevVehicleBtnPressed.AddListener(PreviousVehicleInCategory);
		nextVehicleBtnPressed.AddListener(NextVehicleInCategory);
		prevCatBtnPressed.AddListener(PreviousCategory);
		nextCatBtnPressed.AddListener(NextCategory);

		if(FindObjectOfType<PlayerCarHUD>())
			FindObjectOfType<PlayerCarHUD>().enabled = false;

		LoadAllVehicles();
	}

	private void LoadAllVehiclesInCategory(string catName)
	{
		idsInCategory.Clear();
		namesList.Clear();
		if(mapCatsToIDLists.Count() != 0)
		{
			foreach(int id in mapCatsToIDLists[catName])
			{
				idsInCategory.Add(id);

				namesList.Add(CrashTimeVehiclesDB.Instance.vehicles[id].displayName);
			}

			//selectedIndexInCat = 0;
			//selectedIndex = mapCatsToIDLists[catName][0];
			selectedIndexInCat = PlayerPrefs.GetInt("LastSelectedVehicleInCat");
			if(selectedIndexInCat > idsInCategory.Count - 1)
				selectedIndexInCat = 0;

			selectedIndex = mapCatsToIDLists[catName][selectedIndexInCat];

			menu.Fill(namesList);

			for(int i = 0; i < menu.items.Count; i++)
			{
				LocalizeStringEvent localizeEvent = menu.items[i].GetComponentInChildren<LocalizeStringEvent>();
				LocalizedString localizedNameString = new LocalizedString("CarNames", namesList[i]);
				localizeEvent.StringReference = localizedNameString;
				string localizedName = localizedNameString.GetLocalizedString();
				if(localizedName != "")
					menu.items[i].SetText(localizedName);
			}

			SpawnVehicleFromCategory(catName, selectedIndexInCat);

		}
	}

	private void LoadAllVehicles()
	{
		for(int i = 0; i < CrashTimeVehiclesDB.Instance.vehicles.Length; i++)
		{
			RCC_CarControllerV3 spawnedCar = RCC.SpawnRCCWithLoadedSpecs(i, CrashTimeVehiclesDB.Instance.vehicles[i].defaultColor, spawnPosition.position, spawnPosition.rotation, false, false, false);
			spawnedCar.gameObject.SetActive(false);

			if(spawnedCar.gameObject.GetComponent<FieldOfView>())
				spawnedCar.gameObject.GetComponent<FieldOfView>().enabled = false;
			if(spawnedCar.gameObject.GetComponent<Gun>())
				spawnedCar.gameObject.GetComponent<Gun>().enabled = false;
			spawnedVehicles.Add(spawnedCar);
		}

		categories = CrashTimeVehiclesDB.Instance.vehicles.Select(conf => conf.category).Distinct().ToList();

		// mapping car categories to lists of car IDs (indices in CarDBList.asset)
		mapCatsToIDLists.Clear();
		foreach(string cat in categories)
		{
			List<int> idList = new List<int>();
			for(int i = 0; i < CrashTimeVehiclesDB.Instance.vehicles.Length; i++)
			{
				if(CrashTimeVehiclesDB.Instance.vehicles[i].category == cat)
					idList.Add(i);
			}
			mapCatsToIDLists.Add(cat, idList);
		}

		//selectedCategory = categories[0];
		//selectedCategoryID = 0;
		selectedCategoryID = PlayerPrefs.GetInt("LastSelectedCategory");
		if(selectedCategoryID > categories.Count - 1)
			selectedCategoryID = 0;
		else
			selectedCategory = categories[selectedCategoryID];

		LoadAllVehiclesInCategory(selectedCategory);
		
		//SpawnVehicleFromCategory(selectedCategory, 0);
	}

	public void NextVehicleInCategory()
	{
		selectedIndexInCat++;
		if(selectedIndexInCat > idsInCategory.Count - 1)
			selectedIndexInCat = idsInCategory.Count - 1;
		selectedIndex = mapCatsToIDLists[selectedCategory][selectedIndexInCat];
		SpawnVehicleFromCategory(selectedCategory, selectedIndexInCat);
	}

	public void PreviousVehicleInCategory()
	{
		selectedIndexInCat--;
		if(selectedIndexInCat < 0)
			selectedIndexInCat = 0;
		selectedIndex = mapCatsToIDLists[selectedCategory][selectedIndexInCat];
		SpawnVehicleFromCategory(selectedCategory, selectedIndexInCat);
	}

	private void SpawnVehicleFromCategory(string catName, int indexInCat)
	{
		if(indexInCat < mapCatsToIDLists[catName].Count())
		{
			selectedCategory = catName;

			menu.SelectItem(indexInCat);

			foreach(RCC_CarControllerV3 controller in spawnedVehicles)
			{
				controller.useDamage = false;
				controller.gameObject.SetActive(false);
				StartCoroutine(ToggleVisibility(controller, false));
			}

			spawnedVehicles[selectedIndex].gameObject.SetActive(true);

			StartCoroutine(ToggleVisibility(spawnedVehicles[selectedIndex], true));

			currentConfig = CrashTimeVehiclesDB.Instance.vehicles.FirstOrDefault(conf => conf.category == catName && conf.id == selectedIndex);

			if(currentConfig.unlocked)
			{
				playerCar = spawnedVehicles[selectedIndex].gameObject;
				playerCar.transform.rotation = Quaternion.Euler(lastRotation);

				RCC_SceneManager.Instance.activePlayerVehicle = spawnedVehicles[selectedIndex];

				FOVZooming();

				mainCamera.transform.position = initialCameraPos;
				mainCamera.transform.rotation = initialCameraRot;

				GameObject.Find("Cube").transform.position = new Vector3(0, spawnPosition.position.y, 286);

				SetCategoryText(selectedCategory);

				SetSpeedBar(currentConfig.maxSpeed);
				SetAccelBar(currentConfig.acceleration * 10f);
				SetHandlingBar(GetHandlingValue(spawnedVehicles[selectedIndex]));
				SetRatingBar(currentConfig.rating);

				if(colorSelection)
				{
					if(spawnedVehicles[selectedIndex].GetBodyMtlPresets().Length < 10)
						colorSelection.gameObject.SetActive(false);
					else
					{
						if(!colorSelection.gameObject.activeInHierarchy)
							colorSelection.gameObject.SetActive(true);

						if(selectedIndex == PlayerPrefs.GetInt("SelectedPlayerVehicle"))
							currentColor = PlayerPrefs.GetInt("SelectedPlayerVehicleColor");
						else
							currentColor = spawnedVehicles[selectedIndex].defaultColor;

						for(int i = 0; i < colors.Length; i++)
							DeselectColorBox(i);
						SelectColorBox(currentColor);

						spawnedVehicles[selectedIndex].SetColor(currentColor);
					}
				}
			}
			else
			{
				// Spawn a TolleKiste instead
			}
		}
		else
			Debug.LogError("Can't spawn the vehicle: index in category out of bounds");
	}

	private void FixedUpdate()
	{
		if(spawnedVehicles[selectedIndex].gameObject.GetComponent<Rigidbody>().velocity.magnitude > 0)
			spawnedVehicles[selectedIndex].gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
	}

	private bool IsGrounded(RCC_CarControllerV3 car)
	{
		return spawnedVehicles[selectedIndex].allWheelColliders.All(col => col.isGrounded == true);
	}

	private float GetWheelbase(RCC_CarControllerV3 car)
	{
		return car.FrontLeftWheelCollider.transform.localPosition.z - car.RearLeftWheelCollider.transform.localPosition.z;
	}

	private float GetHandlingValue(RCC_CarControllerV3 car)
	{
		RCC_CarControllerV3.WheelType transmission = car.wheelTypeChoise;
		float result = 0;
		switch(transmission)
		{
			case RCC_CarControllerV3.WheelType.FWD:
				result = GetWheelbase(car) * 229.5f;
				break;
			case RCC_CarControllerV3.WheelType.RWD:
				result = GetWheelbase(car) * 223.5f;
				break;
			case RCC_CarControllerV3.WheelType.AWD:
				result = GetWheelbase(car) * 220.5f;
				break;
		}
		return result;
	}

	public void SelectVehicle()
	{
		if(currentConfig.unlocked)
		{
			RCC.RegisterPlayerVehicle(spawnedVehicles[selectedIndex]);

			spawnedVehicles[selectedIndex].StartEngine();
			spawnedVehicles[selectedIndex].SetCanControl(true);
			spawnedVehicles[selectedIndex].useDamage = true;

			//if(spawnedVehicles[selectedIndex].gameObject.GetComponent<FieldOfView>())
			//	spawnedVehicles[selectedIndex].gameObject.GetComponent<FieldOfView>().enabled = true;
			//if(spawnedVehicles[selectedIndex].gameObject.GetComponent<Gun>())
			//	spawnedVehicles[selectedIndex].gameObject.GetComponent<Gun>().enabled = true;

			PlayerPrefs.SetInt("SelectedPlayerVehicle", selectedIndex);
			PlayerPrefs.SetInt("SelectedPlayerVehicleColor", spawnedVehicles[selectedIndex].selectedColor);
			PlayerPrefs.SetInt("LastSelectedCategory", selectedCategoryID);
			PlayerPrefs.SetInt("LastSelectedVehicleInCat", selectedIndexInCat);

			if(FindObjectOfType<PlayerCarHUD>())
				FindObjectOfType<PlayerCarHUD>().enabled = false;

			if(nextScene != "")
			//OpenScene();
			{
				loadingScreen.LoadScene(nextScene);
			}
		}
	}

	public void OpenScene()
	{
		SceneManager.LoadScene(nextScene);
	}

	private void Reset()
	{
		if(spawned && decay > 0)
			decay -= Time.deltaTime;
		if(decay < 0)
		{
			decay = 0;
			spawned = false;
		}
	}

	void Update()
	{
		if(playerCar)
		{
			Reset();
			playerCar.transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
			lastRotation = playerCar.transform.rotation.eulerAngles;

			if(Input.GetKey(KeyCode.D))
			{
				rotateSpeed = 70f;
			}
			if(Input.GetKey(KeyCode.A))
			{
				rotateSpeed = -70f;
			}
			if(Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D))
				rotateSpeed = 20f;
		}
		#region Events firing when pressing on keyboard keys
		if(Input.GetKeyDown(KeyCode.LeftArrow) && !spawned)
		{
			decay = 0.7f;
			spawned = true;
			prevCatBtnPressed.Invoke();
		}
		if(Input.GetKeyDown(KeyCode.RightArrow) && !spawned)
		{
			decay = 0.7f;
			spawned = true;
			nextCatBtnPressed.Invoke();
		}

		if(Input.GetKeyDown(KeyCode.UpArrow) && !spawned) // previous vehicle in category list
		{
			decay = 0.7f;
			spawned = true;
			prevVehicleBtnPressed.Invoke();
		}
		if(Input.GetKeyDown(KeyCode.DownArrow) && !spawned) // next vehicle in category list
		{
			decay = 0.7f;
			spawned = true;
			nextVehicleBtnPressed.Invoke();
		}

		if(Input.GetKeyDown(KeyCode.PageDown)) // next color
		{
			nextColBtnPressed.Invoke();
		}
		if(Input.GetKeyDown(KeyCode.PageUp)) // previous color
		{
			prevColBtnPressed.Invoke();
		}

		if(Input.GetKeyDown(KeyCode.Return)) // select vehicle
		{
			selectBtnPressed.Invoke();
		}

		Canvas.ForceUpdateCanvases();

		#endregion
	}

	#region Progressbars
	void SetSpeedBar(float value)
	{
		speedBar.BarValue = value;
	}

	void SetAccelBar(float value)
	{
		accelBar.BarValue = value;
	}

	void SetHandlingBar(float value)
	{
		handlingBar.BarValue = value;
	}

	void SetRatingBar(float value)
	{
		ratingBar.BarValue = value;
	}

	void SetCategoryText(string text)
	{
		categoryText.text = text;
		LocalizeStringEvent localizeCategoryEvent = categoryText.GetComponent<LocalizeStringEvent>();
		LocalizedStringTable stringTable = new LocalizedStringTable("CarCategories");
		LocalizedString localizedCatString = new LocalizedString(stringTable.TableReference, selectedCategory);
		localizeCategoryEvent.StringReference = localizedCatString;
		string localizedCategory = localizeCategoryEvent.StringReference.GetLocalizedString();
		if(localizedCategory != "")
			categoryText.text = localizedCategory;
	}
	#endregion

	void FOVZooming()
	{
		float cameraDistance = 2.0f;
		Vector3 boundsCenter = RCC_GetBounds.GetBoundsCenter(playerCar.transform);
		float objectSize = RCC_GetBounds.MaxBoundsExtent(playerCar.transform);
		float cameraView = 2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad * mainCamera.fieldOfView);
		float distance = cameraDistance * objectSize / cameraView;
		distance += 0.5f * objectSize;
		mainCamera.transform.position = boundsCenter - distance * mainCamera.transform.forward;

		// Additional camera adjusting
		mainCamera.transform.Translate(-2f, 2f, 0);
		mainCamera.transform.Rotate(10.1f, 0f, 0f);
	}

	IEnumerator ToggleVisibility(RCC_CarControllerV3 car, bool state)
	{
		foreach(MeshRenderer renderer in car.gameObject.GetComponentsInChildren<MeshRenderer>())
		{
			renderer.enabled = state;
		}
		if(state == true && !IsGrounded(car))
			yield return new WaitUntil(() => IsGrounded(car));
	}

	#region Controllable GUI elements
	void SelectColorBox(int color)
	{
		colors[color].GetComponent<Outline>().effectColor = Color.white;
	}

	void DeselectColorBox(int color)
	{
		colors[color].GetComponent<Outline>().effectColor = Color.black;
	}

	public void PreviousCategory()
	{
		selectedCategoryID--;
		if(selectedCategoryID < 0)
			selectedCategoryID = categories.Count - 1;
		selectedCategory = categories[selectedCategoryID];
		selectedIndexInCat = 0;
		LoadAllVehiclesInCategory(categories[selectedCategoryID]);
	}

	public void NextCategory()
	{
		selectedCategoryID++;
		if(selectedCategoryID > categories.Count - 1)
			selectedCategoryID = 0;
		selectedCategory = categories[selectedCategoryID];
		selectedIndexInCat = 0;
		LoadAllVehiclesInCategory(categories[selectedCategoryID]);
	}

	#endregion

	public void PreviousColor()
	{
		if(GameObject.Find("ColorSelection").activeInHierarchy)
		{
			DeselectColorBox(currentColor);
			currentColor--;
			if(currentColor < 0)
				currentColor = 0;
			SelectColorBox(currentColor);
			spawnedVehicles[selectedIndex].SetColor(currentColor);
		}
	}

	public void NextColor()
	{
		if(GameObject.Find("ColorSelection").activeInHierarchy)
		{
			DeselectColorBox(currentColor);
			currentColor++;
			if(currentColor > 9)
				currentColor = 9;
			SelectColorBox(currentColor);
			spawnedVehicles[selectedIndex].SetColor(currentColor);
		}
	}
}
