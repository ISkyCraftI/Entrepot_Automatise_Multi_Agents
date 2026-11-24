# Initialisation du projet

Retour au [README](README.md).

## Prérequis

- **Unity Editor `6000.6.6f1`** (exactement, voir `ProjectSettings/ProjectVersion.txt`).
  Installer cette version via Unity Hub (Installs → Install Editor → Archive : https://unity.com/releases/editor/archive).
  Avec une autre version, certains packages (`com.unity.pipeline`, URP 17.6…) peuvent ne pas se résoudre.
- **Git LFS** : les assets lourds (images, modèles 3D, audio…) sont stockés avec LFS.
- **Blender** (version libre, 4.x ou plus) pour modéliser les objets.
- Une connexion internet au premier lancement (téléchargement des packages Unity).

## Cloner et ouvrir le projet

```bash
git lfs install                           # une seule fois par machine, AVANT le clone
git config --global core.longpaths true   # Windows uniquement
git clone https://github.com/ISkyCraftI/Entrepot_Automatise_Multi_Agents.git
```

1. Cloner dans un dossier au **chemin court** (ex. `C:\dev\`). Sous Windows, un chemin trop long
   fait échouer l'import des packages (`DirectoryNotFoundException ... Library\PackageCache\...`).
2. Dans Unity Hub : **Add → Add project from disk** et choisir le dossier cloné
   (celui qui contient `Assets/`, `Packages/` et `ProjectSettings/`).
3. Première ouverture : l'import peut durer plusieurs minutes. `Library/`, `Logs/`, `UserSettings/`
   sont générés localement et ne sont pas versionnés.
4. Ouvrir la scène `Assets/Scenes/SampleScene.unity`.

## Si Git LFS était absent au moment du clone

Des fichiers de ~130 octets (pointeurs LFS) apparaissent à la place des images. Corriger avec :

```bash
git lfs install
git lfs pull
```

## Pipeline Blender → Unity

- Unité : **1 unité Blender = 1 m = 1 unité Unity**. Modéliser à l'échelle réelle.
- Garder les fichiers `.blend` **hors de `Assets/`** (dossier `Blender/` à la racine).
  Un `.blend` dans `Assets/` oblige chaque membre à avoir Blender installé pour ouvrir le projet.
- Exporter en **FBX** vers `Assets/Art/Models/` :
  `Apply Scalings: FBX All`, `Forward: -Z Forward`, `Up: Y Up`, cocher `Apply Transform`.
- Origine de l'objet : au sol, au centre de l'emprise (facilite le placement sur la grille).
- Après import : vérifier l'échelle (Scale Factor = 1) et la rotation (0, 0, 0) dans l'Inspector.

## Arborescence prévue

```
Assets/
  Art/Models/      FBX exportés depuis Blender ou assets externes
  Art/Materials/
  Art/Textures/
  Prefabs/
  Scenes/
  Scripts/         C# (agents, ordonnanceur, convoyeurs…)
  ThirdParty/      assets téléchargés, non modifiés (un sous-dossier + LICENSE par asset)
Blender/           sources .blend (LFS)
Docs/              storyboard, choix techniques, crédits des assets
```

## Packages à ajouter

- **AI Navigation** (`com.unity.ai.navigation`) : nécessaire pour `NavMeshSurface`,
  `NavMeshModifier` et les NavMesh par type d'agent (phase 1). Absent du manifest pour l'instant.

## Assets externes

Seulement des assets gratuits avec une licence compatible (CC0 de préférence).
Noter chaque asset dans `Docs/CREDITS.md` : nom, auteur, URL, licence.

## Règles de travail

- Toujours committer les fichiers `.meta` avec leurs assets.
- Ne jamais committer `Library/`, `Temp/`, `Logs/`, `UserSettings/` (déjà dans `.gitignore`).
- Une scène = un responsable à la fois (les conflits de merge sur les `.unity` sont pénibles).
  Préférer travailler dans des prefabs.
