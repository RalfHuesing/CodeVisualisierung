# Graph Universe Contract

Dieses Verzeichnis ist die gemeinsame Grenze zwischen Viewer und Datenquellen.

- `schema/graph-universe.schema.json`: maschinenprüfbarer Vertrag
- `fixtures/`: kleine Test- und Beispieldaten

Die Visualisierung darf keine Bedeutung aus unbekannten Feldnamen erraten. Neue Metriken werden benannt angeliefert und können über `metricDefinitions` erklärt werden.
Rohmetriken tragen `valueKind: "raw"`; bereits global normalisierte Scores tragen
`valueKind: "normalized-score"` und werden nicht aus einer sichtbaren Teilmenge
neu skaliert. Summary-Links verweisen über `derivedFrom` und ihre
`attributes.aggregation` auf die vollständigen Detailbeziehungen.

Der C#-Adapter validiert jede Ausgabe vor dem atomaren Schreiben gegen dieses
Schema. Die Referenz-Fixture und die Viewer-Vertragstests bleiben damit die
gemeinsame Prüfung für alle Datenquellen.
