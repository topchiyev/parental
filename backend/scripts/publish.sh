#!/bin/bash

CUR_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

pushd "$CUR_DIR/.." > /dev/null
rm -rf publish
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeAllContentForSelfExtract=true -o ./publish
mv publish/appsettings.cloud.json publish/appsettings.json
popd > /dev/null