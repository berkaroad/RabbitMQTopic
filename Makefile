all: pack

test:
	dotnet run --project src/Samples/ProducerApp
	dotnet run --project src/Samples/ConsumerApp

perf:
	dotnet run --project src/Samples/PerformanceTests

pack: build
	mkdir -p `pwd`/packages
	dotnet pack -c Release `pwd`/src/RabbitMQTopic/
	mv `pwd`/src/RabbitMQTopic/bin/Release/*.nupkg `pwd`/packages/

build:
	dotnet build -c Release `pwd`/src/RabbitMQTopic/
