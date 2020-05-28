namespace System.Threading
{
	/// <summary>
	///         Gibt an, ob eine Sperre mehrmals dem gleichen Thread zugewiesen werden kann.
	///       </summary>
	[Serializable]
	public enum LockRecursionPolicy
	{
		/// <summary>
		///         Wenn ein Thread versucht, die rekursiv eine Sperre erhalten, wird eine Ausnahme ausgelöst.
		///          Einige Klassen gestatten gewisse Rekursionen, wenn diese Einstellung aktiviert ist.
		///       </summary>
		NoRecursion,
		/// <summary>
		///         Ein Thread kann rekursiv eine Sperre erhalten eingeben.
		///          Einige Klassen können diese Funktion beschränken.
		///       </summary>
		SupportsRecursion
	}
}
