"""Blender --background --python Tools/Art/build_equipment.py
Deterministic OBJ meshes: metre units, Unity Y up/+Z tool forward, palette UVs.
No third party assets. Only this kit's output directory is written.
"""
import bpy
import math
import json
import struct
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Game/Content/Modules/Art/Equipment'
OUT.mkdir(parents=True, exist_ok=True)
COLORS = [(32,43,52),(67,89,103),(220,151,43),(188,206,210),
          (17,24,30),(58,207,204),(153,98,224),(223,77,47)]
parts = []
stats = []

def write_if_changed(path, data):
    if not path.exists() or path.read_bytes() != data:
        path.write_bytes(data)

# Eight solid color swatches, no external textures, one material per mesh.
pixels = bytes(v for y in range(8) for c in COLORS for x in range(8)
               for v in (c[2], c[1], c[0]))
write_if_changed(OUT/'MaintenancePalette.tga',
    struct.pack('<BBBHHBHHHHBB',0,0,2,0,0,0,0,0,64,8,24,32)+pixels)

def finish(obj, color, bevel=0.0):
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Machined edge', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj['palette'] = color
    parts.append(obj)
    return obj

def box(name, p, s, c=0, bevel=.015, rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=p)
    obj = bpy.context.object
    obj.name = name
    obj.scale = s
    obj.rotation_euler = tuple(math.radians(a) for a in rotation)
    return finish(obj,c,min(bevel,min(s)*.2))

def cyl(name,p,r,depth,c=1,axis='z',vertices=12,r2=None):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=r,
        radius2=r if r2 is None else r2, depth=depth, location=p)
    obj=bpy.context.object
    obj.name=name
    if axis=='y': obj.rotation_euler[0]=math.pi/2
    if axis=='x': obj.rotation_euler[1]=math.pi/2
    return finish(obj,c)

def ring(name,p,r,minor,c=1,axis='z'):
    bpy.ops.mesh.primitive_torus_add(major_segments=16,minor_segments=4,
        location=p,major_radius=r,minor_radius=minor)
    obj=bpy.context.object
    obj.name=name
    if axis=='y': obj.rotation_euler[0]=math.pi/2
    if axis=='x': obj.rotation_euler[1]=math.pi/2
    return finish(obj,c)

def bolts(p,r,count=8,size=.025,axis='z'):
    for i in range(count):
        a=i*math.tau/count
        if axis=='z': q=(p[0]+r*math.cos(a),p[1]+r*math.sin(a),p[2])
        elif axis=='x': q=(p[0],p[1]+r*math.cos(a),p[2]+r*math.sin(a))
        else: q=(p[0]+r*math.cos(a),p[1],p[2]+r*math.sin(a))
        cyl('Hex bolt',q,size,size*.7,3,axis,6)

def grip(accent):
    box('Grip',(0,-.23,.1),(.125,.32,.14),4,rotation=(-12,0,0))
    box('Battery shoe',(0,-.40,.08),(.21,.10,.23),0)
    box('Battery lock',(0,-.42,.20),(.13,.045,.025),accent)
    box('Trigger',(0,-.1,.205),(.07,.08,.04),accent,.008)
    for y in [-.17,-.22,-.27,-.32]:
        box('Grip rib',(0,y,.02),(.136,.018,.025),1,.002)

def wrench():
    grip(2)
    box('Impact housing',(0,0,.23),(.23,.21,.35),2,.035)
    cyl('Motor',(0,0,.06),.119,.13,0)
    cyl('Gearcase',(0,0,.44),.10,.14,3)
    ring('Gearcase collar',(0,0,.48),.09,.016,0)
    cyl('Drive shaft',(0,0,.57),.041,.14,3,vertices=8)
    cyl('Socket',(0,0,.68),.071,.12,1,vertices=6)
    cyl('Socket recess',(0,0,.742),.043,.002,4,vertices=6)
    ring('Socket rim',(0,0,.738),.054,.012,3)
    bolts((0,0,-.011),.082,6,.014)
    for x in [-.119,.119]:
        for z in [.12,.16,.20,.24]:
            box('Cooling slot',(x,.025,z),(.008,.075,.017),4,.002)
    box('Top service plate',(0,.111,.27),(.15,.015,.17),0)
    box('Charge bar',(0,.12,.29),(.09,.008,.024),5,.001)

def sealant():
    grip(5)
    cyl('Cartridge',(0,.015,.20),.10,.49,3)
    for z in [-.06,.43]:
        cyl('Clamp',(0,.015,z),.121,.045,0)
        ring('Clamp edge',(0,.015,z),.112,.009,5)
    box('Cartridge label',(0,.119,.21),(.10,.01,.24),5,.002)
    for z in [.12,.17,.22,.27]:
        box('Fill scale',(0,.127,z),(.055,.005,.008),4,.001)
    box('Pump rail',(-.13,-.08,.21),(.045,.055,.51),0)
    cyl('Nozzle shoulder',(0,.015,.52),.085,.12,1,r2=.04)
    cyl('Nozzle',(0,.015,.67),.035,.20,2,r2=.02)
    ring('Nozzle guard',(0,.015,.78),.044,.015,0)
    cyl('Outlet',(0,.015,.792),.018,.004,4)
    cyl('Pressure dial',(.117,.12,.16),.061,.032,0,axis='x')
    cyl('Dial face',(.136,.12,.16),.049,.008,3,axis='x')
    box('Dial needle',(.143,.135,.15),(.008,.045,.006),7,.001,rotation=(25,0,0))

def bridger():
    grip(6)
    box('Insulated housing',(0,0,.20),(.25,.19,.31),0,.028)
    box('Access plate',(0,.105,.18),(.19,.026,.22),6)
    box('Display bezel',(0,.124,.17),(.125,.014,.105),4,.004)
    for x in [-.035,0,.035]:
        box('Display trace',(x,.134,.17),(.014,.006,.065),5,.001)
    cyl('Coil former',(0,0,.41),.085,.17,1)
    for z in [.34,.38,.42,.46]: ring('Induction winding',(0,0,z),.091,.012,6)
    box('Fork bridge',(0,0,.54),(.30,.09,.06),3)
    for x in [-.115,.115]:
        box('Insulated probe',(x,0,.68),(.055,.062,.26),6,.01)
        box('Probe contact',(x,0,.845),(.043,.045,.075),3,.006)
        box('Contact inset',(x,0,.886),(.025,.028,.006),5,.001)

def pump():
    for x in [-1.05,1.05]:
        box('Foundation skid',(x,.28,0),(.45,.55,2.75),0,.07)
        for z in [-1.10,1.10]: bolts((x,.56,z),.12,4,.045,'y')
    cyl('Motor casing',(0,1.05,0),.87,3.20,1,vertices=16)
    for z in [-1.5,-.80,0,.80,1.5]: ring('Reinforcement band',(0,1.05,z),.87,.065,0)
    for i in range(12):
        a=i*math.tau/12
        obj=box('Axial cooling fin',(.85*math.cos(a),1.05+.85*math.sin(a),0),(.08,.13,2.65),1,.012)
        obj.rotation_euler[2]=a-math.pi/2
    for z in [-1.69,1.69]:
        cyl('End shield',(0,1.05,z),.98,.18,2,vertices=16)
        cyl('End plate',(0,1.05,z*1.065),.77,.07,0,vertices=16)
        bolts((0,1.05,z*1.09),.84,12,.055)
    cyl('Intake hub',(0,1.05,-1.95),.34,.25,1)
    ring('Hub seal',(0,1.05,-2.09),.29,.035,3)
    cyl('Hub recess',(0,1.05,-2.1),.24,.015,4)
    box('Service cover',(.88,1.15,.35),(.18,.53,.75),2,.05)
    box('Service label',(.981,1.2,.35),(.012,.20,.48),0,.002)
    for z in [.20,.35,.50]: box('Readout',(.99,1.20,z),(.012,.1,.045),5,.002)
    for x in [-.42,.42]:
        cyl('Upper coupling',(x,1.90,.7),.16,.25,1,axis='y')
        ring('Coupling flange',(x,2.03,.7),.18,.035,2,axis='y')

def console():
    # Normalized to the existing one-unit interaction volume, front -Z.
    box('Cabinet',(0,-.02,.05),(.94,.92,.83),0,.065)
    box('Foot',(0,-.465,.03),(1,.07,.90),1)
    for x in [-.445,.445]: box('Edge armor',(x,.015,-.34),(.07,.87,.12),2)
    box('Screen surround',(0,.20,-.415),(.75,.39,.09),1,.027)
    box('Screen glass',(0,.20,-.469),(.64,.29,.018),4,.004)
    for i in range(5):
        box('Telemetry bar',(-.245+i*.105,.155,-.481),(.056,.035+i*.028,.007),5,.001)
    box('Screen header',(0,.30,-.481),(.51,.017,.008),3,.001)
    box('Control tray',(0,-.15,-.45),(.77,.17,.12),1)
    for x in [-.27,-.16,-.05]:
        cyl('Key',(x,-.14,-.518),.031,.02,2 if x==-.27 else 3,vertices=8)
    cyl('Emergency stop',(.23,-.14,-.53),.055,.04,7,vertices=12)
    for y in [-.30,-.345,-.39]: box('Vent',(0,y,-.383),(.54,.018,.015),4,.002)
    for x in [-.38,.38]:
        for y in [-.40,.40]: cyl('Panel screw',(x,y,-.425),.017,.02,3,vertices=6)

def relay():
    box('Relay backplate',(0,0,.14),(.90,1,.63),0,.045)
    for x in [-.36,.36]: box('Rail',(x,0,-.18),(.085,.85,.15),2)
    cyl('Transformer',(0,.07,-.12),.26,.51,1,axis='y')
    for y in [-.13,-.03,.07,.17,.27]: ring('Coil winding',(0,y,-.12),.25,.024,6,'y')
    for y in [-.38,.42]:
        box('Contact block',(0,y,-.23),(.47,.10,.20),3)
        box('Insulator',(0,y,-.355),(.28,.064,.06),6)
    box('Telemetry cap',(0,.36,.14),(.54,.10,.27),1)
    for x in [-.14,0,.14]: box('Relay strip',(x,.42,.14),(.06,.014,.17),5,.002)

def pipe():
    cyl('Pipe',(0,0,0),.14,1.44,1,vertices=16)
    for z in [-.72,.72]:
        cyl('Flange',(0,0,z),.215,.10,2,vertices=12)
        ring('Rim',(0,0,z*1.08),.167,.03,3)
        cyl('Bore',(0,0,z*1.095),.132,.004,4)
        bolts((0,0,z*1.085),.184,8,.014)
    for z in [-.45,.45]: ring('Locking collar',(0,0,z),.145,.018,0)
    box('Flow marker',(0,.143,0),(.075,.01,.30),5,.003)

def gauge():
    cyl('Gauge body',(0,0,0),.27,.16,0,axis='x',vertices=24)
    ring('Gauge rim',(.095,0,0),.239,.024,3,'x')
    cyl('Dial',(.098,0,0),.22,.012,4,axis='x',vertices=24)
    for i in range(17):
        a=math.radians(-130+i*16)
        obj=box('Scale',(.108,.18*math.cos(a),.18*math.sin(a)),(.006,.025,.01),5 if i<12 else 7,.001)
        obj.rotation_euler[0]=a
    box('Needle',(.12,.058,-.033),(.01,.15,.009),2,.002,rotation=(-30,0,0))
    cyl('Needle hub',(.13,0,0),.025,.015,3,axis='x')
    cyl('Stem',(0,-.30,0),.055,.14,1,axis='y',vertices=6)

def rack():
    box('Dock backplate',(0,0,0),(.92,.96,.25),0,.05)
    for x in [-.39,.39]: box('Dock side',(x,0,-.20),(.12,.94,.28),1)
    for y in [-.39,.39]: box('Safety rail',(0,y,-.19),(.69,.10,.21),2)
    for y in [-.18,.04,.26]: box('Dock slot',(0,y,-.134),(.48,.04,.025),4,.002)
    box('Dock status',(0,.39,-.305),(.30,.035,.018),5,.002)
    for x in [-.28,.28]: box('Retention clip',(x,-.10,-.27),(.08,.19,.16),3)

def panel():
    box('Inspection housing',(0,0,0),(1.05,.75,.20),0,.035)
    box('Removable cover',(0,0,-.115),(.91,.60,.05),1)
    for x in [-.40,.40]:
        for y in [-.24,.24]: cyl('Captive screw',(x,y,-.15),.035,.03,2,vertices=6)
    box('Readout bezel',(-.18,.06,-.152),(.33,.28,.025),4)
    for y in [-.02,.06,.14]: box('Readout line',(-.18,y,-.168),(.25,.025,.008),5,.001)
    for y in [-.16,-.08,0,.08,.16]: box('Vent',(.20,y,-.147),(.24,.025,.025),4,.003)

def export(name, build):
    global parts
    parts=[]
    build()
    lines=['# Maintenance kit; generated from Tools/Art/build_equipment.py','o '+name]
    offset=0
    normal_index=0
    for color in range(8): lines.append('vt %.6f 0.5' % ((color+.5)/8))
    for obj in parts:
        mesh=obj.data
        mesh.calc_loop_triangles()
        mat=obj.matrix_world
        # Ensure dependency graph has evaluated location/rotation changes.
        bpy.context.view_layer.update()
        mat=obj.matrix_world.copy()
        normal_mat=mat.to_3x3().inverted().transposed()
        for v in mesh.vertices:
            p=mat@v.co
            lines.append('v %.6f %.6f %.6f' % tuple(p))
        for tri in mesh.loop_triangles:
            n=(normal_mat@tri.normal).normalized()
            lines.append('vn %.6f %.6f %.6f' % tuple(n))
            normal_index+=1
            lines.append('f '+' '.join('%d/%d/%d' % (offset+v+1,obj['palette']+1,normal_index) for v in tri.vertices))
        offset+=len(mesh.vertices)
    write_if_changed(OUT/(name+'.obj'), ('\n'.join(lines)+'\n').encode('ascii'))
    stats.append({'name':name,'triangles':normal_index,'vertices':offset,'renderers':1,'materials':1})
    for obj in parts: bpy.data.objects.remove(obj,do_unlink=True)

for name,build in [('ImpactWrench',wrench),('SealantGun',sealant),('CircuitBridger',bridger),
                   ('CoolingPump',pump),('ControlConsole',console),('StormRelay',relay),
                   ('ReplacementPipe',pipe),('PressureGauge',gauge),('ToolDock',rack),('InspectionPanel',panel)]:
    export(name,build)
write_if_changed(OUT/'MeshBudget.json',(json.dumps(stats,indent=2)+'\n').encode())
print('MAINTENANCE_KIT_OK '+json.dumps(stats))
