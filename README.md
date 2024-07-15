# dotnet-basic

cd /path/to/your/project/BR.App.Tests

dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=../coverage/

reportgenerator -reports:../coverage/lcov.info -targetdir:coverage_report -reporttypes:Html



dotnet tool install --global dotnet-reportgenerator-globaltool