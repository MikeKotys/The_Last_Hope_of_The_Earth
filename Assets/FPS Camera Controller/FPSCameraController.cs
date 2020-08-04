using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;
using Enemies;


namespace Mechanics
{
	/// <summary>Контроллирует пулемет а так же камеру игрока. Создает эффект запаздывания камеры за движением пушки.
	/// Также проигрывает различные анимации камеры для интро/победы.</summary>
	public class FPSCameraController : MonoBehaviour
	{
		#region Параметры
		[Header("Параметры камеры.")]
		[Tooltip("Скорость вращения камеры.")]
		public Camera Camera;
		[Tooltip("Объект, который привязан к пулемету и перемещается вместе с ним. Камера будет стремиться постепенно сравняться с этим объектом" +
			" по параметрам перемещения и вращения.")]
		public Transform CameraTarget;

		[Tooltip("Чувствительность мыши.")]
		public float MouseSensitivity = 1;
		[Tooltip("Как быстро камера следует за позицией CameraTarget.")]
		[Range(0, 1)]
		public float LerpPosSpeed = 0.75f;
		[Tooltip("Как быстро камера следует за вращением CameraTarget.")]
		[Range(0, 1)]
		public float LerpAngleSpeed = 0.5f;

		[Tooltip("Ограничение в градусах движения мыши по горизонтали (x должен быть меньше нуля, y - больше нуля).")]
		public Vector2 ClampXInDegrees = new Vector2(360, 180);
		[Tooltip("Ограничение в градусах движения мыши по горизонтали (x должен быть меньше нуля, y - больше нуля).")]
		public Vector2 ClampYInDegrees = new Vector2(360, 180);
		[Tooltip("Величина сглаживания мыши по вертикали и горизонтали.")]
		public Vector2 Smoothing = new Vector2(30, 30);

		[Header("Параметры оружия"), Space(30)]
		[Tooltip("Как много выстрелов должен делать пулемет в минуту.")]
		public float RoundsPerMinute = 650;
		/// <summary>Сколько времени должно пройти между выстрелами.</summary>
		float FireCooldown = 0;
		/// <summary>Время следующего выстрела.</summary>
		float NextShotTime = 0;
		/// <summary>Маска слоев с допустимыми для попаданияцелями.</summary>
		LayerMask AllowedTargetsLayer;
		[Tooltip("Сила бросков камеры по вертикали при стрельбе.")]
		public float CameraXJump = 1.5f;
		[Tooltip("Сила бросков камеры по горизонтали при стрельбе (бросок влево).")]
		public float CameraYJumpMin = -1.3f;
		[Tooltip("Сила бросков камеры по горизонтали при стрельбе (бросок вправо).")]
		public float CameraYJumpMax = .75f;
		[Tooltip("Позволяет настроить точность мушки по вертикали.")]
		public float IronsightYOffset = -.01f;
		[Tooltip("Сила с коротой камеру отбрасывает назад при каждом выстреле.")]
		public float CameraRecoilPower = 1;



		[Header("Эффекты стрельбы"), Space(30)]
		[Tooltip("Вспышки.")]
		public ParticleSystem Muzzleflash;
		[Tooltip("Дым.")]
		public ParticleSystem Smoke;
		[Tooltip("Скорость перемещения дыма, появляющегося при выстреле.")]
		public float SmokeSpeed = 10;
		[Tooltip("Пустые гильзы.")]
		public ParticleSystem Shells;
		[Tooltip("Звенья ленты патронов.")]
		public ParticleSystem Links;
		[Tooltip("Точка и направление выброса гильз и звеньев.")]
		public Transform ShellEjectPos;
		[Tooltip("Лента с боевыми патронами (для анимации подачи патронов в ствол).")]
		public Transform BulletBelt;
		[Tooltip("Сила с которой смещается лента с боевыми патронами при выстреле.")]
		public float BulletBeltShiftPower = .1f;
		/// <summary>Изначальная позиция ленты - чтобы можно было ее вернуть на исходную после эффекта подачи патронов.</summary>
		Vector3 BulletBeltOrigPos;
		[Tooltip("Шанс появления вспышки в процентах.")]
		public int MuzzleflashChance = 20;
		[Tooltip("Материал раскаленного ствола.")]
		public Material HotBarrelMat;
		[Tooltip("Материал искажения картинки от горячего воздуха исходящего со ствола.")]
		public Material BarrelDistortionMat;
		[Tooltip("Свет, иногда появляющийся при стрельбе.")]
		public Light MuzzleflashLight;
		[Tooltip("Шанс появления света при каждом выстреле.")]
		public int LightFlashChance = 20;
		[Tooltip("Максимальная яркость света при выстреле.")]
		public float LightMaxIntensity = 2;
		[Tooltip("Скорость затухания света при выстреле.")]
		public float LightFadeSpeed = 12;
		[Tooltip("Звуки стрельбы.")]
		public List<AudioSource> FireSounds;
		[Tooltip("Звуки взрыва врагов.")]
		public List<AudioSource> EnemyExplosionSounds;
		[Tooltip("Звук цикад.")]
		public AudioSource CicadasSound;

		[Header("Эффекты попадания по врагу"), Space(30)]
		[Tooltip("Огненный след 1.")]
		public ParticleSystem Trails1;
		[Tooltip("Огненный след 2.")]
		public ParticleSystem Trails2;
		[Tooltip("Искры.")]
		public ParticleSystem Sparks;
		[Tooltip("Черные куски.")]
		public ParticleSystem Particles;

		[Header("Параметры анимации победы"), Space(30)]
		[Tooltip("Скорость, с которой пулемет будет опускаться вниз при победе.")]
		public float GunLowerSpeed = 2;
		[Tooltip("Точка и направление, к которым будет стремиться камера при победе.")]
		public Transform VictoryCameraPos;

		/// <summary>Проигрывает анимацию победы.</summary>
		[HideInInspector][NonSerialized]
		public bool IsPlayingVictoryAnimation = false;

		/// <summary>Игнорирует весь ввод.</summary>
		[HideInInspector][NonSerialized]
		public bool IsIgnoringInput = true;
		#endregion


		#region Старт
		void Awake()
		{
			CameraTarget.parent = null;
			Camera.transform.parent = null;
			CameraTarget.position = Camera.transform.position;
			CameraTarget.rotation = Camera.transform.rotation;
			CameraTarget.parent = transform;
			AllowedTargetsLayer = LayerMask.GetMask("Enemies");

			FireCooldown = 60.0f / RoundsPerMinute;

			if (HotBarrelMat != null)
				HotBarrelMat.SetColor("_EmissionColor", Color.black);

			if (BarrelDistortionMat != null)
				BarrelDistortionMat.SetFloat("_BumpAmt", 0);

			MuzzleflashLight.gameObject.SetActive(false);

#if UNITY_EDITOR
			if (Application.isPlaying)
			{
				Cursor.lockState = CursorLockMode.Confined;
				Cursor.visible = false;
			}
#endif

			if (BulletBelt != null)
				BulletBeltOrigPos = BulletBelt.localPosition;
		}
		#endregion

		#region Логика вращения пулемета и следования камеры за ним.
		/// <summary>Накопитель сглаженых значений ввода мыши.</summary>
		Vector2 SmoothedMouseValues;

		/// <summary>Логика вращения камеры/ствола.</summary>
		void GunAimingLogic()
		{
			// Читаем и сглаживаем ввод мыши.
			var mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

			mouseDelta = Vector2.Scale(mouseDelta, new Vector2(Smoothing.x, Smoothing.y));
			if (UI.MainMenuController.InvertMouseY)
				mouseDelta.y *= -1;

			SmoothedMouseValues.x = Mathf.Lerp(SmoothedMouseValues.x, mouseDelta.x, 1f / Smoothing.x);
			SmoothedMouseValues.y = Mathf.Lerp(SmoothedMouseValues.y, mouseDelta.y, 1f / Smoothing.y);

			// Ограничиваем вращение мыши.
			float newXAngle = transform.rotation.eulerAngles.x + SmoothedMouseValues.y * MouseSensitivity * Time.deltaTime;
			if (newXAngle <= 0)
				newXAngle = Mathf.Clamp(360 + newXAngle, 360 + ClampXInDegrees.x, 360);
			else
			{
				if (newXAngle > 180)
					newXAngle = Mathf.Clamp(newXAngle, 360 + ClampXInDegrees.x, 360);
				else
					newXAngle = Mathf.Clamp(newXAngle, 0, ClampXInDegrees.y);
			}

			float newYAngle = transform.rotation.eulerAngles.y + SmoothedMouseValues.x * MouseSensitivity * Time.deltaTime;
			if (newYAngle <= 0)
				newYAngle = Mathf.Clamp(360 + newYAngle, 360 + ClampYInDegrees.x, 360);
			else
			{
				if (newYAngle > 180)
					newYAngle = Mathf.Clamp(newYAngle, 360 + ClampYInDegrees.x, 360);
				else
					newYAngle = Mathf.Clamp(newYAngle, 0, ClampYInDegrees.y);
			}


			// Вращаем ствол.
			transform.eulerAngles = new Vector3(newXAngle, newYAngle, transform.rotation.eulerAngles.z);


			// Перемещаем/вращаем камеру за целью, прикрепленной к стволу с отставанием.
			Vector3 newPos = Vector3.Lerp(Camera.transform.position, CameraTarget.position, LerpPosSpeed * Time.deltaTime * 60);
			Camera.transform.position = Vector3.Lerp(Camera.transform.position, newPos, LerpPosSpeed * Time.deltaTime * 60);

			Quaternion newRotation = Quaternion.Lerp(Camera.transform.rotation, CameraTarget.rotation, LerpAngleSpeed * Time.deltaTime * 60);
			Camera.transform.rotation = Quaternion.Lerp(Camera.transform.rotation, newRotation, LerpAngleSpeed * Time.deltaTime * 60);
		}
		#endregion


		#region Логика стрельбы.
		/// <summary>Кол-во выстрелов всего (для эффекта красного дула и миража).</summary>
		float TotalShotsFired = 0;

		/// <summary>Последний звук выстрела (для предотвращения повторения случайных звуков стрельбы).</summary>
		int LastFireSoundNum = 0;
		/// <summary>Последний звук взрыва (для предотвращения повторения случайных звуков взрывов).</summary>
		int LastEnemyExplosionSoundNum = 0;

		/// <summary>Логика стрельбы.</summary>
		void Shoot()
		{
			// Не позволяем стрелять чаще, чем указанный RPM.
			if (Time.timeSinceLevelLoad > NextShotTime)
			{
				// Выключаем звук цикад.
				if (CicadasSound != null)
					CicadasSound.Stop();

				// Проигрываем звуки стрельбы в случайном порядке но без повторений.
				if (FireSounds != null && FireSounds.Count > 0)
				{
					// Гарантируем, что случайные звуки стрельбы не будут повторятся.
					int fireSoundNum = 0;
					do
						fireSoundNum = UnityEngine.Random.Range(0, FireSounds.Count);
					while (fireSoundNum == LastFireSoundNum);
					LastFireSoundNum = fireSoundNum;

					FireSounds[fireSoundNum].PlayOneShot(FireSounds[fireSoundNum].clip);
				}

				// Анимируем ленту с патронами для создания эффекта подачи патронов в ствол.
				if (BulletBelt != null && FramesToBringBulletsBack <= 0)
				{
					BulletBelt.localPosition += BulletBelt.up * BulletBeltShiftPower;
					FramesToBringBulletsBack = 13;
				}

				// Логика мгновенных hitscan попаданий.
				RaycastHit hitResult;
				if (Physics.Raycast(transform.position + transform.up * IronsightYOffset, transform.forward,
					out hitResult, 1000, AllowedTargetsLayer))
				{
					var enemyCollider = hitResult.collider.GetComponent<EnemyCollision>();
					if (enemyCollider != null)
					{
						// Enemy.DamageLogic() вернет true если противник уничтожен.
						if (enemyCollider.Enemy.DamageLogic(hitResult, enemyCollider))
						{
							// Проигрываем звук взрыва и гарантируем, что случайные звуки взрывов не будут повторятся.
							int explosionSoundNum = 0;
							do
								explosionSoundNum = UnityEngine.Random.Range(0, FireSounds.Count);
							while (explosionSoundNum == LastEnemyExplosionSoundNum);
							LastEnemyExplosionSoundNum = explosionSoundNum;

							EnemyExplosionSounds[explosionSoundNum].PlayOneShot(EnemyExplosionSounds[explosionSoundNum].clip);
						}
						else if (!enemyCollider.Enemy.IsShielded)
						{
							// Проигрываем эффект попадания.
							EmitParams hitOptions = new EmitParams();
							hitOptions.position = hitResult.point;
							hitOptions.velocity = new Vector3(UnityEngine.Random.Range(200, 1000) * .004f,
								UnityEngine.Random.Range(200, 1000) * .004f, UnityEngine.Random.Range(200, 1000) * .004f);
							Trails1.Emit(hitOptions, 5);
							Trails2.Emit(hitOptions, 5);
							Sparks.Emit(hitOptions, 5);
							Particles.Emit(hitOptions, 10);
						}
					}
				}

				// Вспышки.
				if (UnityEngine.Random.Range(0, 10000) * .01f < MuzzleflashChance)
					Muzzleflash.Emit(1);

				// Свет.
				if (UnityEngine.Random.Range(0, 10000) * .01f < LightFlashChance)
				{
					MuzzleflashLight.gameObject.SetActive(true);
					MuzzleflashLight.intensity = LightMaxIntensity;
				}

				// Дым.
				EmitParams options = new EmitParams();
				options.position = transform.position;
				options.velocity = transform.forward * SmokeSpeed;

				Smoke.Emit(options, 1);

				// Эффект красного ствола и миража.
				TotalShotsFired++;
				if (HotBarrelMat != null)
					HotBarrelMat.SetColor("_EmissionColor", Color.Lerp(Color.black, Color.white, TotalShotsFired * .0003f));
				if (BarrelDistortionMat != null)
					BarrelDistortionMat.SetFloat("_BumpAmt", Mathf.Lerp(0, 200, TotalShotsFired * .005f));

				float randomJumpY = UnityEngine.Random.Range((int)(CameraYJumpMin * 1000), (int)(CameraYJumpMax * 1000)) * .001f;

				// Эффект подбрасывания ствола от выстрела.
				transform.localEulerAngles = new Vector3(transform.localEulerAngles.x + CameraXJump,
					transform.localEulerAngles.y + randomJumpY, transform.localEulerAngles.z);

				// Эффект отбрасывания камеры назад от выстрела.
				Camera.transform.position -= transform.forward * CameraRecoilPower;

				// Гильзы/звенья ленты.
				if (ShellEjectPos != null)
				{
					if (Shells != null)
					{
						options = new EmitParams();
						options.position = ShellEjectPos.position;
						options.velocity = transform.right * .8f;
						Shells.Emit(options, 1);
					}
					if (Links != null)
					{
						options = new EmitParams();
						options.position = ShellEjectPos.position;
						options.velocity = Vector3.Lerp(transform.right, transform.up, 0.3f) * .65f;
						Links.Emit(options, 1);
					}
				}

				// Рассчитываем время следующего выстрела.
				NextShotTime = Time.timeSinceLevelLoad + FireCooldown;
			}
		}
		#endregion


		#region Update
		/// <summary>Через сколько кадров возвращать назад ленту с патронами.</summary>
		int FramesToBringBulletsBack = 0;


		void Update()
		{
			if (IsPlayingVictoryAnimation)
			{
				// Предотвращаем эффекты стрельбы
				MuzzleflashLight.gameObject.SetActive(false);

				// Плавно перемещаем камеру в нужную точку.
				Camera.transform.position = Vector3.Lerp(Camera.transform.position, VictoryCameraPos.position, 0.5f * Time.deltaTime);
				Camera.transform.rotation = Quaternion.Lerp(Camera.transform.rotation, VictoryCameraPos.rotation, 0.5f * Time.deltaTime);

				// Плавно опускаем ствол.
				if (transform.localEulerAngles.x > 320 || transform.localEulerAngles.x < 0)
					transform.localEulerAngles = new Vector3(transform.localEulerAngles.x - Time.deltaTime * GunLowerSpeed,
						transform.localEulerAngles.y, transform.localEulerAngles.z);
			}
			else
			{
				if (!IsIgnoringInput)
				{
					GunAimingLogic();
					if (Input.GetMouseButton(0))
						Shoot();
				}

				// Затухаем свет стрельбы.
				if (MuzzleflashLight.gameObject.activeSelf)
				{
					MuzzleflashLight.intensity -= Time.deltaTime * LightFadeSpeed;
					if (MuzzleflashLight.intensity <= 0)
						MuzzleflashLight.gameObject.SetActive(false);
				}

				// Возвращаем назад ленту с патронами.
				if (FramesToBringBulletsBack > 0)
				{
					FramesToBringBulletsBack--;

					if (FramesToBringBulletsBack <= 0)
						BulletBelt.localPosition = BulletBeltOrigPos;
				}
			}
		}
		#endregion
	}
}
