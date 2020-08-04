using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mechanics
{
	/// <summary></summary>
	public class Victory : MonoBehaviour
	{
		[Header("Убедитесь, что скрипт", order = 0)]
		[Header("выключен в редакторе!", order = 1)]

		[Tooltip("Ссылка на объект-руку, которая при включении будет отдавать честь (при победе).")]
		public GameObject SaluteHand;

		[Tooltip("Ссылка на объект с файлами победы.")]
		public GameObject Container;
		[Tooltip("Музыка победы.")]
		public GameObject VictoryTheme;
		[Tooltip("Звук цикад.")]
		public AudioSource CicadasSound;

		[Tooltip("Этот черный квадрат плавно появится перед загрузкой уровня.")]
		public Image Black;

		/// <summary>В какой момент загружать главное меню.</summary>
		float LoadMainMenuTime = float.MaxValue;

		public void PlayEpilogue()
		{
			// Активируем объекты победы.
			Container.SetActive(true);

			// Запускаем анимацию руки, отдающей честь.
			SaluteHand.SetActive(true);

			VictoryTheme.transform.parent = null;

			// Гарантируем, что музыка будет играть после загрузки главного меню.
			DontDestroyOnLoad(VictoryTheme);

			LoadMainMenuTime = Time.timeSinceLevelLoad + 36;
			enabled = true;
		}

		private void Update()
		{
			// Плавно показываем черный квадрат за 2 секунды до победы.
			if (Time.timeSinceLevelLoad > LoadMainMenuTime - 2)
			{
				if (Black.color.a <= 0)
					Black.gameObject.SetActive(true);

				if (CicadasSound != null)
					CicadasSound.volume -= Time.deltaTime * .5f;

				Black.color = new Color(0, 0, 0, Time.timeSinceLevelLoad - LoadMainMenuTime + 2);
			}

			if (Time.timeSinceLevelLoad > LoadMainMenuTime)
				SceneManager.LoadScene("Main Menu");
		}

		public void Start() { }
	}
}
