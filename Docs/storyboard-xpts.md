# Storyboard de la vidéo SAVOYE X-PTS

Source : « SAVOYE X-PTS : solution haute performance de préparation de commandes »
(https://youtu.be/wzu_jIn9ThI), 3 min 28 s. Planches d'images : `Docs/video-ref/`.

## Séquences

| Temps | Plan | Ce qu'on voit | Texte à l'écran |
|---|---|---|---|
| 0:00 | Logo | Titre Savoye X-PTS | — |
| 0:02–0:08 | Aérien | Bâtiment vu du ciel, puis toit retiré : vue de l'intérieur | — |
| 0:10–0:22 | Vue d'ensemble | Bloc de stockage bleu, convoyeurs, mezzanine, postes | Système à navettes X-PTS ; Jivaro (réduction de la hauteur des colis) ; Lancement automatique des colis ; Préparation à gares ; Goods-to-Person X-PTS Pick Stations ; Trieur d'expédition |
| 0:24–0:30 | Isolement | Le bloc X-PTS seul, la caméra longe les racks | — |
| 0:32–0:40 | Réel + 3D | Élévateur en tête d'allée | Élévateur haute performance, jusqu'à 4 charges par cycle |
| 0:42–0:46 | Façade | Le bloc de face | Stockage jusqu'à 20 mètres de hauteur |
| 0:48–0:54 | Extrémité | Passerelles et escaliers | Allées divisées en plusieurs zones d'accès maintenance |
| 0:56–1:10 | Dans l'allée | Une navette rouge par niveau, empilées sur toute la hauteur | — |
| 1:12–1:56 | Dessus du bloc | Navettes qui circulent entre les racks, bacs bleus, puis cartons | Stockage de bacs, plateaux et cartons sur un même niveau ; Bacs compartimentables ; Environnements ambiant, frais et froid négatif ; Supports jusqu'à 820 × 620 mm, 55 kg ; Cartons de tailles variables |
| 1:58–2:02 | Vue d'ensemble | Bloc et postes ajoutés | Évolutivité : performance et capacité de stockage |
| 2:04–2:30 | Poste de préparation | Opérateur, écran, bac source bleu → carton commande | Poste de préparation haute performance ; Très faible contrainte posturale ; Évolutif vers le picking robotisé (bras robot) |
| 2:32–2:36 | Réel | Postes réels, convoyeurs à rouleaux | — |
| 2:38–2:46 | Séquenceur | Tour de bacs en tampon vertical | Séquencement des produits pour chaque colis |
| 2:48–2:56 | Mezzanine | Machines Jivaro sur la boucle de convoyeurs | Sélection du meilleur colis selon l'état du système ; Adaptation automatique de la hauteur des colis |
| 2:58–3:02 | Rez-de-chaussée | Préparation manuelle sous la mezzanine | Synchronisation avec un système de préparation à gares |
| 3:04–3:06 | Expédition | Trieur en arête de poisson, opérateurs, palettes | — |
| 3:08–3:10 | Palettisation | Bras robots qui palettisent | Palettisation robotisée ; stockage des colis dans les allées X-PTS |
| 3:12–3:22 | Vue d'ensemble | Retour au plan général | — |
| 3:24 | Logo | — | — |

## Chaîne de flux observée

1. Les **navettes** (une par niveau et par allée) sortent un bac du rack.
2. L'**élévateur** en tête d'allée descend jusqu'à 4 bacs par cycle vers le convoyeur.
3. La **boucle de convoyeurs** amène le bac au **séquenceur**, puis au **poste de préparation**.
4. En parallèle, le **lancement automatique** pose un carton vide sur la ligne.
5. Au poste, l'opérateur prend l'article du bac source et le met dans le carton.
   L'écran indique la quantité.
6. Le bac source retourne au stockage. Le carton plein va au **Jivaro** (hauteur ajustée, carton fermé).
7. Le carton fermé va au **trieur d'expédition**, puis sur une palette.

## Éléments à modéliser (phase 0)

| Élément | Priorité | Remarque |
|---|---|---|
| Rack X-PTS (allées, niveaux) | Haute | Modulaire : un module = 1 allée × 1 travée × N niveaux |
| Navette | Haute | Simple boîte rouge avec 2 bras télescopiques |
| Élévateur (double berceau) | Haute | 2 berceaux × 2 bacs |
| Bac 600 × 400 | Haute | + variante compartimentée |
| Carton | Haute | 2 ou 3 tailles |
| Convoyeur droit, courbe, aiguillage | Haute | Kenney Conveyor Kit (CC0) possible |
| Poste de préparation | Haute | Table, écran, 2 lignes (bac source, carton) |
| Opérateur | Moyenne | Mannequin simple (la vidéo utilise un mannequin rouge) |
| Jivaro (fermeture carton) | Moyenne | Boîte grise avec tunnel |
| Lancement des colis | Moyenne | Boîte grise |
| Séquenceur | Basse | Tour de bacs |
| Trieur d'expédition | Basse | Ligne + goulottes en arête de poisson |
| Préparation à gares, palettisation robot | Hors phase 0 | — |
