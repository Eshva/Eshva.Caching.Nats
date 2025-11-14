dotnet-coverage collect "dotnet test --solution Eshva.Caching.Nats.slnx --retry-failed-tests 5" --output-format cobertura --output cobertura.xml
reportgenerator -reports:cobertura.xml -targetdir:coverage -reporttypes:Html_Dark -assemblyfilters:+Eshva.Caching.Nats -classfilters:-*Settings
./coverage/index.html
