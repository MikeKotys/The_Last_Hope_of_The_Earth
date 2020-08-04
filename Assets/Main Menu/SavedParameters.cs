using UnityEngine;


namespace UI
{
	/// <summary>Хранит опции игры</summary>
	[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SpawnManagerScriptableObject", order = 1)]
	public class SavedParameters : ScriptableObject
	{
		/// <summary>Нужно ли инвертировать ввод мыши по вертикали.</summary>
		public bool InvertMouseY = true;
	}
}