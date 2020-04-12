all: pack

test:
	dotnet run --project src/Samples/ProducerApp
	dotnet run --project src/Samples/ConsumerApp

perf:
	dotnet run --project src/Samples/PerformanceTests

pack: rebuild
	rm -rf `pwd`/nuget/.DS_Store
	rm -rf `pwd`/nuget/*/.DS_Store
	rm -rf `pwd`/nuget/*/*/.DS_Store
	rm -rf `pwd`/nuget/*/lib/*/*.pdb
	rm -rf `pwd`/nuget/*/lib/*/*.json
	nuget pack -OutputDirectory `pwd`/packages/ `pwd`/nuget/RabbitMQTopic/RabbitMQTopic.nuspec

rebuild: clean build

clean:
	rm -rf `pwd`/nuget/.DS_Store
	rm -rf `pwd`/nuget/*/.DS_Store
	rm -rf `pwd`/nuget/*/*/.DS_Store
	rm -rf `pwd`/nuget/*/lib/*

build: build-2_0 build-netFramework

build-2_0:
	dotnet build -c Release -f 'netstandard2.0' -o `pwd`/nuget/RabbitMQTopic/lib/netstandard2.0/ `pwd`/src/RabbitMQTopic/

build-netFramework:
	msbuild `pwd`/src/RabbitMQTopic/RabbitMQTopic.csproj -r -noConLog -t:Rebuild -p:Configuration=Release -p:TargetFramework=net462 -p:OutputPath=`pwd`/nuget/RabbitMQTopic/lib/net462/
