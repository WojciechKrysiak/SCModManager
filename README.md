# SCModManager

## Stellaris mod manager - A mod managment tool, designed to reduce the headaches of modding Stellaris.

### Disclaimer!
This is very much a work in progress - bugs are expected - backup any files you don't wish irreprably harmed (for now only settings.txt).

### Usage:

Start SCModManager.exe from any location. It should detect the location of your Stellaris documents folder automatically. If not, let me know.

## Main features:

### Steam workshop integration
![]({{site.baseurl}}/PageImages/Main view.png)

**Save to stellaris** - saves your current mod selection to Settings.txt to be used in game.

**Custom selections** - manage different mod selections for different playthroughs.

**Current mod selection** - select the mods you wish to include in the current selection.

**Steam workshop page** - Steam workshop information about the selected mod.

**Resolve mod conflicts** - If the selected mods have conflicts you can combine them into one, resolving the conflicts.

The main page lets you select the mods that you want to use in your playthrough, providing a much more detailed description of what the mod does than the base game. You can create multiple selection lists, to handle multiple games at the same time. 

The Steam symbol signifies mods that have a Steam workshop page, and their details can be viewed in the application. 

The number of conflicting mods is displayed at the end of the list entry, and if you select it, all the conflicting mods will be displayed in red. 

### Mod conflict preview
![]({{site.baseurl}}/PageImages/Preview conflicts.png)

**Files in mod** - files added or modified by the mod.

**Conflicting mods** - mods that contain a conflict with the selected file.

**Conflict preview** - preview of the conflicts.

Conflict preview can quickly show whether the mod conflict is serious, or just a readme file named the same in two mods. 

### Merge mods
![]({{site.baseurl}}/PageImages/Merge.png)

**Conflicting files** - files in the resulting mod, marked in bold/red if there is a conflict. You can filter only those files with "Show only conflicts"

**Mod files to compare** - mods that contain the selected file. The button in between the drop-down resets the current comparison status.

**Conflict preview** - preview the individual conflict lines and decide (with right-click), which version to choose as a result.

**Merge tools** - tools to handle merging at the file level: 

- (Left/Right) as new file (before/after) - duplicates the files so that the left/right file has a name that's sorted before/after the other.
- Pick (left/right) - pick left/right file as the merge result.
- Save - save merge result. Won't be enabled untill all conflicts are resolved.

**Resulting file** - preview of the merge result, showing what will be saved to the merged mod. 

## Bugs

As there are surely going to be bugs, please let me know about them by submitting an [issue](https://github.com/WojciechKrysiak/SCModManager/issues/new) 


