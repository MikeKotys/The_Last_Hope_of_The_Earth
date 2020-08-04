using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mechanics;

namespace Intro
{
	/// <summary>Управляет анимациями интро ролика главного уровня.</summary>
	public class IntroAnimManager : MonoBehaviour
	{
		[Tooltip("GameController для передачи событий анимации.")]
		public GameController GameController;

		[Tooltip("FPSCameraController для разблокировки ввода по окончанию интро анимации.")]
		public FPSCameraController FPSCameraController;

		[Tooltip("Главная камера для ее анимации.")]
		public Camera MainCamera;

		[Tooltip("С какой скоростью камера камера будет двигаться к пулемету ближе к концу интро анимации.")]
		[Range(0.005f, 1)]
		public float CameraLerpSpeed = 0.4f;
		/// <summary>Начальная позиция камеры.</summary>
		Vector3 OriginalCameraPos;
		/// <summary>Начальное вращение камеры.</summary>
		Quaternion OriginalCameraRot;
		/// <summary>Начальн угол обзора камеры.</summary>
		float OriginalCameraFOV;

		[Tooltip("Анимируемый объект головы солдата - к нему будет временно прикреплена камера.")]
		public Transform SoldierHead;

		[Tooltip("Пулемет будет анимироваться в интро.")]
		public GameObject M60;

		[Tooltip("Вращение по X, которое будет задано для пулемета в начале интро ролика.")]
		public float M60LoweredXAngle = -40;
		float M60OriginalXAngle;

		[Tooltip("По истечению этого времени анимация рук с флягой стартует.")]
		public float IntroAnimDelay= 2.5f;

		[Tooltip("По истечению этого времени игроку передается контроль над пулеметом.")]
		public float PlayerControllDelay = 10;

		[Tooltip("Звук, который будет проигран, когда игрок глотнет из фляги в первый раз.")]
		public AudioSource Sip1Sound;

		[Tooltip("Звук, который будет проигран, когда игрок глотнет из фляги во второй раз.")]
		public AudioSource Sip2Sound;

		[Tooltip("Звук, который будет проигран, когда игрок впервые увидит врагов.")]
		public AudioSource JesusSound;

		[Tooltip("Звук, который будет проигран, когда игрок схватит M60.")]
		public AudioSource GrabM60Sound;
		/// <summary>Аниматор будет использован для запуска анимации рук с флягой.</summary>
		Animator Animator;

		/// <summary>Имя параметра в аниматоре, который управляет скоростью анимации.</summary>
		const string A_AnimSpeed = "AnimSpeed";


		void Start()
		{
			// Все должно вызываться именно в Start()!

			if (M60 != null)
			{
				M60OriginalXAngle = M60.transform.localEulerAngles.x;

				M60.transform.localEulerAngles = new Vector3(M60LoweredXAngle,
					M60.transform.localEulerAngles.y, M60.transform.localEulerAngles.z);
			}

			// Выставляем пулемет и камеру в исходные позиции.
			OriginalCameraPos = MainCamera.transform.localPosition;
			OriginalCameraRot = MainCamera.transform.localRotation;
			OriginalCameraFOV = MainCamera.fieldOfView;


			MainCamera.transform.parent = SoldierHead;
			MainCamera.transform.localPosition = Vector3.zero;
			MainCamera.transform.localRotation = Quaternion.identity;
			MainCamera.fieldOfView = 26;

			Animator = GetComponent<Animator>();
			Animator.SetFloat(A_AnimSpeed, .001f);
		}

		/// <summary>Событие анимации. В этот момент пора показывать врагов.</summary>
		void AE_ShowEnemies()
		{
			GameController.RequestShowEnemies = true;
		}

		/// <summary>Событие анимации. В этот момент нужно проиграть звук 'Jesus'.</summary>
		void AE_PlayJesusSound()
		{
			if (JesusSound != null)
				JesusSound.Play();
		}

		/// <summary>Событие анимации. В этот момент пора отсоеденять камеру от анимируемых рук и цеплять ее к М60.</summary>
		void AE_DetachCamera()
		{
			MainCamera.transform.parent = null;
			IsAttachingCameraToM60 = true;
			if (GrabM60Sound != null && !GrabM60Sound.isPlaying)
				GrabM60Sound.Play();
		}

		/// <summary>Событие анимации. В этот момент нужно проиграть звук глотания воды #1.</summary>
		void AE_Sip1()
		{
			if (Sip1Sound != null)
				Sip1Sound.Play();
		}

		/// <summary>Событие анимации. В этот момент пора проиграть звук глотания воды #2.</summary>
		void AE_Sip2()
		{
			if (Sip2Sound != null)
				Sip2Sound.Play();
		}

		

		/// <summary>Проигрывает анимацию присоеденения камеры к пулемету.</summary>
		bool IsAttachingCameraToM60 = false;

		/// <summary>Была ли уже запущена анимация рук с флягой?</summary>
		bool IsIntroAnimPlaying = false;

		private void Update()
		{
			if (!IsIntroAnimPlaying && Time.timeSinceLevelLoad > IntroAnimDelay)
			{
				Animator.SetFloat(A_AnimSpeed, .65f);
				IsIntroAnimPlaying = true;
			}

			if (IsAttachingCameraToM60)
			{
				MainCamera.transform.localPosition = Vector3.Lerp(MainCamera.transform.localPosition, OriginalCameraPos,
					CameraLerpSpeed * Time.deltaTime * 60);
				MainCamera.transform.localRotation = Quaternion.Lerp(MainCamera.transform.localRotation, OriginalCameraRot,
					CameraLerpSpeed * Time.deltaTime * 60);
				MainCamera.fieldOfView = Mathf.Lerp(MainCamera.fieldOfView, OriginalCameraFOV,
					CameraLerpSpeed * Time.deltaTime * 60);

				float newM60XAngle = Mathf.Lerp(M60.transform.localEulerAngles.x, M60OriginalXAngle,
					CameraLerpSpeed * Time.deltaTime * 60);

				M60.transform.localEulerAngles = new Vector3(newM60XAngle,
					M60.transform.localEulerAngles.y, M60.transform.localEulerAngles.z);
			}

			if (Time.timeSinceLevelLoad > PlayerControllDelay)
			{
				M60.transform.localEulerAngles = new Vector3(M60OriginalXAngle,
					M60.transform.localEulerAngles.y, M60.transform.localEulerAngles.z);

				MainCamera.transform.localPosition = OriginalCameraPos;
				MainCamera.transform.localRotation = OriginalCameraRot;
				MainCamera.fieldOfView = OriginalCameraFOV;
				FPSCameraController.IsIgnoringInput = false;
				GameController.IsEnemyActivationPaused = false;
				enabled = false;
				Destroy(gameObject);
			}
		}
	}
}
