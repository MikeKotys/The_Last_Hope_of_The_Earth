using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Enemies;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;
using System;

namespace Mechanics
{
	/// <summary>Контроллирует ход игры, загрузку уровней и логику победы/поражения.</summary>
	public class GameController : MonoBehaviour
	{
		#region Параметры
		[Tooltip("Ссылка класс контроллера камеры, который висит на объекте пулемета.")]
		public FPSCameraController FPSCameraController;

		[Header("Размещение врагов"), Space(30)]
		[Tooltip("Как много врагов в одном ряду.")]
		public int NumberOfEnemiesInARow = 4;
		[Tooltip("Как много врагов в одной колоне.")]
		public int NumberOfEnemiesInAColumn = 4;

		[Tooltip("Список ссылок на всех врагов на уровне - должен включать НЕ МЕНЬШЕ элементов нежели .")]
		public List<Enemy> AllEnemies = new List<Enemy>();

		[Tooltip("Угол, на который будет вращатся каждый из врагов вокруг камеры, относительно соседней колоны врагов.")]
		public float AngleBetweenSaucersX = 35;
		[Tooltip("Угол, на который будет вращатся каждый из врагов вокруг камеры, относительно соседнего ряда врагов.")]
		public float AngleBetweenSaucersY = 15;
		[Tooltip("Угол, на который будет смещен каждый из врагов по горизонтали.")]
		public float AngleOffsetX = -35;
		[Tooltip("Угол, на который будет смещен каждый из врагов по вертикали.")]
		public float AngleOffsetY = -35;
		[Tooltip("Как долго должен моргать каждый враг во время анимации смены врага.")]
		public float EnemyBlinkInterval = 0.3f;
		[Tooltip("Радиус полусферы на котором находится каждый из врагов относительно игрока.")]
		public float EnemyDistance = 100;


		[Header("Компоненты логики победы"), Space(30)]
		[Tooltip("Как много врагов убить пока не наступит победа.")]
		public int EnemyKillsTillVictory = 10;
		[Tooltip("Этот звук проигрывается когда враги отступают.")]
		public AudioSource RetreatSound;
		[Tooltip("Звуковая группа всех звуков отступления.")]
		public AudioMixer RetreatMixer;

		[Tooltip("Звук цикад.")]
		public AudioSource CicadasSound;

		[Tooltip("Ссылка на класс, которые отвечает за обрабатывание логики победы.")]
		public Victory Victory;


		[Header("Меню"), Space(30)]
		[Tooltip("Объект меню, который будет показываться при нажатии Escape.")]
		public GameObject Menu;
		[Tooltip("Объект слушателя с помощью которого звуки будут ставится на паузу.")]
		public AudioListener AudioListener;
		[Tooltip("Этот черный квадрат плавно скроется в начале игры.")]
		public Image Black;

		/// <summary>Запрос показать врагов.</summary>
		[HideInInspector][NonSerialized]
		public bool RequestShowEnemies = false;
		/// <summary>Не позволяет искать нового врага.</summary>
		[HideInInspector][NonSerialized]
		public bool IsEnemyActivationPaused = true;
		#endregion

		#region Старт
		void Awake()
		{
			// Должно вызываться именно в Awake! не в Start!
			ArrangeEnemies();

			UnpauseTheGame();

			if (Black != null)
				Black.gameObject.SetActive(true);

			RetreatMixer.SetFloat("RetreatVolume", 0);
		}

		/// <summary>Включает врагов, позиционирует их в форме полусферы. Отключает лишних врагов.</summary>
		void ArrangeEnemies()
		{
			if (AllEnemies.Count > 0)
			{
				// Перемещаем каждого врага в точку, которая удалена от игрока на заданный радиус полусферы.
				for (int i = 0; i < NumberOfEnemiesInARow; i++)
				{
					for (int j = 0; j < NumberOfEnemiesInAColumn; j++)
					{
						int elementNum = i * NumberOfEnemiesInARow + j;

						if (AllEnemies.Count > elementNum)
						{
							AllEnemies[elementNum].gameObject.SetActive(true);
							AllEnemies[elementNum].transform.position = Quaternion.Euler(
								AngleBetweenSaucersX * (i - ((float)NumberOfEnemiesInAColumn * .5f)) + AngleOffsetX,
								AngleBetweenSaucersY * (j - ((float)NumberOfEnemiesInARow * .5f)) + AngleOffsetY, 0)
								* new Vector3(0, 0, EnemyDistance);
						}
					}
				}

				// Выключаем лишних врагов.
				if (AllEnemies.Count > NumberOfEnemiesInARow * NumberOfEnemiesInAColumn)
					for (int i = NumberOfEnemiesInARow * NumberOfEnemiesInAColumn; i < AllEnemies.Count; i++)
						AllEnemies[i].gameObject.SetActive(false);
			}
		}
		#endregion

		#region Смена врага.
		/// <summary>Последний активный враг (которого только что убили).</summary>
		Enemy OldChargingEnemy = null;
		/// <summary>Текущий активный враг.</summary>
		Enemy ActiveChargingEnemy = null;
		/// <summary>Последний мерцающий враг.</summary>
		Enemy OldBlinkingEnemy = null;

		/// <summary>Выбрать следующего случайного врага для активации.</summary>
		void AcivateARandomEnemy()
		{
			ActiveChargingEnemy = AllEnemies[UnityEngine.Random.Range(0, NumberOfEnemiesInARow * NumberOfEnemiesInAColumn)];
		}

		/// <summary>Когда в след. раз искать нового мерцающего врага.</summary>
		float NextFindEnemyToBlinkTime;
		/// <summary>Проигрывается ли в данный момент анимация мерцания линии.</summary>
		bool IsEnemyChangedLineAnimating = false;

		/// <summary>Анимация "линии" смены активного врага (поочередного мерцания врагов по направлению к новой цели).</summary>
		void EnemyChangedLineAnimationLogic()
		{
			// Строим вектор по прямой к новому врагу, потом двигаемся по этому вектору на эмпирически определенную дистанцию и в полученной
			//	точке ищем ближайшего врага, который не является OldChargingEnemy и не является OldBlinkingEnemy и не мертв.
			//	Найденого врага подсвечиваем (Blink) Если найденый ближайший
			//	враг == ActiveChargingEnemy - мы пришли, выключаем IsEnemyChangedLineAnimating.


			// Строим прямую линию.
			Vector3 straightLine;
			if (OldBlinkingEnemy != null)
				straightLine = ActiveChargingEnemy.transform.position - OldBlinkingEnemy.transform.position;
			else
				straightLine = ActiveChargingEnemy.transform.position - OldChargingEnemy.transform.position;

			straightLine = straightLine.normalized;


			// Смещаемся по линии.
			Vector3 newPos;
			if (OldBlinkingEnemy != null)
				newPos = OldBlinkingEnemy.transform.position + straightLine * 15.0f;
			else
				newPos = OldChargingEnemy.transform.position + straightLine * 15.0f;

			// Ищем ближайшего врага к новой точке.
			float smallestDistance = float.MaxValue;
			Enemy nearestEnemy = null;
			for (int i = 0; i < AllEnemies.Count; i++)
			{
				var sqMagnitude = (AllEnemies[i].transform.position - newPos).sqrMagnitude;
				if (AllEnemies[i] != OldChargingEnemy
					&& (OldBlinkingEnemy == null || AllEnemies[i] != OldBlinkingEnemy)
					&& !AllEnemies[i].IsDead && sqMagnitude < smallestDistance)
				{
					nearestEnemy = AllEnemies[i];
					smallestDistance = sqMagnitude;
				}
			}

			// Выключаем мерцание прошлого врага.
			if (OldBlinkingEnemy != null)
				OldBlinkingEnemy.RequestStopBlinking();

			if (nearestEnemy == ActiveChargingEnemy)
			{
				// Мы пришли к новому активному врагу - включаем эффект зарядки для него.
				ActiveChargingEnemy.RequestChargeUp();

				OldBlinkingEnemy = null;
				IsEnemyChangedLineAnimating = false;
			}
			else
			{
				// Включаем мерцание следующего врага.
				nearestEnemy.RequestBlink();
				OldBlinkingEnemy = nearestEnemy;

				// Считаем когда мы будет запускать поиск мерцающего врага в след. раз (как долго текущий враг будет мерцать).
				NextFindEnemyToBlinkTime = Time.timeSinceLevelLoad + EnemyBlinkInterval;
			}
		}
		#endregion


		#region Логика меню.
		/// <summary>Ставит игру на паузу.</summary>
		void PauseTheGame()
		{
			Menu.SetActive(true);
			Time.timeScale = 0;
			AudioListener.pause = true;
			Cursor.visible = true;
		}

		/// <summary>Снимает игру с паузы.</summary>
		public void UnpauseTheGame()
		{
			Menu.SetActive(false);
			Time.timeScale = 1;
			AudioListener.pause = false;
			Cursor.visible = false;
		}

		/// <summary>Загрузка главного меню.</summary>
		public void LoadMainMenu()
		{
			SceneManager.LoadScene("Main Menu");
			UnpauseTheGame();
		}
		#endregion

		#region Логика победы.
		/// <summary>Проигрывается ли в данный момент анимация победы.</summary>
		bool IsVictoryAnimationPlaying = false;
		/// <summary>Момент с точки загрузки уровня когда произошла победа.</summary>
		float VictoryTime;

		/// <summary>Кол-во убитых врагов.</summary>
		int EnemiesKilled = 0;

		/// <summary>Включает эпилог победы.</summary>
		void PlayVictoryEpilogue()
		{
			Victory.PlayEpilogue();
			enabled = false;
		}

		/// <summary>Запущена ли логика отступления врагов.</summary>
		bool EnemiesRetreated = false;

		/// <summary>Запускает логику отступления врагов.</summary>
		void StartEnemyRetreatLogic()
		{
			for (int i = 0; i < AllEnemies.Count; i++)
			{
				if (!AllEnemies[i].IsDead)
				{
					AllEnemies[i].enabled = false;
					AllEnemies[i].MoveRotate.enabled = true;
				}
			}

			EnemiesRetreated = true;
		}


		/// <summary>Позволяет поочередно сменивать мерцание врагов (вкл/выкл).</summary>
		bool RetreatBlinkPhaseIsEven = false;

		/// <summary>Анимация массового мерцания всех оставшихся врагов.</summary>
		void AnimateEnemyMassBlink()
		{
			// Выбираем врагов в шахматном порядке и заставляем их мерцать вкл/выкл.
			for (int i = 0; i < NumberOfEnemiesInARow; i++)
				for (int j = 0; j < NumberOfEnemiesInAColumn; j++)
				{
					if (!AllEnemies[i * NumberOfEnemiesInARow + j].IsDead)
					{
						bool setToBlink = true;
						if (j % 2 == 0)
							setToBlink = false;

						if (i % 2 == 0)
						{
							if (RetreatBlinkPhaseIsEven)
							{
								if (setToBlink)
									AllEnemies[i * NumberOfEnemiesInARow + j].RequestBlink();
								else
									AllEnemies[i * NumberOfEnemiesInARow + j].RequestStopBlinking();
							}
							else
							{
								if (setToBlink)
									AllEnemies[i * NumberOfEnemiesInARow + j].RequestStopBlinking();
								else
									AllEnemies[i * NumberOfEnemiesInARow + j].RequestBlink();
							}
						}
						else
						{
							if (RetreatBlinkPhaseIsEven)
							{
								if (setToBlink)
									AllEnemies[i * NumberOfEnemiesInARow + j].RequestStopBlinking();
								else
									AllEnemies[i * NumberOfEnemiesInARow + j].RequestBlink();
							}
							else
							{
								if (setToBlink)
									AllEnemies[i * NumberOfEnemiesInARow + j].RequestBlink();
								else
									AllEnemies[i * NumberOfEnemiesInARow + j].RequestStopBlinking();
							}
						}
					}
				}

			// Переключатель мерцания вкл/выкл
			RetreatBlinkPhaseIsEven = !RetreatBlinkPhaseIsEven;

			// Звук мерцания.
			RetreatSound.PlayOneShot(RetreatSound.clip);

			// Вычисляем время следующего переключения мерцания.
			NextFindEnemyToBlinkTime = Time.timeSinceLevelLoad + EnemyBlinkInterval;
		}
		#endregion


		#region Update.

		private void Update()
		{
			if (IsVictoryAnimationPlaying)
			{
				// Анимации/логики победы.
				if (Time.timeSinceLevelLoad - VictoryTime > 7)
					PlayVictoryEpilogue();
				else if (Time.timeSinceLevelLoad - VictoryTime > 4)
				{
					if (CicadasSound != null)
						CicadasSound.Play();

					if (!EnemiesRetreated)
						StartEnemyRetreatLogic();

					RetreatMixer.SetFloat("RetreatVolume", (1 - (Time.timeSinceLevelLoad - VictoryTime - 4) * 30));
				}
				else if (Time.timeSinceLevelLoad > NextFindEnemyToBlinkTime)
					AnimateEnemyMassBlink();
			}
			else
			{
				// Показываем врагов по запросу
				if (RequestShowEnemies)
				{
					if (AllEnemies[0].transform.parent != null)
						AllEnemies[0].transform.parent.gameObject.SetActive(true);

					RequestShowEnemies = false;
				}

				// Меню
				if (Input.GetKeyDown(KeyCode.Escape))
				{
					if (Menu.activeSelf)
						UnpauseTheGame();
					else
						PauseTheGame();
				}

				// Плавно затухаем черный квадрат после загрузки уровня.
				if (Black != null && Black.color.a > 0)
				{
					Black.color = new Color(0, 0, 0, 1 - ((Time.timeSinceLevelLoad - 0.5f) * .5f));
					if (Black.color.a <= 0)
						Black.gameObject.SetActive(false);
				}

				// Ищим нового врага.
				if (!IsEnemyActivationPaused)
				{
					if (ActiveChargingEnemy == null)
					{
						AcivateARandomEnemy();
						ActiveChargingEnemy.RequestChargeUp();
					}
					else if (ActiveChargingEnemy.IsDead)
					{
						EnemiesKilled++;

						// Условие победы.
						if (EnemiesKilled >= EnemyKillsTillVictory)
						{
							VictoryTime = Time.timeSinceLevelLoad;
							IsVictoryAnimationPlaying = true;
							FPSCameraController.IsPlayingVictoryAnimation = true;
						}
						else
						{
							// Ищем нового врага для активации.
							OldChargingEnemy = ActiveChargingEnemy;
							AcivateARandomEnemy();
							// Запускаем анимацию смены врага.
							IsEnemyChangedLineAnimating = true;
						}
					}
				}

				// Анимация линии смены врага.
				if (IsEnemyChangedLineAnimating && Time.timeSinceLevelLoad > NextFindEnemyToBlinkTime)
					EnemyChangedLineAnimationLogic();

			}
		}
		#endregion
	}
}
