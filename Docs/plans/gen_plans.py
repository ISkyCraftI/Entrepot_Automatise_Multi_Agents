"""Génère les plans de l'entrepôt (SVG) pour le projet Entrepot_Automatise_Multi_Agents.

Repère : 1 unité = 1 m. Origine = coin sud-ouest du bâtiment.
Plan : X vers l'est (droite), Z vers le nord (haut) -> identique au repère Unity vu de dessus.
Usage : python3 gen_plans.py  puis  for f in *.svg; do rsvg-convert -z 1.5 "$f" -o "${f%.svg}.png"; done
"""
from pathlib import Path

OUT = Path(__file__).parent


FONT = "Inter, DejaVu Sans, sans-serif"
C = dict(
    ink="#1f2933", grid="#e4e7eb", wall="#52606d", floor="#f7f9fb",
    rack="#cfe0fa", rack_line="#6f9ce0", bin="#2a6fdb", shuttle="#d7263d",
    lift="#7b8794", conv_bin="#8fb3e8", conv_box="#e9b77c", carton="#c97a2b",
    machine="#9aa5b1", station="#ffffff", phase1="#7c5cbf", charge="#2f9e44",
    todo="#e8590c", text_soft="#616e7c",
)


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


class Svg:
    def __init__(self, w, h):
        self.w, self.h, self.items = w, h, []

    def add(self, s):
        self.items.append(s)

    def text(self, x, y, s, size=12, weight=400, color=None, anchor="start", rotate=None):
        tr = f' transform="rotate({rotate} {x:.1f} {y:.1f})"' if rotate is not None else ""
        self.add(f'<text x="{x:.1f}" y="{y:.1f}" font-family="{FONT}" font-size="{size}" '
                 f'font-weight="{weight}" fill="{color or C["ink"]}" text-anchor="{anchor}"{tr}>{esc(s)}</text>')

    def rect(self, x, y, w, h, fill="none", stroke="none", sw=1, rx=0, dash=None, opacity=1):
        d = f' stroke-dasharray="{dash}"' if dash else ""
        self.add(f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" rx="{rx}" fill="{fill}" '
                 f'stroke="{stroke}" stroke-width="{sw}"{d} opacity="{opacity}"/>')

    def line(self, x1, y1, x2, y2, stroke, sw=1, dash=None, marker=None):
        d = f' stroke-dasharray="{dash}"' if dash else ""
        m = f' marker-end="url(#{marker})"' if marker else ""
        self.add(f'<line x1="{x1:.1f}" y1="{y1:.1f}" x2="{x2:.1f}" y2="{y2:.1f}" stroke="{stroke}" '
                 f'stroke-width="{sw}"{d}{m}/>')

    def poly(self, pts, stroke, sw=1, fill="none", dash=None, join="round", marker=None):
        d = f' stroke-dasharray="{dash}"' if dash else ""
        me = f' marker-end="url(#{marker})"' if marker else ""
        p = " ".join(f"{x:.1f},{y:.1f}" for x, y in pts)
        self.add(f'<polyline points="{p}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}" '
                 f'stroke-linejoin="{join}"{d}{me}/>')

    def save(self, name, defs=""):
        body = "\n".join(self.items)
        svg = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{self.w}" height="{self.h}" '
               f'viewBox="0 0 {self.w} {self.h}">\n<defs>{defs}</defs>\n'
               f'<rect width="100%" height="100%" fill="#ffffff"/>\n{body}\n</svg>\n')
        (OUT / name).write_text(svg, encoding="utf-8")


DEFS = f"""
<marker id="arr" viewBox="0 0 10 10" refX="5" refY="5" markerWidth="5" markerHeight="5" orient="auto">
  <path d="M0,1 L9,5 L0,9 z" fill="{C['ink']}"/></marker>
<marker id="arrB" viewBox="0 0 10 10" refX="5" refY="5" markerWidth="6" markerHeight="6" orient="auto">
  <path d="M0,1 L9,5 L0,9 z" fill="{C['bin']}"/></marker>
<marker id="arrC" viewBox="0 0 10 10" refX="5" refY="5" markerWidth="6" markerHeight="6" orient="auto">
  <path d="M0,1 L9,5 L0,9 z" fill="{C['carton']}"/></marker>
<marker id="arrP" viewBox="0 0 10 10" refX="5" refY="5" markerWidth="6" markerHeight="6" orient="auto">
  <path d="M0,1 L9,5 L0,9 z" fill="{C['phase1']}"/></marker>
<pattern id="hatch" width="8" height="8" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">
  <rect width="8" height="8" fill="#f3effb"/><line x1="0" y1="0" x2="0" y2="8" stroke="#cdbfea" stroke-width="3"/></pattern>
"""

# ---------------------------------------------------------------------------
# Paramètres de l'entrepôt (m) — modifiables
# ---------------------------------------------------------------------------
BW, BD = 100.0, 60.0            # bâtiment (X, Z)
XPTS_X0, XPTS_X1 = 24.0, 84.0   # bloc X-PTS en X
XPTS_Z0 = 49.4
RACK_D, AISLE_W = 0.65, 1.0     # profondeur rack simple, largeur allée navette
N_AISLES = 4
MODULE = 2 * RACK_D + AISLE_W   # 2,3 m
XPTS_Z1 = XPTS_Z0 + N_AISLES * MODULE
LIFT_W = 2.5
N_LEVELS, LEVEL_PITCH, BASE_H = 12, 0.5, 0.4
BAY = 1.2                       # travée : 2 bacs de 400 mm + jeux
STATIONS_X = [70.0, 76.0, 82.0, 88.0]
LANE_OUT_X, LANE_IN_X = 90.0, 92.0   # collecteurs vertical sortie / retour
LOOP_N, LOOP_S = 44.0, 42.0          # boucle bacs (z)
LOOP_W_X = 66.0
CARTON_Z = 21.0
STATION_Z = 27.0


def aisle_center(i):
    return XPTS_Z0 + i * MODULE + RACK_D + AISLE_W / 2


# ---------------------------------------------------------------------------
# 1. Plan de masse (vue de dessus)
# ---------------------------------------------------------------------------
def plan_masse():
    S = 11.0
    ox, oy = 70.0, 100.0
    legend_x = ox + BW * S + 40
    W, H = int(legend_x + 330), int(oy + BD * S + 130)
    g = Svg(W, H)

    def P(x, z):
        return ox + x * S, oy + (BD - z) * S

    def R(x0, z0, x1, z1, **kw):
        x, y = P(x0, z1)
        g.rect(x, y, (x1 - x0) * S, (z1 - z0) * S, **kw)

    def conv(pts, kind, arrows=True):
        col = C["conv_bin"] if kind == "bin" else C["conv_box"]
        mk = "arrB" if kind == "bin" else "arrC"
        q = [P(*p) for p in pts]
        g.poly(q, col, sw=0.8 * S, join="miter")
        if arrows:
            for (x1, y1), (x2, y2) in zip(q, q[1:]):
                L = ((x2 - x1) ** 2 + (y2 - y1) ** 2) ** 0.5
                if L < 30:
                    continue
                mx, my = (x1 + x2) / 2, (y1 + y2) / 2
                ux, uy = (x2 - x1) / L, (y2 - y1) / L
                g.line(mx - ux * 4, my - uy * 4, mx + ux * 2, my + uy * 2,
                       C["bin"] if kind == "bin" else C["carton"], sw=1.6, marker=mk)

    # Titre
    g.text(ox - 30, 36, "Plan de masse — entrepôt X-PTS (phase 0) + réservation phase 1", 22, 700)
    g.text(ox - 30, 60, "Vue de dessus · 1 unité Unity = 1 m · origine (0,0,0) au coin sud-ouest · "
           "cotes proposées, toutes paramétrables", 13, color=C["text_soft"])

    # Grille 10 m
    for x in range(0, int(BW) + 1, 10):
        g.line(*P(x, 0), *P(x, BD), C["grid"], 1)
        g.text(P(x, 0)[0], P(x, 0)[1] + 16, f"{x}", 10, color=C["text_soft"], anchor="middle")
    for z in range(0, int(BD) + 1, 10):
        g.line(*P(0, z), *P(BW, z), C["grid"], 1)
        g.text(P(0, z)[0] - 8, P(0, z)[1] + 4, f"{z}", 10, color=C["text_soft"], anchor="end")

    # Bâtiment
    R(0, 0, BW, BD, fill="none", stroke=C["wall"], sw=3)

    # --- Zone phase 1 (hachurée) ---
    R(26, 26, 62, 40.6, fill="url(#hatch)", stroke=C["phase1"], sw=1.5, dash="6 4")
    for i in range(2, 28, 3):
        for j in range(1, 11, 3):
            for a in range(2):
                for b in range(2):
                    x0 = 26 + (i + a) * 1.2 + 0.1
                    z0 = 26 + (j + b) * 1.2 + 0.4
                    if x0 + 1.0 < 61.5 and z0 + 1.0 < 38.0:
                        R(x0, z0, x0 + 1.0, z0 + 1.0, fill="#ffffff", stroke=C["phase1"], sw=0.8)
    for k in range(5):
        R(27 + k * 2.4, 26.4, 27 + k * 2.4 + 1.2, 27.6, fill=C["charge"], stroke="none")
    x, y = P(44, 39.4)
    g.text(x, y + 2, "PHASE 1 — Zone tampon étagères (AMR)", 12, 700, C["phase1"], "middle")
    x, y = P(44, 26.2)
    g.text(x, y + 14, "grille 1,2 m · étagères = NavMeshObstacle · ■ bases de recharge", 10.5,
           color=C["phase1"], anchor="middle")
    R(62.3, 29, 67.0, 36, fill="#ffffff", stroke=C["phase1"], sw=1.5, dash="6 4")
    x, y = P(64.65, 34.4)
    g.text(x, y, "Poste", 9.5, 700, C["phase1"], "middle")
    g.text(x, y + 13, "opérateur", 9.5, 700, C["phase1"], "middle")
    g.text(x, y + 26, "AMR", 9.5, 700, C["phase1"], "middle")
    R(57.8, 37.2, 66.2, 41.0, fill="#fff4e6", stroke=C["todo"], sw=1.8, dash="5 3")
    x, y = P(62.0, 40.0)
    g.text(x, y + 2, "Bacs → étagères", 10, 700, C["todo"], "middle")
    g.text(x, y + 15, "? À DÉFINIR", 10, 700, C["todo"], "middle")

    # --- Bloc X-PTS ---
    R(XPTS_X0, XPTS_Z0, XPTS_X1, XPTS_Z1, fill=C["rack"], stroke=C["rack_line"], sw=1.5)
    for i in range(N_AISLES):
        za = XPTS_Z0 + i * MODULE
        R(XPTS_X0, za, XPTS_X1, za + RACK_D, fill="#b5cff6", stroke="none")
        R(XPTS_X0, za + RACK_D + AISLE_W, XPTS_X1, za + MODULE, fill="#b5cff6", stroke="none")
        zc = aisle_center(i)
        g.line(*P(XPTS_X0, zc), *P(XPTS_X1, zc), C["shuttle"], 1.5, dash="10 6")
        for xs in (32 + i * 7, 58 - i * 5):
            R(xs, zc - 0.4, xs + 1.0, zc + 0.4, fill=C["shuttle"])
        R(XPTS_X1, zc - 0.9, XPTS_X1 + LIFT_W, zc + 0.9, fill=C["lift"], stroke=C["ink"], sw=1)
        x, y = P(XPTS_X1 + LIFT_W / 2, zc)
        g.text(x, y + 4, f"É{i + 1}", 10, 700, "#ffffff", "middle")
    for xb in range(int(XPTS_X0), int(XPTS_X1) + 1, 6):
        g.line(*P(xb, XPTS_Z0), *P(xb, XPTS_Z1), C["rack_line"], 0.6)
    for xm in (44, 64):
        g.line(*P(xm, XPTS_Z0 - 0.4), *P(xm, XPTS_Z1 + 0.4), C["ink"], 2.5, dash="2 3")
    x, y = P((XPTS_X0 + XPTS_X1) / 2, XPTS_Z0)
    g.text(x, y + 16, f"SYSTÈME À NAVETTES X-PTS · {N_AISLES} allées · {N_LEVELS} niveaux · "
           "1 navette / niveau / allée", 12, 700, C["ink"], "middle")
    x, y = P(XPTS_X0 - 1, (XPTS_Z0 + XPTS_Z1) / 2)
    g.text(x, y - 6, "Accès", 10, 600, C["text_soft"], "end")
    g.text(x, y + 7, "maintenance", 10, 600, C["text_soft"], "end")

    # --- Convoyeurs bacs ---
    for i in range(N_AISLES):
        zc = aisle_center(i)
        conv([(XPTS_X1 + LIFT_W, zc - 0.35), (LANE_OUT_X, zc - 0.35)], "bin", arrows=False)
        conv([(LANE_IN_X, zc + 0.35), (XPTS_X1 + LIFT_W, zc + 0.35)], "bin", arrows=False)
    conv([(LANE_OUT_X, XPTS_Z1 - 0.6), (LANE_OUT_X, LOOP_N), (LOOP_W_X, LOOP_N),
          (LOOP_W_X, LOOP_S), (LANE_IN_X, LOOP_S), (LANE_IN_X, XPTS_Z1 - 0.6)], "bin")
    for sx in STATIONS_X:
        conv([(sx - 0.5, LOOP_S), (sx - 0.5, STATION_Z + 0.5)], "bin")
        conv([(sx + 0.5, STATION_Z + 0.5), (sx + 0.5, LOOP_S)], "bin")
    R(73, 44.6, 79, 47.4, fill=C["machine"], stroke=C["ink"], sw=1)
    x, y = P(76, 46)
    g.text(x, y + 4, "Séquenceur", 10.5, 700, "#ffffff", "middle")

    # --- Convoyeurs cartons ---
    conv([(95, CARTON_Z), (27, CARTON_Z)], "box")
    for sx in STATIONS_X:
        conv([(sx + 1.6, CARTON_Z), (sx + 1.6, STATION_Z - 2.2)], "box", arrows=False)
        conv([(sx + 2.4, STATION_Z - 2.2), (sx + 2.4, CARTON_Z)], "box", arrows=False)
    R(94, 18.6, 99, 23.4, fill=C["machine"], stroke=C["ink"], sw=1)
    x, y = P(96.5, 21)
    g.text(x, y - 2, "Lancement", 10, 700, "#ffffff", "middle")
    g.text(x, y + 10, "colis", 10, 700, "#ffffff", "middle")
    R(42.8, 19.2, 53.2, 22.8, fill=C["machine"], stroke=C["ink"], sw=1)
    x, y = P(48, 21)
    g.text(x, y - 2, "JIVARO", 10.5, 700, "#ffffff", "middle")
    g.text(x, y + 10, "hauteur + fermeture", 9, 600, "#ffffff", "middle")

    # --- Postes de préparation ---
    for k, sx in enumerate(STATIONS_X):
        R(sx - 2.2, STATION_Z - 2.6, sx + 3.0, STATION_Z + 0.6, fill=C["station"], stroke=C["ink"], sw=1.5)
        R(sx - 1.0, STATION_Z - 1.4, sx, STATION_Z - 0.6, fill=C["bin"])
        R(sx + 1.4, STATION_Z - 1.5, sx + 2.6, STATION_Z - 0.5, fill=C["carton"])
        cx, cy = P(sx + 0.7, STATION_Z - 2.0)
        g.add(f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="5" fill="{C["shuttle"]}"/>')
        x, y = P(sx + 0.4, STATION_Z - 2.6)
        g.text(x, y + 14, f"P{k + 1}", 11, 700, C["ink"], "middle")
    x, y = P(80, 15.2)
    g.text(x, y, "POSTES GOODS-TO-PERSON P1–P4", 11.5, 700, C["ink"], "middle")
    g.text(x, y + 14, "(bac source → carton commande)", 10, color=C["text_soft"], anchor="middle")

    # --- Trieur d'expédition (arête de poisson) ---
    conv([(27.0, CARTON_Z), (3.0, CARTON_Z)], "box", arrows=False)
    for xc in range(7, 26, 3):
        for sgn in (1, -1):
            x2, z2 = xc - 3.0, CARTON_Z + sgn * 4.0
            g.line(*P(xc, CARTON_Z), *P(x2, z2), C["conv_box"], 0.6 * S)
            pz = z2 + (0.3 if sgn > 0 else -1.5)
            R(x2 - 0.6, pz, x2 + 0.6, pz + 1.2, fill="#f1d3ad", stroke=C["carton"], sw=1)
    x, y = P(15, CARTON_Z + 7.4)
    g.text(x, y, "TRIEUR D'EXPÉDITION", 11.5, 700, C["ink"], "middle")
    g.text(x, y + 14, "goulottes → palettes", 10, color=C["text_soft"], anchor="middle")

    for k in range(6):
        R(3 + k * 4, 0, 3 + k * 4 + 3, 1.0, fill=C["wall"])
    x, y = P(15, 1.0)
    g.text(x, y - 8, "Quais d'expédition", 11, 600, C["text_soft"], "middle")

    # --- Cotes ---
    def dim_h(x0, x1, z, label, off):
        a, b = P(x0, z), P(x1, z)
        yy = a[1] + off
        g.line(a[0], yy, b[0], yy, C["ink"], 1)
        for xx in (a[0], b[0]):
            g.line(xx, yy - 5, xx, yy + 5, C["ink"], 1)
        g.text((a[0] + b[0]) / 2, yy - 5, label, 11, 600, C["ink"], "middle")

    dim_h(0, BW, 0, f"{BW:.0f} m", off=40)
    dim_h(XPTS_X0, XPTS_X1, BD, f"{XPTS_X1 - XPTS_X0:.0f} m", off=-14)
    a, b = P(0, 0), P(0, BD)
    g.line(a[0] - 34, a[1], b[0] - 34, b[1], C["ink"], 1)
    g.text(a[0] - 40, (a[1] + b[1]) / 2, f"{BD:.0f} m", 11, 600, C["ink"], "middle", rotate=-90)
    a, b = P(XPTS_X0, XPTS_Z0), P(XPTS_X0, XPTS_Z1)
    g.text(a[0] + 6, (a[1] + b[1]) / 2 + 4, f"{XPTS_Z1 - XPTS_Z0:.1f} m", 10, 700, C["ink"])
    a, b = P(STATIONS_X[0] + 2.0, 18.4), P(STATIONS_X[1] + 2.0, 18.4)
    g.line(a[0], a[1], b[0], b[1], C["ink"], 1)
    for xx in (a[0], b[0]):
        g.line(xx, a[1] - 4, xx, a[1] + 4, C["ink"], 1)
    g.text((a[0] + b[0]) / 2, a[1] + 13, "6 m", 10, 600, C["ink"], "middle")

    # Axes Unity
    ax, ay = P(0, 0)
    ax, ay = ax + 4, ay + 108
    g.line(ax, ay, ax + 50, ay, C["shuttle"], 2.5, marker="arr")
    g.line(ax, ay, ax, ay - 30, "#2f9e44", 2.5, marker="arr")
    g.text(ax + 58, ay + 4, "X (est)", 11, 700, C["shuttle"])
    g.text(ax + 8, ay - 28, "Z (nord)", 11, 700, "#2f9e44")
    g.text(ax + 130, ay + 4, "Y = hauteur (vers le haut)", 11, color=C["text_soft"])

    # --- Légende ---
    lx, ly = legend_x, oy
    g.text(lx, ly, "Légende", 15, 700)
    ly += 6
    items = [
        ("rect", C["rack"], C["rack_line"], "Rack X-PTS (bacs bleus)"),
        ("dash", C["shuttle"], None, "Allée de navette (rail)"),
        ("rect", C["shuttle"], None, "Navette (1 par niveau)"),
        ("rect", C["lift"], C["ink"], "Élévateur double berceau (É1–É4)"),
        ("conv", C["conv_bin"], C["bin"], "Convoyeur bacs (flux bleu)"),
        ("conv", C["conv_box"], C["carton"], "Convoyeur cartons (flux orange)"),
        ("rect", C["machine"], C["ink"], "Machine (séquenceur, Jivaro…)"),
        ("rect", "#ffffff", C["ink"], "Poste de préparation (P1–P4)"),
        ("dot", C["shuttle"], None, "Opérateur"),
        ("hatch", None, C["phase1"], "Réservé phase 1 (AMR)"),
        ("rect", C["charge"], None, "Base de recharge AMR"),
        ("todo", "#fff4e6", C["todo"], "Décision à prendre"),
    ]
    for kind, f, s, label in items:
        ly += 26
        if kind == "rect":
            g.rect(lx, ly - 12, 26, 14, fill=f, stroke=s or "none", sw=1)
        elif kind == "dash":
            g.line(lx, ly - 5, lx + 26, ly - 5, f, 2, dash="6 4")
        elif kind == "conv":
            g.line(lx, ly - 5, lx + 26, ly - 5, f, 8)
            g.line(lx + 8, ly - 5, lx + 16, ly - 5, s, 1.6, marker="arr")
        elif kind == "dot":
            g.add(f'<circle cx="{lx + 13}" cy="{ly - 5}" r="5" fill="{f}"/>')
        elif kind == "hatch":
            g.rect(lx, ly - 12, 26, 14, fill="url(#hatch)", stroke=s, sw=1, dash="4 3")
        elif kind == "todo":
            g.rect(lx, ly - 12, 26, 14, fill=f, stroke=s, sw=1.5, dash="4 3")
        g.text(lx + 36, ly, label, 12)

    ly += 44
    g.text(lx, ly, "Flux", 15, 700)
    flows = [
        (C["bin"], "Bacs : rack → navette → élévateur →"),
        (C["bin"], "   séquenceur → poste → retour rack"),
        (C["carton"], "Cartons : lancement → poste →"),
        (C["carton"], "   Jivaro → trieur → palette"),
        (C["phase1"], "Phase 1 : étagère → AMR → poste AMR"),
        (C["phase1"], "   → retour zone tampon"),
    ]
    for col, t in flows:
        ly += 20
        g.text(lx, ly, t, 11.5, 500, col)

    ly += 40
    g.text(lx, ly, "Simplifications vs vidéo", 15, 700)
    for t in ["• Mezzanine ramenée au sol (1 seul niveau)",
              "• Préparation à gares et palettisation",
              "  robot : hors périmètre",
              "• 4 allées × 12 niveaux au lieu de ~20 m",
              "  de haut (N_AISLES, N_LEVELS)"]:
        ly += 19
        g.text(lx, ly, t, 11.5, color=C["text_soft"])

    g.save("plan_masse.svg", DEFS)


# ---------------------------------------------------------------------------
# 2. Coupe transversale + élévation de tête d'allée
# ---------------------------------------------------------------------------
def coupe():
    S = 62.0
    W, H = 1520, 680
    g = Svg(W, H)
    g.text(40, 40, "Rack X-PTS — coupe transversale A-A et tête d'allée", 22, 700)
    g.text(40, 64, f"{N_AISLES} allées · {N_LEVELS} niveaux · pas vertical {LEVEL_PITCH} m · "
           f"bac 600 × 400 × 300 mm · cotes en mètres", 13, color=C["text_soft"])

    ox, base = 70.0, 565.0
    total_w = N_AISLES * MODULE
    top = BASE_H + N_LEVELS * LEVEL_PITCH

    def P(z, y):
        return ox + z * S, base - y * S

    g.text(ox, 112, "COUPE A-A (vue depuis la tête d'allée · Z → droite · Y ↑)", 13, 700)
    g.line(*P(-0.4, 0), *P(total_w + 0.4, 0), C["wall"], 3)
    for i in range(N_AISLES):
        z0 = i * MODULE
        for zr in (z0, z0 + RACK_D + AISLE_W):
            for zz in (zr, zr + RACK_D):
                g.line(*P(zz, 0), *P(zz, top), C["rack_line"], 2)
            for lv in range(N_LEVELS):
                y = BASE_H + lv * LEVEL_PITCH
                g.line(*P(zr, y), *P(zr + RACK_D, y), C["rack_line"], 1.5)
                x, yy = P(zr + 0.025, y + 0.3)
                g.rect(x, yy, 0.6 * S, 0.3 * S, fill=C["bin"], opacity=0.9 if (lv + i) % 5 else 0.25)
        za = z0 + RACK_D
        for lv in range(N_LEVELS):
            y = BASE_H + lv * LEVEL_PITCH
            g.line(*P(za, y), *P(za + AISLE_W, y), C["ink"], 1)
            x, yy = P(za + 0.05, y + 0.12)
            g.rect(x, yy, 0.9 * S, 0.12 * S, fill=C["shuttle"], rx=2)
        x, yy = P(za + AISLE_W / 2, top + 0.25)
        g.text(x, yy, f"allée {i + 1}", 12, 700, C["shuttle"], "middle")

    def hdim(z0, z1, label, dy):
        a, b = P(z0, 0), P(z1, 0)
        yy = a[1] + dy
        g.line(a[0], yy, b[0], yy, C["ink"], 1)
        for xx in (a[0], b[0]):
            g.line(xx, yy - 5, xx, yy + 5, C["ink"], 1)
        g.text((a[0] + b[0]) / 2, yy - 6, label, 12, 600, C["ink"], "middle")

    hdim(0, RACK_D, f"{RACK_D}", 26)
    hdim(RACK_D, RACK_D + AISLE_W, f"{AISLE_W}", 26)
    hdim(RACK_D + AISLE_W, MODULE, f"{RACK_D}", 26)
    hdim(0, total_w, f"{total_w:.1f} m ({N_AISLES} modules de {MODULE:.2f} m)", 58)
    a, b = P(total_w + 0.35, 0), P(total_w + 0.35, top)
    g.line(a[0], a[1], b[0], b[1], C["ink"], 1)
    g.text(a[0] + 16, (a[1] + b[1]) / 2, f"≈ {top:.1f} m", 12, 600, C["ink"], "middle", rotate=-90)
    a, b = P(total_w + 0.75, BASE_H + 2 * LEVEL_PITCH), P(total_w + 0.75, BASE_H + 3 * LEVEL_PITCH)
    g.line(a[0], a[1], b[0], b[1], C["ink"], 1)
    for yy in (a[1], b[1]):
        g.line(a[0] - 4, yy, a[0] + 4, yy, C["ink"], 1)
    g.text(a[0] + 8, (a[1] + b[1]) / 2 + 4, f"pas {LEVEL_PITCH}", 11, 600, C["ink"])

    # --- Tête d'allée (X-Y) ---
    ox2, S2 = 820.0, 52.0

    def Q(x, y):
        return ox2 + x * S2, base - y * S2

    g.text(ox2, 112, "TÊTE D'ALLÉE (vue de côté · X → droite · Y ↑)", 13, 700)
    g.line(*Q(-0.2, 0), *Q(12.6, 0), C["wall"], 3)
    nb = 5
    for b in range(nb + 1):
        g.line(*Q(b * BAY, 0), *Q(b * BAY, top), C["rack_line"], 2)
    for lv in range(N_LEVELS):
        y = BASE_H + lv * LEVEL_PITCH
        g.line(*Q(0, y), *Q(nb * BAY, y), C["rack_line"], 1.2)
        for b in range(nb):
            for s in range(2):
                if (b + s + lv) % 6 == 0:
                    continue
                x, yy = Q(b * BAY + 0.15 + s * 0.5, y + 0.3)
                g.rect(x, yy, 0.4 * S2, 0.3 * S2, fill=C["bin"])
    x, yy = Q(2.0, BASE_H + 6 * LEVEL_PITCH + 0.12)
    g.rect(x, yy, 0.9 * S2, 0.12 * S2, fill=C["shuttle"], rx=2)
    x, yy = Q(2.45, BASE_H + 6 * LEVEL_PITCH + 0.1)
    g.text(x, yy - 14, "navette", 11, 700, C["shuttle"], "middle")
    lx0 = nb * BAY + 0.3
    x, yy = Q(lx0, top + 0.3)
    g.rect(x, yy, 1.6 * S2, (top + 0.3) * S2, fill="none", stroke=C["lift"], sw=3)
    for yc in (2.6, 3.1):
        x, yy = Q(lx0 + 0.15, yc + 0.35)
        g.rect(x, yy, 1.3 * S2, 0.35 * S2, fill=C["lift"], rx=3)
        for s in range(2):
            x, yy = Q(lx0 + 0.25 + s * 0.6, yc + 0.32)
            g.rect(x, yy, 0.5 * S2, 0.28 * S2, fill=C["bin"])
    x, yy = Q(lx0 + 0.8, top + 0.55)
    g.text(x, yy, "Élévateur", 12, 700, C["ink"], "middle")
    x, yy = Q(lx0 + 1.9, 3.3)
    g.text(x, yy, "2 berceaux × 2 bacs", 11.5, 600, C["ink"])
    g.text(x, yy + 15, "= 4 charges / cycle", 11.5, 600, C["ink"])
    for k, (yc, lab) in enumerate(((0.9, "sortie → boucle bacs"), (0.5, "retour ← boucle bacs"))):
        g.line(*Q(lx0 + 1.6, yc), *Q(12.4, yc), C["conv_bin"], 10)
        x, yy = Q(lx0 + 1.8, yc)
        g.text(x + 4, yy - 9 if k == 0 else yy + 20, lab, 11, 600, C["bin"])
    x, yy = Q(2.5 * BAY, 0)
    g.text(x, yy + 22, f"travée {BAY} m = 2 bacs de 400 mm", 11.5, 600, C["ink"], "middle")

    g.text(40, 660, "Vidéo SAVOYE X-PTS : élévateur jusqu'à 4 charges/cycle, une navette par niveau, "
           "stockage jusqu'à 20 m. Les dimensions ci-dessus sont une proposition pour Unity.", 12,
           color=C["text_soft"])
    g.save("coupe_xpts.svg", DEFS)


# ---------------------------------------------------------------------------
# 3. Schéma de flux
# ---------------------------------------------------------------------------
def flux():
    W, H = 1500, 640
    g = Svg(W, H)
    g.text(40, 40, "Schéma de flux — agents et circulation des charges", 22, 700)
    g.text(40, 64, "Bleu = bacs (stock) · Orange = cartons (commandes) · Violet pointillé = phase 1 · "
           "Bord épais = agent candidat", 13, color=C["text_soft"])

    def box(x, y, w, h, title, sub="", fill="#ffffff", stroke=C["ink"], dash=None, agent=False, tc=None):
        g.rect(x, y, w, h, fill=fill, stroke=stroke, sw=3.5 if agent else 1.5, rx=8, dash=dash)
        g.text(x + w / 2, y + h / 2 - (4 if sub else -5), title, 14, 700, tc or C["ink"], "middle")
        if sub:
            g.text(x + w / 2, y + h / 2 + 15, sub, 11, 500, C["text_soft"], "middle")

    def arrow(pts, col, mk, label=None, lpos=None, dash=None):
        g.poly(pts, col, sw=2.5, dash=dash, marker=mk)
        if label:
            g.text(lpos[0], lpos[1], label, 11, 600, col, "middle")

    y1, y2, y3 = 120, 300, 470
    box(40, y1, 190, 80, "Rack X-PTS", "stock « infini » (phase 0)", fill=C["rack"])
    box(290, y1, 170, 80, "Navette", "1 / niveau / allée", agent=True, fill="#fde8eb", stroke=C["shuttle"])
    box(520, y1, 170, 80, "Élévateur", "4 bacs / cycle", agent=True, fill="#eef0f2", stroke=C["lift"])
    box(750, y1, 190, 80, "Boucle bacs", "convoyeurs + aiguillages", fill="#eaf2fd", stroke=C["bin"])
    box(1000, y1, 170, 80, "Séquenceur", "ordre des bacs / colis", fill="#eef0f2", stroke=C["machine"])
    box(1000, y2, 170, 90, "Poste G2P", "bac → carton", agent=True)
    box(1230, y2 - 70, 230, 70, "Ordonnanceur / WCS", "assigne bacs, cartons, postes", agent=True,
        fill="#fff9db", stroke="#e0a800")
    box(1230, y2 + 40, 230, 60, "Opérateur", "temps de picking aléatoire", fill="#fde8eb", stroke=C["shuttle"])
    box(750, y3, 190, 80, "Lancement colis", "carton vide", fill="#fdf1e3", stroke=C["carton"])
    box(1000, y3, 170, 80, "Jivaro", "hauteur + fermeture", fill="#fdf1e3", stroke=C["carton"])
    box(1230, y3, 230, 80, "Trieur d'expédition", "→ palettes / quais", fill="#fdf1e3", stroke=C["carton"])

    B, Cc, Pp = C["bin"], C["carton"], C["phase1"]
    for xa, xb in ((230, 282), (460, 512), (690, 742), (940, 992)):
        arrow([(xa, y1 + 40), (xb, y1 + 40)], B, "arrB")
    arrow([(1085, y1 + 80), (1085, y2 - 8)], B, "arrB", "bac source", (1135, y1 + 140))
    arrow([(1000, y2 + 25), (870, y2 + 25), (870, y1 + 88)], B, "arrB", "bac retour au stock", (935, y2 + 17))
    arrow([(845, y3), (845, y2 + 70), (992, y2 + 70)], Cc, "arrC", "carton vide", (915, y2 + 90))
    arrow([(1085, y2 + 90), (1085, y3 - 8)], Cc, "arrC", "carton plein", (1140, y2 + 140))
    arrow([(1170, y3 + 40), (1222, y3 + 40)], Cc, "arrC")
    arrow([(1230, y2 + 70), (1178, y2 + 70)], C["shuttle"], "arr")
    arrow([(1345, y2), (1345, y2 + 32)], "#e0a800", "arr")
    g.poly([(1460, y2 - 35), (1480, y2 - 35), (1480, y1 - 20), (375, y1 - 20), (375, y1 - 8)],
           "#e0a800", sw=1.5, dash="3 3", marker="arr")
    g.text(930, y1 - 26, "ordres (ordonnanceur → navettes, élévateurs, postes)", 11, 600, "#b08900", "middle")

    box(40, y2 + 10, 230, 90, "Zone tampon étagères", "grille, NavMeshObstacle", fill="url(#hatch)",
        stroke=Pp, dash="6 4", tc=Pp)
    box(340, y2 + 10, 190, 90, "AMR / robot-tortue", "à vide · chargé (2 agents)", stroke=Pp, dash="6 4",
        agent=True, tc=Pp)
    box(340, y3 + 10, 190, 70, "Poste opérateur AMR", "phase 1", stroke=Pp, dash="6 4", tc=Pp)
    box(570, y2 + 10, 150, 90, "Bacs → étagères", "? À DÉFINIR", fill="#fff4e6", stroke=C["todo"],
        dash="5 3", tc=C["todo"])
    arrow([(790, y1 + 80), (790, y1 + 125), (645, y1 + 125), (645, y2 + 2)], Pp, "arrP", dash="6 4")
    arrow([(570, y2 + 55), (538, y2 + 55)], Pp, "arrP", dash="6 4")
    arrow([(340, y2 + 55), (278, y2 + 55)], Pp, "arrP", dash="6 4")
    arrow([(155, y2 + 100), (155, y3 + 45), (332, y3 + 45)], Pp, "arrP", "étagère → opérateur",
          (240, y3 + 35), dash="6 4")
    arrow([(435, y3 + 10), (435, y2 + 108)], Pp, "arrP", dash="6 4")

    g.text(40, 610, "Agents candidats (bord épais) : navette, élévateur, poste, AMR, ordonnanceur. "
           "Les convoyeurs restent des objets passifs pilotés par l'ordonnanceur — à discuter.", 12,
           color=C["text_soft"])
    g.save("schema_flux.svg", DEFS)


if __name__ == "__main__":
    plan_masse()
    coupe()
    flux()
    print("ok")
