# Konzept.md: Astrophysikalische 3D-Code-Observability für agentische Softwareentwicklung

---

## 1. Motivation & Ausgangslage

Bei der agentischen Softwareentwicklung verschiebt sich die Rolle des Entwicklers vom aktiven Coder hin zum Architekten, Orchestrator und Auditor. Da autonome KI-Agenten Codezeilen in hoher Frequenz modifizieren, refaktorisieren und erweitern, geht das klassische mentale Modell („Welche Datei macht was?“) verloren.

Bestehende Werkzeuge lösen dieses Problem nicht:

* **Klassische Architektur-Tools (z. B. NDepend):** Zu analytisch, matrixorientiert, statisch.
* **Git-Visualisierer (z. B. Gource):** Rein dateibasiert, ohne Verständnis für Aufrufsemantik oder Methodenketten.
* **Code-Health-Tools (z. B. CodeScene):** Stark auf Reports und 2D-Kreise fokussiert, kein interaktives "Gefühl" für Software-Räume.

**Das Ziel:** Ein visuelles 3D-Cockpit, das eine C#-Codebase als dynamisch agierendes, physikalisches Universum rendert. Das System soll auf einen Blick Stabilität, Gefahrenherde, Testlücken und Live-Aktivitäten autonomer Agenten intuitiv erfassbar machen.

---

## 2. Das Metaphern-Modell: Das Code-Universum

Statt Code in statischen Bäumen oder Ordnern darzustellen, nutzt das System Gesetze der Himmelsmechanik und Thermodynamik.

### 2.1 Himmelskörper (Knoten)

* **Sonnensysteme (Klassen):** Eine Klasse ist kein simpler Punkt, sondern ein lokales Schwerefeld.
* **Monde / Trabanten (Methoden & Properties):** Sie kreisen in stabilen Orbits um ihr Klassen-Gravitationszentrum. Private Methoden kreisen innen, `public`-Methoden im Außenorbit.
* **Sonnengröße & Masse (Einstiegspunkte):** Komponenten mit Außenwirkung (Blazor-Pages, API-Controller, CLI-Einstiege) besitzen maximale Masse. Sie ziehen nachgelagerte Business-Services über Gravitationsvektoren in ihre Umlaufbahn.
* **Erkaltete Zwerge:** Stabile, seit Monaten unveränderte Klassen mit 100 % Testabdeckung. Sie sind dunkel, kompakt und ruhend im Zentrum des Systems verankert.

### 2.2 Git-Dynamik & Gefahrenobjekte

* **Pulsare (Hohe Frequenz):** Methoden oder Klassen mit hoher Commit-Frequenz in den letzten 14 Tagen. Sie pulsieren rhythmisch im Takt ihrer Änderungsrate.
* **Magnetare (Hotspots):** Hohe Komplexität gepaart mit brutalem Git-Churn. Sie strahlen visuelle Plasma-Blitze ab und verzerren das umliegende Gravitationsfeld (akute Bruchgefahr für Agenten).
* **Supernovae (Refactoring-Explosionen):** Wenn ein Agent eine zentrale Klasse aufbricht, explodiert der Knoten visuell, stößt alte Monde ab und bildet neue Planetenkerne.

### 2.3 Qualität, Tests & Komplexität

* **Hitze / Temperatur (Kognitive Last):**
* *Cyan / Blau:* Trivialer Code, geringe zyklomatische Komplexität (Cyclomatic Complexity < 3).
* *Gelb / Bernstein:* Gewachsene Logik mit mittlerer Verzweigungstiefe.
* *Glühendes Rotorange:* Hochkomplexe Verschachtelungen, Guard-Kaskaden oder State-Machine-Ungetüme.


* **Orbitale Schutzschilde (Test-Abdeckung):**
* *Blauer/Grüner Energieschild:* Eine Methode verfügt über solide Unit-Tests. Der Schild umschließt den Mond.
* *Nackter Fels:* Keine Tests vorhanden. Die Methode ist ungeschützt gegen Seiteneffekte von Refactorings.
* *Weltraumschrott (Schild-Überlastung):* Überproportional viele fragile Tests belagern eine triviale Methode.



### 2.4 Gravitationslinien (Kanten / Aufrufe)

* Gerichtete Laser-/Partikelströme repräsentieren `Call`-Beziehungen (`foo.bar()` ruft `ziel.baz()` auf).
* Die Flussgeschwindigkeit von Lichtpartikeln auf den Kanten visualisiert die Aufrufdichte oder Relevanz.

---

## 3. Interaktion & Agenten-Cockpit

### 3.1 Semantic Zoom (3-Ebenen-Fokus)

1. **Galaxie-Ebene (Namespaces / Bounded Contexts):** Nur Gravitationscluster und dicke Transportströme sind sichtbar. Erkennt sofort Architektur-Verklumpungen (God Classes, zyklische Modulabhängigkeiten).
2. **System-Ebene (Klassen):** Klassen-Sonnensysteme treten in den Vordergrund. Interfaces fungieren als Andockpunkte.
3. **Oberflächen-Ebene (Methoden & State):** Kameraschwenk direkt an eine Klasse heran. Einzelne Statements, Parameter und private Hilfsmethoden werden im Orbit sichtbar.

### 3.2 Spotlight-Isolation

Ein Klick auf einen Mond/Planeten dimmt 95 % des restlichen Universums in den Hintergrund:

* **Upstream (Wer ruft mich auf?):** Leuchtet in Signalgelb auf.
* **Downstream (Wen rufe ich auf?):** Leuchtet in Neon-Cyan auf.
* Der restliche transitive Call-Graph wird sofort als klare Kette isoliert.

### 3.3 Live-Agenten-Radar (Realtime Watch Mode)

* **Agenten-Spotlight:** Während ein Coder-Agent Code generiert, wird die betroffene Klasse von einem Scan-Strahl beleuchtet.
* **Live-Pulsieren:** Schreibt der Agent Code, schlägt der Knoten optisch aus.
* **Test-Run-Feedback:** Ein erfolgreicher Testlauf lässt den Schutzschild der Klasse aufblitzen; ein roter Test bricht den Schild funkensprühend auf.

---

## 4. Technische Architektur

Das System setzt auf eine strikte Trennung zwischen **Analyse-Engine** (C# .NET) und **Render-Frontend** (WebGL/Browser).

```
┌─────────────────────────────────────────────────────────────┐
│                       C# .NET 10 BACKEND                    │
│                                                             │
│  ┌──────────────────────┐      ┌─────────────────────────┐  │
│  │  Roslyn Workspace    │      │       LibGit2Sharp      │  │
│  │  • Single-Pass AST   │      │  • Commit Churn         │  │
│  │  • SemanticModel     │      │  • Recency              │  │
│  └──────────┬───────────┘      └────────────┬────────────┘  │
│             │                               │               │
│             └───────────────┬───────────────┘               │
│                             ▼                               │
│                ┌─────────────────────────┐                  │
│                │   Graph Aggregation     │                  │
│                │   • Node Pruning        │                  │
│                │   • Mass & Metric Calc  │                  │
│                └────────────┬────────────┘                  │
│                             ▼                               │
│                ┌─────────────────────────┐                  │
│                │   Embedded Web Server   │                  │
│                │   (Kestrel + SignalR)   │                  │
│                └────────────┬────────────┘                  │
└─────────────────────────────┼───────────────────────────────┘
                              │ JSON Stream / Deltas
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    WEBGL / JS FRONTEND                      │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ 3D Force-Directed Engine (Three.js / 3d-force-graph)  │  │
│  │ • Global Force: Klassen-Sonnensysteme                │  │
│  │ • Local Orbits: Methoden-Monde                        │  │
│  │ • Post-Processing: Bloom, Glow, Particle Trails      │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘

```

### 4.1 Backend: Analyse-Pipeline

* **`Microsoft.CodeAnalysis.CSharp.Workspaces`:**
* Öffnet `.sln` oder `.csproj`.
* Kein teures `SymbolFinder.FindReferencesAsync` im Loop. Stattdessen ein hochparalleler `CSharpSyntaxWalker`, der pro Dokument alle `InvocationExpressionSyntax`- und `MemberAccessExpressionSyntax`-Knoten einmalig traversiert und über das `SemanticModel` auflöst.


* **`LibGit2Sharp`:**
* Paralleles Auslesen der Commit-Historie.
* Zuweisung von *Churn-Rate* (geänderte Zeilen der letzten $N$ Tage) und *Recency* auf Dateiebene.


* **Kestrel & SignalR / WebSockets:**
* Startet einen leichten lokalen Server auf `localhost:XXXX`.
* Sendet beim Initialstart den Gesamtgraphen.
* Sendet bei Dateiänderungen (via `FileSystemWatcher`) nur noch schlanke Diff-Pakete.



### 4.2 Frontend: Visualisierungs-Engine

* **Rendering-Stack:** `3d-force-graph` oder `Cosmograph` (Three.js / WebGL / WebGPU).
* **Shader-Effekte:** UnrealBloomPass für strahlende Sonnen, rotierende Partikelringe für Test-Orbits.

---

## 5. Skalierung auf 180.000+ LOC

Um Performance-Einbrüche und visuelles Chaos bei großen Codebases zu verhindern, greifen drei Heuristiken:

1. **Intelligentes Pruning (Rauschunterdrückung):**
* Auto-Properties (`public int Id { get; set; }`) werden nicht als Monde gerendert, sondern erhöhen lediglich die Masse der Eltern-Klasse.
* Reine Boilerplate-Klassen (DTOs, Records ohne Methoden, leere Interfaces) werden als kompakte Asteroidengürtel statt voller Sonnensysteme gerendert.
* Reduktion von ~15.000 potenziellen Knoten auf ~3.000 relevante Logik-Knoten.


2. **Hierarchische Kraftfeldberechnung:**
* Die CPU/GPU berechnet die globale Abstoßung/Anziehung ausschließlich zwischen den ~1.000 Klassen-Sonnen.
* Methoden-Monde haben feste oder elastische Lokal-Orbits um ihr Klassenzentrum und interagieren physikalisch nicht mit fremden Monden.


3. **Parallele Multi-Thread-Pipeline:**
* Nutzung moderner Multi-Core-CPUs: Parallele Kompilierung und Analyse via `Parallel.ForEachAsync` über alle Projekte und Dokumente der Solution.
* Initialer Analyse-Durchlauf für 180k LOC: Zielzeit < 5 Sekunden.



---

## 6. Datenstruktur-Entwurf (`graph.json`)

```json
{
  "meta": {
    "solution": "EnterpriseApp.sln",
    "totalLoc": 180420,
    "timestamp": "2026-09-03T18:40:00Z"
  },
  "nodes": [
    {
      "id": "App.Core.OrderService",
      "label": "OrderService",
      "kind": "class",
      "type": "planet",
      "mass": 45,
      "churnScore": 82,
      "isPulsar": true,
      "metrics": {
        "loc": 620,
        "cyclomaticComplexity": 18
      }
    },
    {
      "id": "App.Core.OrderService.ProcessPayment",
      "parentId": "App.Core.OrderService",
      "label": "ProcessPayment()",
      "kind": "method",
      "type": "moon",
      "temperature": 0.85,
      "testCoverage": 0.95,
      "shieldActive": true
    }
  ],
  "links": [
    {
      "source": "App.Api.OrderController.Post",
      "target": "App.Core.OrderService.ProcessPayment",
      "callCount": 12,
      "isCrossNamespace": true
    }
  ]
}

```

---

## 7. Namensvorschläge für das Projekt

### Kategorie A: Astronomie & C# / .NET Symbiose

* **RoslynVerse** – Direkte Verbindung der Microsoft-Compiler-Plattform mit dem Universums-Konzept.
* **CosmoNet** – Klingt elegant, fokussiert auf das kosmische .NET-System.
* **OrbiSharp** – Kurz, einprägsam; fokussiert auf die planetaren Orbits und C#.
* **Astroslyn** – Kraftvolle Wortschöpfung aus *Astronomie* und *Roslyn*.
* **SharpOrbit** – Klar, technisch und modern.

### Kategorie B: Taktische Observability & Phänomene

* **Magnetar.NET** – Benannt nach den intensivsten Sternen; signalisiert Energie und Hotspot-Erkennung.
* **PulsarScope** – Impliziert Rhythmus, Messung und Live-Überwachung.
* **CodeNebula** – Bildhaft; steht für die Entstehung und Struktur von Codesystemen.
* **ApexHorizon** – Klingt nach High-End-Cockpit und Beobachtungsstation.
* **NovaSharp** – Assoziiert plötzliche Erleuchtung, Klarheit und Energie.

### Kategorie C: Kurz & Marken-tauglich

* **AetherCode**
* **VortexDotNet**
* **StellarC#**

---

## 8. Umsetzungs-Fahrplan (MVP)

* **Phase 1 (Headless Parser):** C#-Konsolen-Tool mit Roslyn Workspace. Traversiert eine `.sln`, sammelt Klassen, Methoden, Invocation-Kanten und exportiert eine statische `graph.json`.
* **Phase 2 (Minimal 3D View):** Lokale HTML-Seite mit `3d-force-graph`. Lädt die JSON und rendert Klassen als Kugeln und Aufrufe als Lichtlinien.
* **Phase 3 (Orbits & Visuelle Grammatik):** Aufteilung in Klassen-Zentren und Methoden-Monde. Anbindung von Shadern für Hitze (Komplexität) und Schutzschilde (Tests).
* **Phase 4 (Git-Integration):** Einbindung von `LibGit2Sharp`. Churn-Metriken lassen Hotspots zu Pulsaren und Magnetaren werden.
* **Phase 5 (Agenten-Cockpit & Live-Sync):** Kestrel-Websocket integrieren, `FileSystemWatcher` scharfschalten und Dateiänderungen in Echtzeit als visuelle Impulse streamen.

---