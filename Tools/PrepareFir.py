"""Create reusable game LODs from Poly Haven's CC0 high-density fir mesh.
Run with the portable Blender executable --background --python Tools/PrepareFir.py.
The downloaded original and Blender binaries stay under ignored Logs/.
"""
import bpy
import os

project = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=os.path.join(project, 'Logs', 'ForestSource', 'fir_tree_01.fbx'))
sources = [o for o in bpy.context.scene.objects if o.type == 'MESH']
for obj in sources:
    print('FIR SOURCE', obj.name, len(obj.data.polygons), tuple(obj.dimensions), [m.name for m in obj.data.materials], flush=True)

destination = os.path.join(project, 'Assets', 'ThirdParty', 'ForestScans', 'fir_tree_01')
os.makedirs(destination, exist_ok=True)
for variant, source in enumerate(sources[:2]):
    # Preserve source UVs/normals and material slots; simplify geometry, not bitmap textures.
    for lod, budget in enumerate((180000, 55000, 15000)):
        bpy.ops.object.select_all(action='DESELECT')
        obj = source.copy()
        obj.data = source.data.copy()
        bpy.context.collection.objects.link(obj)
        obj.name = f'Fir_{variant}_LOD{lod}'
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        obj.location = (0, 0, 0)
        modifier = obj.modifiers.new('Game LOD', 'DECIMATE')
        modifier.ratio = min(1.0, budget / max(1, len(obj.data.polygons)))
        modifier.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        path = os.path.join(destination, obj.name + '.fbx')
        bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True,
                                 bake_anim=False, add_leaf_bones=False, path_mode='STRIP',
                                 axis_forward='-Z', axis_up='Y')
        print('FIR LOD', obj.name, len(obj.data.polygons), os.path.getsize(path), flush=True)
        bpy.data.objects.remove(obj, do_unlink=True)
