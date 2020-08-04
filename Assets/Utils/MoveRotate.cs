using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
	/// <summary>Позволяет бесконечно перемещать/вращать объекты.</summary>
	public class MoveRotate : MonoBehaviour
	{
		[Tooltip("Сила перемещения объекта.")]
		public Vector3 RotationPower;
		[Tooltip("Сила вращения объекта.")]
		public Vector3 MovePower;

		void Update()
		{
			transform.Rotate(RotationPower * Time.deltaTime * 60);
			transform.position += MovePower * Time.deltaTime * 60;
		}
	}
}
