# Maintenance equipment art

`build_equipment.py` is the authoritative procedural source for this kit's OBJ meshes
and 64x8 palette. No external assets or Python packages are required.

Run explicitly from the repository root with Blender 5.2:

    <Blender executable> --background --factory-startup --python Tools/Art/build_equipment.py

Only `Assets/Game/Content/Modules/Art/Equipment` is written. Output is stable and is
only written when bytes change. Unity uses metre units, Y up and +Z tool forward.
OBJ meshes carry palette UVs and normals; Unity imports them with one shared material.

In Unity run **Fun Game > Art > Import Maintenance Equipment**, or invoke
`FunGame.Editor.MaintenanceEquipmentArt.Import` with the repository's
`Tools/Invoke-Unity.ps1` and an explicit `-UnityEditorPath` matching ProjectVersion.txt.
This is an explicit import operation, never a build hook. Existing visual Prefabs are
not overwritten on repeated import. Mesh changes are picked up by normal asset import.

For a Blender contact sheet:

    <Blender executable> --background --factory-startup --python Tools/Art/render_equipment.py

This writes the preview PNG and Blender review scene to ignored `Logs`. The review
scene is a display layout with normalized display scales; it is not the Unity source.
The generated OBJ files and modelling script retain the actual integration dimensions.

Validation and scope: `Docs/Reviews/MAINTENANCE_EQUIPMENT_ART.md`.