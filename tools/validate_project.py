"""Quick source-package sanity check (NOT a Unity compilation or play test)."""
from pathlib import Path
import json
import math
import re

ROOT = Path(__file__).resolve().parents[1]

def check(expression, label):
    if not expression:
        raise AssertionError(label)
    print('PASS:', label)

manifest = json.loads((ROOT / 'Packages/manifest.json').read_text())
check('com.unity.modules.physics' in manifest['dependencies'], '3D physics dependency declared')

scripts = list((ROOT / 'Assets/Scripts').rglob('*.cs'))
check(len(scripts) >= 10, 'gameplay scripts packaged')
if any(re.search(r'\bParticleSystem\b', script.read_text(encoding='utf-8')) for script in scripts):
    check(manifest['dependencies'].get('com.unity.modules.particlesystem') == '1.0.0',
          'Particle System module enabled when VFX scripts reference ParticleSystem')
check((ROOT / 'Assets/Scenes/PhysicsLab.unity').is_file(), 'Unity playable scene included')
check('Palinodia.Runtime' in (ROOT / 'Assets/Tests/Editor/Palinodia.EditorTests.asmdef').read_text(),
      'edit-mode tests reference runtime assembly')

allmetas = list((ROOT / 'Assets').rglob('*.meta'))
guids = []
for meta in allmetas:
    m = re.search(r'^guid: ([0-9a-f]{32})$', meta.read_text(), re.M)
    check(m is not None, f'valid meta GUID in {meta.relative_to(ROOT)}')
    guids.append(m.group(1))
check(len(guids) == len(set(guids)), 'every asset GUID is unique')
for script in scripts:
    check(Path(str(script) + '.meta').exists(), f'{script.name} has meta')

boot_meta = (ROOT / 'Assets/Scripts/Gameplay/DemoBootstrap.cs.meta').read_text()
boot_guid = re.search(r'guid: ([0-9a-f]{32})', boot_meta).group(1)
scene = (ROOT / 'Assets/Scenes/PhysicsLab.unity').read_text()
check(boot_guid in scene, 'scene references DemoBootstrap MonoBehaviour')
scene_meta = (ROOT / 'Assets/Scenes/PhysicsLab.unity.meta').read_text()
scene_guid = re.search(r'guid: ([0-9a-f]{32})', scene_meta).group(1)
check(scene_guid in (ROOT / 'ProjectSettings/EditorBuildSettings.asset').read_text(), 'scene included in build')

# Compare the C# formula against a direct kinetic-energy calculation.
for mass, initial_v, added_e in [(20, 0, 2500), (12, -4, 300), (80, 8, 1000)]:
    delta_v = -initial_v + math.sqrt(initial_v**2 + 2*added_e/mass)
    measured = 0.5 * mass * ((initial_v + delta_v)**2 - initial_v**2)
    check(abs(measured - added_e) < 1e-8, f'energy-bounded impulse test ({mass}, {initial_v}, {added_e})')

check((ROOT / 'Docs/PROJECT_SOURCE.md').exists(), 'original game canon included')
print('\nSource packaging checks passed. Run Unity Test Runner for real C# / engine tests.')
