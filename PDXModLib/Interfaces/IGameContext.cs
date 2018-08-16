using PDXModLib.GameContext;
using PDXModLib.ModData;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PDXModLib.Interfaces
{
    public interface IGameContext
    {
		ModSelection CurrentSelection { get; set; }
		IReadOnlyList<ModSelection> Selections { get; }

		Task<bool> Initialize();

		bool SaveSettings();
		bool SaveSelection();
		Task<bool> SaveMergedMod(MergedMod mod, bool mergedFilesOnly);
		void DeleteCurrentSelection();
		void DuplicateCurrentSelection(string newName);
		void LoadMods();

	}
}
