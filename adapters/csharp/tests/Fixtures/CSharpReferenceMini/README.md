# CSharpReferenceMini

Diese kleine physische Solution ist die Referenz für die Roslyn- und
`MSBuildWorkspace`-Integration des implementierten C#-Adapters. Sie zeigt eine
Contract-/Application-Referenz und ein separates Testprojekt. Sie gehört
bewusst nicht zur produktiven Adapter-Solution; CLI- und Integrationstests
laden sie gezielt über ihren absoluten Fixture-Pfad und erzeugen daraus
Graph-Universe-JSON.
