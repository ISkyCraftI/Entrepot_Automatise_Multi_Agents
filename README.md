# Entrepot_Automatise_Multi_Agents
Projet Unity afin de simuler un entrepôt automatisé en utilisant des multi-agents.
Projet n°1 « Entrepôt automatisé » du module Systèmes Multi-Agents (ESEO).

# Initialisation
Voir [INIT.md](INIT.md) : version de Unity, clone, Git LFS, pipeline Blender → Unity, arborescence.

# Attendus du projet

L'entrepôt fonctionne en **Goods-to-Person (GTP)** : les produits viennent à l'opérateur humain.
L'opérateur ne va pas chercher les produits dans les rayons.

- **Partie Unity** : assets de l'entrepôt, des étagères et des robots-tortues ; zones de picking ;
  bases de recharge ; NavMesh en grille (pas de diagonales, obstacles invisibles au sol) ;
  étagères en `NavMeshObstacle` ; deux types de `NavMeshAgent` (robot à vide qui passe sous les
  étagères, robot chargé qui les évite).
- **Partie optimisation** : répartition des produits sur les étagères (produits souvent commandés
  ensemble sur la même étagère) ; ordonnanceur qui transforme un carnet de commandes (fixe, puis
  dynamique) en ordres (étagère à prendre, zone de picking, position de dépose) ; trajectoires et
  évitement entre robots ; retour à la base la plus proche (pas d'ordre ou batterie basse) ;
  vitesse, autonomie et temps de picking aléatoires ; remontée d'état des robots vers l'ordonnanceur.

Vidéos de référence :
- Sujet (robots-tortues) : https://www.youtube.com/watch?v=Ql7Xm6q-798
- SAVOYE X-PTS (navettes, convoyeurs, poste de préparation) : https://youtu.be/wzu_jIn9ThI

# Comment marche l'usine

## I. Stockage

On ne fait pas encore le remplissage du stockage. On part du principe que le stockage est infini.

## II. Robots

Pour extraire du stockage (et plus tard le remplir), on utilise des [navettes 2D de chez Savoye](https://www.savoye.com/ressource/systeme-a-navettes-x-pts/).
Pour transporter des étagères on utilise un AMR.
Exosquelette

## III. Préparation des commandes

Voir https://www.savoye.com/en/ressource/shuttle-systems/

## IV. Résumé schéma

Stockage toujours plein -> robot qui se déplace dans les racks -> envoie à un système de tri qui -> étagères sont rangées dans une zone de stockage tampon par l'AMR -> étagère est amenée à l'opérateur pour la commande -> étagère est ramenée dans la zone tampon.

# Feuille de route

| Phase | Contenu | État |
|---|---|---|
| 0 | Reproduire en 3D la vidéo SAVOYE X-PTS : racks, navettes 2D, élévateurs, convoyeurs, tri. Stockage infini. **Pas d'étagères mobiles.** | En cours |
| 1 | AMR (robots-tortues), étagères mobiles, zone tampon, NavMesh en grille, deux types d'agents | À faire |
| 2 | Ordonnanceur, batterie, communication entre robots, aléas | À faire |

> Phase 0 : le storyboard exact (éléments, ordre des séquences) reste à valider à partir de la vidéo.
