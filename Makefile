all: pack

test:
	dotnet run --project src/Samples/ProducerApp
	dotnet run --project src/Samples/ConsumerApp

perf:
	dotnet run --project src/Samples/PerformanceTests

pack: rebuild
	mkdir -p `pwd`/packages
	dotnet pack -c Release `pwd`/src/RabbitMQTopic/
	mv `pwd`/src/RabbitMQTopic/bin/Release/*.nupkg `pwd`/packages/

rebuild: clean build

clean:
	rm -rf `pwd`/nuget/.DS_Store
	rm -rf `pwd`/nuget/*/.DS_Store
	rm -rf `pwd`/nuget/*/*/.DS_Store
	rm -rf `pwd`/nuget/*/lib/*

build:
	dotnet build -c Release `pwd`/src/RabbitMQTopic/
