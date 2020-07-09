all: pack

test: test-p test-c

test-p:
	dotnet run --project src/Samples/ProducerApp -c Release

test-c:
	dotnet run --project src/Samples/ConsumerApp -c Release

perf:
	dotnet run --project src/Samples/PerformanceTests -c Release

pack: build
	mkdir -p `pwd`/packages
	dotnet pack -c Release `pwd`/src/RabbitMQTopic/
	mv `pwd`/src/RabbitMQTopic/bin/Release/*.nupkg `pwd`/packages/

build:
	dotnet build -c Release `pwd`/src/RabbitMQTopic/
