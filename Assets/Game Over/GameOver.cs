using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mechanics
{
	/// <summary>Обрабатывает анимацию и логику Game Over.</summary>
	public class GameOver : MonoBehaviour
	{
		[Header("Убедитесь, что скрипт", order = 0)]
		[Header("выключен в редакторе!", order = 1)]

		[Tooltip("Куски мяса и оружие игрока.")]
		public GameObject Gibs;

		[Tooltip("Главная камера.")]
		public GameObject MainCamera;

		/// <summary>В какой момент активировать объект кусков мяса.</summary>
		float ShowGibsTime = float.MaxValue;
		/// <summary>В какой момент загружать главное меню.</summary>
		float LoadMainMenuTime = float.MaxValue;

		/// <summary>Анимация Game Over.</summary>
		public void PlayGameOverAnim()
		{
			// Включаем всех наследников - они необходимы для анимации Game Over.
			for (int i = 0; i < transform.childCount; i++)
				transform.GetChild(i).gameObject.SetActive(true);

			// Выключаем все GameObject-ы в корне сцены кроме камеры.
			var allGOs = SceneManager.GetActiveScene().GetRootGameObjects();

			for (int i = 0; i < allGOs.Length; i++)
			{
				if (allGOs[i] != gameObject && allGOs[i] != MainCamera)
					allGOs[i].SetActive(false);
			}

			// Поворачиваем камеру в ноль - чтобы куски мяса летели в нее из планеты а не из непонятно откудова.
			MainCamera.transform.rotation = Quaternion.identity;

			// Вычисляем когда показывать мясо.
			ShowGibsTime = Time.timeSinceLevelLoad + 2;

			// Вычисляем когда загружать главное меню.
			LoadMainMenuTime = Time.timeSinceLevelLoad + 6.5f;
			enabled = true;
		}

		// Чтобы можно было деактивировать скрипт в редакторе.
		private void Start() { }



		private void Update()
		{
			if (Time.timeSinceLevelLoad > ShowGibsTime)
				Gibs.SetActive(true);

			if (Time.timeSinceLevelLoad > LoadMainMenuTime)
				SceneManager.LoadScene("Main Menu");
		}
	}
}
