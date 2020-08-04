using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
	/// <summary>Управляет главным вступительным уровнем игры (Main Menu).</summary>
	public class MainMenuController : MonoBehaviour
	{
		#region Параметры
		[Tooltip("Главная тема. Может не проигрываться, если игрок попал в главное меню после победы.")]
		public AudioSource MainTheme;
		[Tooltip("Весь канвас. Нужен, чтобы спрятать его по старту игры.")]
		public CanvasGroup MainCanvas;
		[Tooltip("Озвучка, включающаясь по началу игры.")]
		public AudioSource Intro;

		[Tooltip("Заставка Maya Gameworks Presents.")]
		public Image MayaGameworksPresents;
		[Tooltip("Заставка The Last Hope Of The Earth.")]
		public Image TheLastHopeOfTheEarth;

		[Tooltip("Скриптованый объект, в котором храняться параметры игры.")]
		public SavedParameters SavedParameters;

		[Tooltip("Группа 'Основное меню'.")]
		public GameObject MainGroup;
		[Tooltip("Группа 'Меню опций'.")]
		public GameObject OptionsGroup;
		[Tooltip("Квадратик рядом с опцией 'Инвертировать мышь по Y'.")]
		public Image SquareUI;
		[Tooltip("Спрайт пустого квадратика.")]
		public Sprite Square;
		[Tooltip("Спрайт полного квадратика.")]
		public Sprite TickedSquare;

		[Tooltip("Этот черный квадрат плавно скроется в начале игры.")]
		public Image Black;

		/// <summary>Стоит ли инвертировать ввод мыши по вертикали (задается в опциях игры).</summary>
		public static bool InvertMouseY = true;

		/// <summary>Музыка победы. Эта переменная будет заполнена в Awake(), если игрок попал в главное меню после победы в игре.</summary>
		[HideInInspector][NonSerialized]
		public AudioSource VictoryTheme;
		#endregion

		#region Старт
		private void Awake()
		{
			var go = GameObject.Find("Victory Theme");
			if (go == null)
				MainTheme.Play();
			else
			{
				VictoryTheme = go.GetComponent<AudioSource>();
				if (VictoryTheme == null || !VictoryTheme.isPlaying)
				{
					MainTheme.Play();
					Destroy(VictoryTheme.gameObject);
				}
			}

			Cursor.lockState = CursorLockMode.Confined;
			Cursor.visible = true;

			if (File.Exists(Application.dataPath + "/TLHOTE.ini"))
			{
				StreamReader reader = new StreamReader(Application.dataPath + "/TLHOTE.ini");
				JsonUtility.FromJsonOverwrite(reader.ReadLine(), SavedParameters);
			}
			else
			{
				SavedParameters.InvertMouseY = true;
				string json = JsonUtility.ToJson(SavedParameters);
				File.WriteAllText(Application.dataPath + "/TLHOTE.ini", json);
			}
			InvertMouseY = SavedParameters.InvertMouseY;

			if (InvertMouseY)
				SquareUI.sprite = TickedSquare;
			else
				SquareUI.sprite = Square;

			OptionsGroup.SetActive(false);

			if (Black != null)
				Black.gameObject.SetActive(true);
		}
		#endregion



		#region Функции меню
		/// <summary>Запрос начала игры.</summary>
		public void StartGame()
		{
			GameIsStarting = true;
			Cursor.visible = false;
			if (Intro != null)
				Intro.Play();
		}

		/// <summary>Включить меню опций.</summary>
		public void ShowOptions()
		{
			MainGroup.SetActive(false);
			OptionsGroup.SetActive(true);
		}

		/// <summary>Включить основное меню.</summary>
		public void BackToMainMenu()
		{
			OptionsGroup.SetActive(false);
			MainGroup.SetActive(true);
		}

		/// <summary>Сменить режим инверсии мыши по Y (вкл/выкл).</summary>
		public void ToggleInvertMouseY()
		{
			SavedParameters.InvertMouseY = !SavedParameters.InvertMouseY;
			InvertMouseY = SavedParameters.InvertMouseY;

			if (InvertMouseY)
				SquareUI.sprite = TickedSquare;
			else
				SquareUI.sprite = Square;

			string json = JsonUtility.ToJson(SavedParameters);
			File.WriteAllText(Application.dataPath + "/TLHOTE.ini", json);
		}

		/// <summary>Выход из игры.</summary>
		public void ExitGame()
		{
			Application.Quit();
		}
		#endregion

		#region Update
		/// <summary>Игра начитается.</summary>
		bool GameIsStarting = false;

		private void Update()
		{
			// Плавно затухаем черный квадрат по заргузке уровня.
			if (Black != null && Black.color.a > 0)
			{
				Black.color = new Color(0, 0, 0, 1 - ((Time.timeSinceLevelLoad - 0.5f) * .5f));
				if (Black.color.a <= 0)
					Black.gameObject.SetActive(false);
			}

			// Логика плавного начала игры.
			if (GameIsStarting)
			{
				// Если вступление закончилось проигрываться - запускаем главный уровень.
				if (!Intro.isPlaying)
					SceneManager.LoadScene("The Border");

				// Плавно выключаем музыку.
				if (MainTheme != null)
					MainTheme.volume -= Time.deltaTime;

				// Плавно выключаем музыку победы и удаляем ее, так как в загруженом уровне будет такой же GameObject.
				if (VictoryTheme != null)
				{
					VictoryTheme.volume -= Time.deltaTime;

					if (VictoryTheme.volume <= 0)
						Destroy(VictoryTheme.gameObject);
				}


				// Плавно затухаем все главное меню.
				if (MainCanvas != null)
					MainCanvas.alpha -= Time.deltaTime;


				if (Intro.time > 14)	// Выключаем заставку 'The Last Hope Of The Earth'
					TheLastHopeOfTheEarth.color = new Color(TheLastHopeOfTheEarth.color.r,
						TheLastHopeOfTheEarth.color.g, TheLastHopeOfTheEarth.color.b,
						Mathf.Clamp01(TheLastHopeOfTheEarth.color.a - Time.deltaTime * .4f));
				else if (Intro.time > 10.5f)    // Показываем заставку 'The Last Hope Of The Earth'
					TheLastHopeOfTheEarth.color = new Color(TheLastHopeOfTheEarth.color.r,
						TheLastHopeOfTheEarth.color.g, TheLastHopeOfTheEarth.color.b,
						Mathf.Clamp01(TheLastHopeOfTheEarth.color.a + Time.deltaTime * .4f));
				else if (Intro.time > 6)    // Выключаем заставку 'Maya Gameworks Presents'.
					MayaGameworksPresents.color = new Color(MayaGameworksPresents.color.r,
						MayaGameworksPresents.color.g, MayaGameworksPresents.color.b,
						Mathf.Clamp01(MayaGameworksPresents.color.a - Time.deltaTime * .4f));
				else if (Intro.time > 3)    // Показываем заставку 'Maya Gameworks Presents'.
					MayaGameworksPresents.color = new Color(MayaGameworksPresents.color.r,
						MayaGameworksPresents.color.g, MayaGameworksPresents.color.b,
						Mathf.Clamp01(MayaGameworksPresents.color.a + Time.deltaTime * .4f));
			}
		}
		#endregion

	}
}
