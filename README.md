# Entrepot_Automatise_Multi_Agents
Projet Unity afin de simuler un Entrepot automatisé en utilsiant des multi-agents

## Prérequis

- **Unity Editor `6000.6.6f1`** (exactement, voir `ProjectSettings/ProjectVersion.txt`).
  Installer cette version via Unity Hub (Installs → Install Editor → Archive : https://unity.com/releases/editor/archive).
  Avec une autre version, certains packages (`com.unity.pipeline`, URP 17.6…) peuvent ne pas se résoudre.
- **Git LFS** : les assets lourds (images, modèles 3D, audio…) sont stockés avec LFS.
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
3. Premier ouverture : l'import peut durer plusieurs minutes. `Library/`, `Logs/`, `UserSettings/`
   sont générés localement et ne sont pas versionnés.
4. Ouvrir la scène `Assets/Scenes/SampleScene.unity`.

## Si Git LFS était absent au moment du clone

Des fichiers de ~130 octets (pointeurs LFS) apparaissent à la place des images. Corriger avec :

```bash
git lfs install
git lfs pull
```

## Règles de travail

- Toujours committer les fichiers `.meta` avec leurs assets.
- Ne jamais committer `Library/`, `Temp/`, `Logs/`, `UserSettings/` (déjà dans `.gitignore`).
