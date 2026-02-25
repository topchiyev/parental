#!/bin/bash

CUR_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

pushd "$CUR_DIR/.." > /dev/null

rm -rf dist

npm install
npx ng build --base-href=/parental/ --configuration=production

pushd dist/frontend/assets/environments > /dev/null

cp environment.cloud.json bak-environment.json
rm environment*.json
mv bak-environment.json environment.json

popd > /dev/null

popd > /dev/null
