#!/bin/bash

CUR_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

pushd "$CUR_DIR/.." > /dev/null
rm -rf bin
rm -rf obj
rm -rf publish
popd