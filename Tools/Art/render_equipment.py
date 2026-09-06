"""Offline Blender contact sheet for the generated source meshes, output in Logs."""
import bpy
import math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
KIT=ROOT/'Assets/Game/Content/Modules/Art/Equipment'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
mat=bpy.data.materials.new('Maintenance palette')
mat.use_nodes=True
shader=mat.node_tree.nodes.get('Principled BSDF')
shader.inputs['Metallic'].default_value=.25
shader.inputs['Roughness'].default_value=.38
texture=mat.node_tree.nodes.new('ShaderNodeTexImage')
texture.image=bpy.data.images.load(str(KIT/'MaintenancePalette.tga'))
texture.interpolation='Closest'
mat.node_tree.links.new(texture.outputs['Color'],shader.inputs['Base Color'])
names=['ImpactWrench','SealantGun','CircuitBridger','PressureGauge','ReplacementPipe',
       'CoolingPump','ControlConsole','StormRelay','ToolDock','InspectionPanel']
for i,name in enumerate(names):
    vertices=[]; faces=[]; tex=[]; uvs=[]
    for line in (KIT/(name+'.obj')).read_text().splitlines():
        p=line.split()
        if not p: continue
        if p[0]=='v':
            x,y,z=map(float,p[1:]); vertices.append((x,-z,y))
        if p[0]=='vt': tex.append(tuple(map(float,p[1:])))
        if p[0]=='f':
            q=[s.split('/') for s in p[1:]]
            faces.append([int(s[0])-1 for s in q]); uvs.append([tex[int(s[1])-1] for s in q])
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(vertices,[],faces)
    uv=mesh.uv_layers.new()
    for face,values in zip(mesh.polygons,uvs):
        for loop,value in zip(face.loop_indices,values): uv.data[loop].uv=value
    obj=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    # Each asset gets the same display footprint; the source remains metre scale.
    span=max(max(v[j] for v in vertices)-min(v[j] for v in vertices) for j in range(3))
    s=1.55/span
    obj.scale=(s,s,s)
    obj.rotation_euler[2]=math.radians(155 if i in [6,7,8,9] else -25)
    obj.location=((i%5-2)*2.6,(i//5)*3.2, .15-min(v[2] for v in vertices)*s)
    bpy.ops.mesh.primitive_cube_add(size=1,location=(obj.location.x,obj.location.y,.04))
    plinth=bpy.context.object; plinth.scale=(2.25,2.5,.08)
    base=bpy.data.materials.get('Base') or bpy.data.materials.new('Base')
    base.diffuse_color=(.035,.055,.07,1); base.use_nodes=True; base.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.035,.055,.07,1); plinth.data.materials.append(base)
    bpy.ops.object.text_add(location=(obj.location.x-1.0,obj.location.y-1.1,.10))
    label=bpy.context.object
    label.data.body=f'{i+1:02}  '+name
    label.data.size=.13
    label.data.extrude=.001
    ink=bpy.data.materials.get('Ink') or bpy.data.materials.new('Ink')
    ink.diffuse_color=(.6,.8,.85,1); ink.use_nodes=True; ink.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.6,.8,.85,1); label.data.materials.append(ink)

bpy.ops.object.camera_add(location=(5,-12,14))
camera=bpy.context.object
camera.rotation_euler=(Vector((0,1.5,.3))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'; camera.data.ortho_scale=15.5
bpy.context.scene.camera=camera
for loc,power,size in [((1,-5,10),2400,8),((-6,3,6),1600,7),((6,6,8),2200,6)]:
    bpy.ops.object.light_add(type='AREA',location=loc)
    lamp=bpy.context.object; lamp.data.energy=power; lamp.data.shape='DISK'; lamp.data.size=size
    lamp.rotation_euler=(Vector((0,1,0))-lamp.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.samples=32
scene.world.color=(.12,.12,.12)
scene.render.resolution_x=1800; scene.render.resolution_y=1050; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(ROOT/'Logs/Equipment-ContactSheet.png')
scene.view_settings.view_transform='AgX'
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Logs/Equipment-Review.blend'))
bpy.ops.render.render(write_still=True)
