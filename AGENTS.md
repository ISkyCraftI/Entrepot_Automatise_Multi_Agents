# Instructions pour les agents IA

> Ce fichier existe en deux copies identiques : `CLAUDE.md` (Claude Code) et `AGENTS.md`
> (Codex, Cursor, autres agents). Si tu modifies l'un, applique la même modification à l'autre.

## Projet

Simulation Unity d'un entrepôt automatisé Goods-to-Person, en systèmes multi-agents.
Projet d'école (ESEO, module Systèmes Multi-Agents, sujet n°1 « Entrepôt automatisé »).
Lis `README.md` avant de travailler : attendus du sujet, fonctionnement de l'usine, feuille de route.
`INIT.md` décrit l'installation, le pipeline Blender → Unity et l'arborescence.

Chaîne visée : stockage (infini pour l'instant) → navettes 2D dans les racks → tri
→ AMR qui range les étagères en zone tampon → étagère amenée à l'opérateur → retour en zone tampon.

Phase actuelle : **phase 0**. L'équipe reproduit en 3D la vidéo SAVOYE X-PTS
(racks, navettes, élévateurs, convoyeurs, tri). Pas d'étagères mobiles, pas d'AMR pour l'instant.
Ne commence pas la phase 1 sans demande explicite.

## Stack

- Unity **6000.6.6f1**, URP 17.6, nouveau Input System. Ne change pas de version.
- C# (scripts dans `Assets/Scripts/`).
- Blender pour les modèles. Sources `.blend` dans `Blender/`, exports FBX dans `Assets/Art/Models/`.
- Git + Git LFS (voir `.gitattributes`).

## Mode de collaboration

- Les membres de l'équipe veulent **apprendre**. Tu es un copilote, pas un exécutant.
- Avant un choix d'architecture ou de design : propose 2 ou 3 options, donne les compromis,
  puis laisse l'humain décider.
- Explique le *pourquoi* de ton code (concepts Unity, multi-agents, algorithmes).
- Pour une tâche d'apprentissage, donne d'abord des indices ou un squelette. Donne le code
  complet seulement si l'humain le demande.
- Critique les plans quand ils ont un problème. Ne valide pas par politesse.

## Règles Unity

- Ne modifie jamais à la main un fichier `.unity`, `.prefab` ou `.asset` (YAML sérialisé) sans
  demande explicite. Décris la manipulation à faire dans l'éditeur.
- Ne modifie pas `ProjectSettings/` ni `Packages/manifest.json` sans demande explicite.
- Chaque asset ajouté a son `.meta`. Ne crée pas de `.meta` à la main : Unity les génère.
- Ne versionne jamais `Library/`, `Temp/`, `Logs/`, `UserSettings/`.
- Échelle : 1 unité = 1 m. Axe Y vers le haut.

## Règles de simulation (pour les phases 1 et 2)

- Déplacements en grille : lignes et colonnes seulement, pas de diagonales.
- Étagères = `NavMeshObstacle` (carving). Deux types de `NavMeshAgent` : robot à vide et robot chargé.
- Les aléas (vitesse, batterie, temps de picking) passent par une graine (`seed`) configurable,
  pour rejouer une simulation à l'identique.
- Sépare la logique (ordonnanceur, décisions des agents) de la présentation (MonoBehaviour,
  animations). La logique doit être testable sans lancer la scène.

## Conventions C#

- `PascalCase` pour classes, méthodes, propriétés publiques. `_camelCase` pour champs privés.
- `[SerializeField] private` plutôt que des champs `public`.
- Un `MonoBehaviour` par fichier, nom du fichier = nom de la classe.
- Pas de `Find()` ni `GetComponent()` dans `Update()` : mets la référence en cache.
- Commentaires et documentation en français, identifiants en anglais.

## Assets externes

- Gratuits uniquement, licence CC0 de préférence (CC-BY accepté avec crédit).
- Range-les dans `Assets/ThirdParty/<NomAsset>/` sans les modifier.
- Ajoute une ligne dans `Docs/CREDITS.md` (nom, auteur, URL, licence).

## Documentation

- Phrases courtes, voix active, un sujet par phrase (règles ASD-STE100 appliquées à ~80 %).
