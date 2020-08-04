using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Enemies
{
	/// <summary>Клас, обрабатывающий логику врага.</summary>
	public class Enemy : MonoBehaviour
	{
		#region Параметры
		[Header("Попадания и смерть")]
		[Header("Текущее здоровье.")]
		public float CurrentHP = 100;
		[Header("Урон от попадания по телу.")]
		public float HullHitDamage = 10;
		[Header("Урон от попадания по 'глазам'.")]
		public float CriticalHitDamage = 35;
		[Header("Объект со всеми сетками моделей врага (будет выключен при смерти).")]
		public GameObject Model;
		[Header("Сферы с эффектами силового щита.")]
		public List<MeshRenderer> ForceFieldEffects;
		[Header("Эффект взрыва.")]
		public GameObject Explosion;
		[Header("Как далеко может отлететь враг от точки спавна при попадании в него.")]
		public float MaxPosImpulse;
		[Header("Как сильно может развернуться враг при попадании в него.")]
		public float MaxRotImpulse;
		[Header("Как далеко может отлететь враг за фрейм.")]
		public float MaxFramePosImpulse;
		[Header("Как сильно может развернуться враг за фрейм.")]
		public float MaxFrameRotImpulse;
		[Header("Время, которое должно пройти до того, как врага снова можно будет развернуть/сместить попаданием.")]
		public float ImpulseInterval = 0.3f;
		[Range(0.01f, 1)]
		[Tooltip("Как быстро корабль возвращается к исходной точке после попадания по нему.")]
		public float StabilizationSpeed = 0.1f;


		[Header("Эффект активации."), Space(30)]
		[Header("Объект 'глаз'. Ему будут меняться материалы на материалы с другим цветом.")]
		public MeshRenderer Eyes;
		[Header("Эффекты зарева от 'глаз'.")]
		public List<LensFlare> LaserFlares;
		[Header("Материал активного врага для 'глаз'.")]
		public Material ChargedMaterial;
		[Header("Осветители. Их цвет и интенсивность будут меняться для активных врагов.")]
		public List<Light> Lights;


		[Header("Game Over"), Space(30)]
		[Header("Цилиндры с эффектами лазеров.")]
		public List<GameObject> Lasers;
		[Header("Время с момента активации врага до того, как враг начнет разогревать лазеры.")]
		public float TimeUntilLasers = 5;
		[Header("Время с момента активации врага до того, как наступит конец игры.")]
		public float TimeUntilGameOver = 7.5f;
		[Header("Звук лазеров.")]
		public AudioSource LaserSound;
		[Header("Звук активного врага.")]
		public AudioSource ChargeUpSound;


		[Header("Отступление"), Space(30)]
		[Header("Скрипт, который позволит врагу отступить в момент победы.")]
		public Utils.MoveRotate MoveRotate;

		float NextImpulseInput = 0;
		Vector3 OriginalPos;
		Quaternion OriginaLocalRot;

		[HideInInspector][NonSerialized]
		public bool IsShielded = true;
		[HideInInspector][NonSerialized]
		public Material OriginalLaserMaterial;

		[HideInInspector][NonSerialized]
		public bool IsDead = false;

		float LasersOriginalXZScale = 1;
		#endregion

		#region Старт
		private void Start()
		{
			// Важно вызывать старт для врагов а не Awake, ибо в Awake GameController-а враги выставляются на позиции.
			OriginalPos = transform.position;
			OriginaLocalRot = transform.localRotation;

			OriginalLaserMaterial = Eyes.sharedMaterial;

			if (Lasers != null && Lasers.Count > 0)
				LasersOriginalXZScale = Lasers[0].transform.localScale.x;
		}
		#endregion

		#region Обработка попаданий.
		/// <summary>Остаточное отложенное смещение.</summary>
		Vector3 ImpulsePos;
		/// <summary>Остаточное отложенное вращение.</summary>
		Vector3 ImpulseRot;

		/// <summary>Не позволяет смещать врага.</summary>
		bool PreventImpulses = false;

		/// <summary>Запрос на отложенное плавное смещение и вращение корабля врага.</summary>
		/// <param name="shift">Величина смещения.</param>
		/// <param name="rot">Величина вращения.</param>
		public void Impulse(Vector3 shift, Vector3 rot)
		{
			if (!PreventImpulses && Time.timeSinceLevelLoad > NextImpulseInput)
			{
				ImpulsePos = shift;
				ImpulseRot = rot;

				NextImpulseInput = Time.timeSinceLevelLoad + ImpulseInterval;
			}
		}

		/// <summary>Логика попадания во врага.</summary>
		/// <param name="enemyCollision">В какой коллайдер попали.</param>
		/// <returns>Возвращает жив ли враг после попадания.</returns>
		public bool DamageLogic(RaycastHit hitResult, EnemyCollision enemyCollision)
		{
			if (IsShielded)
			{
				// Враг в режиме щита - активируем одну из сфер эффекта щита.
				if (ForceFieldEffects != null && ForceFieldEffects.Count > 0)
				{
					for (int i = 0; i < ForceFieldEffects.Count; i++)
					{
						// Находим незанятый эффект
						if (!ForceFieldEffects[i].gameObject.activeSelf)
						{
							ForceFieldEffects[i].gameObject.SetActive(true);
							ForceFieldEffects[i].transform.localScale = Vector3.one * .2f;
							ForceFieldEffects[i].transform.position = hitResult.point;
							break;
						}
					}
				}
			}
			else
			{
				// Логика критического попадания
				if (enemyCollision.IsACriticalHit)
					CurrentHP -= CriticalHitDamage;
				else
					CurrentHP -= HullHitDamage;

				// Смерть
				if (CurrentHP <= 0)
				{
					Model.SetActive(false);
					Explosion.SetActive(true);

					IsDead = true;
					enabled = false;
					Impulse(Vector3.zero, Vector3.zero);
				}
				else
				{
					// Смещаем врага в зависимости от того, куда ему попали.
					Vector3 impulse = transform.right * -1;
					float impulseRotYAxis = -1;
					float impulseRotXAxis = 0;

					if (enemyCollision.IsALeftCollider)
					{
						impulse *= -1;
						impulseRotYAxis = 1;
					}

					if (enemyCollision.IsABottomCollider)
					{
						impulseRotXAxis = -.35f;
						impulse = Vector3.Lerp(impulse, -transform.up, 0.08f);
					}
					else
					{
						if (UnityEngine.Random.Range(0, 1) == 0)
							impulse = Vector3.Lerp(impulse, transform.up, UnityEngine.Random.Range(0, 100) * .004f);
						else
							impulse = Vector3.Lerp(impulse, -transform.up, UnityEngine.Random.Range(0, 100) * .004f);
					}


					Impulse(impulse * MaxPosImpulse,
						new Vector3(impulseRotXAxis, impulseRotYAxis, 0) * MaxRotImpulse);
				}
			}

			return IsDead;
		}


		/// <summary>Эффект силового щита.</summary>
		void AnimateForceFieldHit()
		{
			// Если хотябы одна из сфер эффекта активна - проигрываем эффект (скаллируем сферу и плавно затухаем цвет материала).
			if (ForceFieldEffects != null && ForceFieldEffects.Count > 0)
			{
				for (int i = 0; i < ForceFieldEffects.Count; i++)
				{
					if (ForceFieldEffects[i].gameObject.activeSelf)
					{
						if (ForceFieldEffects[i].transform.localScale.sqrMagnitude < 7)
						{
							ForceFieldEffects[i].transform.localScale *= 1 + Time.deltaTime * 2.8f;
							var alpha = 1 - ForceFieldEffects[i].transform.localScale.sqrMagnitude / 7.0f;
							alpha *= alpha;
							alpha *= alpha;
							alpha *= alpha;
							ForceFieldEffects[i].material.SetColor("_MainColor", new Color(0.3f, 1, 1, alpha));
						}
						else
							ForceFieldEffects[i].gameObject.SetActive(false);
					}
				}
			}
		}
		#endregion


		#region Обработка мерцания и активации
		/// <summary>Анимация мерцания запрошена.</summary>
		bool IsBlinkRequested = false;

		/// <summary>Запрос анимации мерцания.</summary>
		public void RequestBlink()
		{
			IsBlinkRequested = true;
		}

		/// <summary>Запрошено выключение анимации мерцания.</summary>
		bool IsStopBlinkingRequested = false;
		/// <summary>Запрос выключения анимации мерцания.</summary>
		public void RequestStopBlinking()
		{
			IsStopBlinkingRequested = true;
		}


		/// <summary>Активация врага запрошена.</summary>
		bool IsChargeRequested = false;

		/// <summary>Запрос активации врага.</summary>
		public void RequestChargeUp()
		{
			if (IsShielded)
				IsChargeRequested = true;
		}

		/// <summary>Если время с начала уровня перевалит за эту отметку враг начнет стрелять лазерами.</summary>
		float ShootLasersTime;

		/// <summary>Если время с начала уровня перевалит за эту отметку и враг при этом не убит - Game Over.</summary>
		float GameOverTime;

		/// <summary>Зарево лазеров сейчас нарастает.</summary>
		bool AreLaserFlaresFadingIn = true;
		#endregion

		#region Update
		private void Update()
		{
			// Обработка смещения врага в результате попаданий.
			if (ImpulsePos.sqrMagnitude > 0)
			{
				var delta = ImpulsePos * Time.deltaTime * 60;
				if (delta.sqrMagnitude > MaxFramePosImpulse * Time.deltaTime * 60)
					delta = delta.normalized * MaxFramePosImpulse * Time.deltaTime * 60;
				transform.position += delta;
				ImpulsePos -= delta;
			}

			// Обработка вращения врага в результате попаданий.
			if (ImpulseRot.sqrMagnitude > 0)
			{
				var delta = ImpulseRot * Time.deltaTime * 60;
				if (delta.sqrMagnitude > MaxFrameRotImpulse * Time.deltaTime * 60)
					delta = delta.normalized * MaxFrameRotImpulse * Time.deltaTime * 60;
				transform.localEulerAngles += delta;
				ImpulseRot -= delta;
			}

			// Анимация попадания в силовой щит.
			AnimateForceFieldHit();

			// Обработка стабилизации врага (возврата на исходную) после смещений/вращений в результате попаданий.
			transform.position = Vector3.Lerp(transform.position, OriginalPos, StabilizationSpeed * Time.deltaTime * 60);
			transform.localRotation = Quaternion.Lerp(transform.localRotation, OriginaLocalRot, StabilizationSpeed * Time.deltaTime * 60);

			// Логика активации врага.
			if (IsChargeRequested)
			{
				// Меняем материал 'глаз' на оранжевый.
				Eyes.sharedMaterial = ChargedMaterial;

				// Делаем свет белым и менее ярким.
				if (Lights != null && Lights.Count > 0)
					for (int i = 0; i < Lights.Count; i++)
					{
						Lights[i].intensity = 12;
						Lights[i].color = Color.white;
					}

				// Включаем зарева от лазеров.
				if (LaserFlares != null && LaserFlares.Count > 0)
					for (int i = 0; i < LaserFlares.Count; i++)
						LaserFlares[i].gameObject.SetActive(true);

				// Выключаем щиты.
				IsShielded = false;

				// Вычисляем когда надо стрелять лазерами.
				ShootLasersTime = Time.timeSinceLevelLoad + TimeUntilLasers;
				// Вычисляем когда надо заканчивать игру провалом.
				GameOverTime = Time.timeSinceLevelLoad + TimeUntilGameOver;

				// Включаем звук зарядки.
				if (ChargeUpSound != null)
					ChargeUpSound.Play();

				IsChargeRequested = false;
			}

			// Логика включения мерцания.
			if (IsBlinkRequested)
			{
				// Меняем материал 'глаз' на оранжевый.
				Eyes.sharedMaterial = ChargedMaterial;

				// Включаем зарева от лазеров.
				if (LaserFlares != null && LaserFlares.Count > 0)
					for (int i = 0; i < LaserFlares.Count; i++)
						LaserFlares[i].gameObject.SetActive(true);

				IsBlinkRequested = false;
			}

			// Логика выключения мерцания.
			if (IsStopBlinkingRequested)
			{
				// Меняем материал 'глаз' на оригинальный.
				Eyes.sharedMaterial = OriginalLaserMaterial;

				// Выключаем зарева от лазеров.
				if (LaserFlares != null && LaserFlares.Count > 0)
					for (int i = 0; i < LaserFlares.Count; i++)
						LaserFlares[i].gameObject.SetActive(false);

				IsStopBlinkingRequested = false;
			}

			if (!IsShielded && !IsDead)
			{
				// Эффект нарастания/затухания света зарев из лазеров.
				for (int i = 0; i < LaserFlares.Count; i++)
				{
					if (AreLaserFlaresFadingIn)
					{
						LaserFlares[i].color = new Color(LaserFlares[i].color.r, LaserFlares[i].color.g, LaserFlares[i].color.b,
							LaserFlares[i].color.a + Time.deltaTime * 1.5f);
						if (LaserFlares[i].color.a >= 0)
							AreLaserFlaresFadingIn = false;
					}
					else
					{
						LaserFlares[i].color = new Color(LaserFlares[i].color.r, LaserFlares[i].color.g, LaserFlares[i].color.b,
							LaserFlares[i].color.a - Time.deltaTime * 1.5f);
						if (LaserFlares[i].color.a <= 0.4f)
							AreLaserFlaresFadingIn = true;
					}
				}

				// Включаем лазеры.
				if (Time.timeSinceLevelLoad > ShootLasersTime && Lasers != null && Lasers.Count > 0)
				{
					if (!LaserSound.isPlaying)
						LaserSound.Play();

					StabilizationSpeed = .1f;
					PreventImpulses = true;

					for (int i = 0; i < Lasers.Count; i++)
					{
						Lasers[i].SetActive(true);

						float lerpPower = (Time.timeSinceLevelLoad - ShootLasersTime) / (GameOverTime - ShootLasersTime);

						// Плавно увеличиваем размер цилиндров эффектов лазеров, в зависимости от времени отданного на разогрев лазеров
						//	(TimeUntilGameOver - TimeUntilLasers).
						float newXZScale = Mathf.Lerp(0.01f, LasersOriginalXZScale, lerpPower);
						Lasers[i].transform.localScale = new Vector3(newXZScale, Lasers[i].transform.localScale.y, newXZScale);
					}
				}

				// Запускаем Game Over.
				if (Time.timeSinceLevelLoad > GameOverTime)
				{
					var gameOver = FindObjectOfType<Mechanics.GameOver>();
					gameOver.PlayGameOverAnim();
				}
			}
		}
		#endregion
	}
}
