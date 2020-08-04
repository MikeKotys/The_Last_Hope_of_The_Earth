using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enemies
{
	/// <summary>Контейнер с полезными ссылками, который должен висеть на каждом коллайдере врага.</summary>
	public class EnemyCollision : MonoBehaviour
	{
		[Header("Скрипт врага.")]
		public Enemy Enemy;
		[Header("С какой стороны находится коллайдер (представьте себе, что вы сидите внутри корабля врага).")]
		public bool IsALeftCollider;
		[Header("Находится ли коллайдер в нижней части корабля (представьте себе, что вы сидите внутри корабля врага).")]
		public bool IsABottomCollider;
		[Header("Является ли этот коллайдер зоной получения критического урона.")]
		public bool IsACriticalHit;
	}
}
